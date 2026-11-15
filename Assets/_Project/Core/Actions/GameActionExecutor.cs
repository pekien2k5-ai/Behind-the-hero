using System;
using BTH.Core.Combat;
using BTH.Core.Run;
using BTH.Core.Turn;
using BTH.Core.Units;

namespace BTH.Core.Actions
{
    /// <summary>
    /// Thực thi lệnh: validate trước, đổi state qua registry/damage/status, rồi mới phát event.
    /// Không bao giờ tự chạy AI hay tự kết thúc lượt trừ khi lệnh là EndTurn.
    /// </summary>
    public sealed class GameActionExecutor : IGameActionExecutor
    {
        private readonly IActionValidator _validator;
        private readonly IObjectiveEvaluator _objective;

        public GameActionExecutor(IActionValidator validator, IObjectiveEvaluator objective = null)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _objective = objective ?? new ObjectiveEvaluator();
        }

        public ActionResult Execute(ActionRequest request, GameState state)
        {
            var validation = _validator.Validate(request, state);
            if (!validation.IsValid)
            {
                return ActionResult.Fail(validation.Error, validation.Message);
            }

            var actor = state.Units.Get(request.Actor);
            switch (request.Kind)
            {
                case ActionKind.Move:
                    return ExecuteMove(actor, request, state);
                case ActionKind.Attack:
                    return ExecuteAttack(actor, request, state);
                case ActionKind.Skill:
                    return ExecuteSkill(actor, request, state);
                case ActionKind.Guard:
                    return ExecuteGuard(actor, state);
                case ActionKind.EndTurn:
                    state.Turn.EndTurn();
                    return ActionResult.Ok();
                case ActionKind.ClashSkill:
                    return ExecuteClash(actor, request, state);
                default:
                    return ActionResult.Fail(ActionError.InvalidTarget, "Hành động không hỗ trợ");
            }
        }

        private ActionResult ExecuteMove(UnitState actor, ActionRequest request, GameState state)
        {
            var from = actor.Position;
            var pathfinder = state.Pathfinder ?? new AStarPathfinder(state.Grid);
            var path = pathfinder.FindPath(new PathRequest(from, request.MoveTo, actor.Move,
                actor.MovePoints, actor.Id));
            if (path.Status != PathStatus.Found)
            {
                return ActionResult.Fail(ActionError.CellBlocked, "Không có đường tới ô đích");
            }

            state.Turn.TrySpend(actor.Id, 1);
            state.Units.MoveTo(actor.Id, request.MoveTo);
            actor.HasMovedThisTurn = true;
            state.Events.Publish(new UnitMovedEvent(actor.Id, from, request.MoveTo, path.Cost));
            state.Events.Publish(new UnitActedEvent(actor.Id, ActionKind.Move, request.MoveTo));
            return ActionResult.Ok();
        }

        private ActionResult ExecuteAttack(UnitState actor, ActionRequest request, GameState state)
        {
            var target = state.Units.At(request.TargetCell);
            if (target == null)
            {
                return ActionResult.Fail(ActionError.TargetEmpty, "Ô mục tiêu trống");
            }

            var queue = new EffectQueue();
            var element = actor.Definition.NativeElement;
            queue.Enqueue(new DamageEffect(target.Id, actor.Definition.Attack, element));
            var ctx = new EffectContext(state, queue, actor.Id, actor.Center, target.Id,
                target.Position, null, actor.Definition.Attack, element);
            state.Turn.TrySpend(actor.Id, 1);
            actor.HasActedThisTurn = true;
            queue.Run(ctx);
            state.Events.Publish(new UnitActedEvent(actor.Id, ActionKind.Attack, target.Position));
            return FinishIfOver(state);
        }

        private ActionResult ExecuteSkill(UnitState actor, ActionRequest request, GameState state)
        {
            if (state.Skills == null || !state.Skills.TryGet(request.SkillId, out var skill))
            {
                return ActionResult.Fail(ActionError.SkillNotReady, "Không có chiêu " + request.SkillId);
            }

            var targetUnit = request.TargetCell.IsValid ? state.Units.At(request.TargetCell) : null;
            var queue = new EffectQueue();
            var ctx = new EffectContext(state, queue, actor.Id, actor.Center,
                targetUnit != null ? targetUnit.Id : UnitId.None, request.TargetCell, skill,
                skill.Power, skill.Element);

            state.Turn.TrySpend(actor.Id, skill.ApCost);
            actor.HasActedThisTurn = true;
            actor.SetCooldown(skill.Id, skill.CooldownTurns);

            if (skill.Behaviour != null)
            {
                skill.Behaviour.BuildEffects(ctx);
            }
            else
            {
                queue.Enqueue(new DamageEffect(UnitId.None, skill.Power, skill.Element, true));
            }

            queue.Run(ctx);
            state.Events.Publish(new SkillUsedEvent(actor.Id, skill.Id, ctx.ResolvedCell));
            state.Events.Publish(new UnitActedEvent(actor.Id, ActionKind.Skill, ctx.ResolvedCell));
            return FinishIfOver(state);
        }

        private ActionResult ExecuteGuard(UnitState actor, GameState state)
        {
            state.Turn.TrySpend(actor.Id, 1);
            actor.HasActedThisTurn = true;
            actor.Statuses.Add(new GuardStatus(), new StatusContext(actor, state.Events, state.Units));
            state.Events.Publish(new UnitActedEvent(actor.Id, ActionKind.Guard, actor.Position));
            return ActionResult.Ok();
        }

        private ActionResult ExecuteClash(UnitState actor, ActionRequest request, GameState state)
        {
            if (state.Clash == null || state.Stage == null || state.Stage.Clash == null)
            {
                return ActionResult.Fail(ActionError.ClashNotAvailable, "Màn không có Clash");
            }

            var boss = state.Units.Boss;
            if (boss == null || !boss.IsAlive)
            {
                return ActionResult.Fail(ActionError.ClashNotAvailable, "Không có boss để Clash");
            }

            if (!state.Skills.TryGet(request.SkillId, out var skill))
            {
                return ActionResult.Fail(ActionError.SkillNotReady, "Không có chiêu " + request.SkillId);
            }

            var outcome = state.Clash.Resolve(actor, boss, skill, state.Stage.Clash);
            ApplyClashEffect(outcome, actor, boss, state);
            actor.HasActedThisTurn = true;
            state.Events.Publish(new ClashResolvedEvent(boss.Id, skill.Id, outcome.Result,
                outcome.PlayerPower, outcome.BossPower));
            state.Events.Publish(new UnitActedEvent(actor.Id, ActionKind.ClashSkill, boss.Position));
            return FinishIfOver(state);
        }

        private static void ApplyClashEffect(ClashOutcome outcome, UnitState player, UnitState boss,
            GameState state)
        {
            if (outcome == null || outcome.AppliedEffect == null)
            {
                return;
            }

            var target = outcome.AppliedToBoss ? boss : player;
            if (target == null || !target.IsAlive)
            {
                return;
            }

            target.Statuses.Add(outcome.AppliedEffect,
                new StatusContext(target, state.Events, state.Units));
        }

        private StageResult CheckObjective(GameState state)
        {
            return _objective.Evaluate(state, state.Stage);
        }

        /// <summary>Kết thúc màn ngay nếu đánh giết boss/chết giữa lượt, không đợi EndTurn.</summary>
        private ActionResult Finish(StageResult result)
        {
            return ActionResult.Ok();
        }

        private ActionResult FinishIfOver(GameState state)
        {
            var result = _objective.Evaluate(state, state.Stage);
            if (result != StageResult.None && state.Turn != null &&
                state.Turn.Phase != TurnPhase.StageEnded)
            {
                state.Turn.FinishStage(result);
            }

            return ActionResult.Ok();
        }
    }
}
