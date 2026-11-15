using System;
using System.Collections.Generic;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Turn;
using BTH.Core.Units;

namespace BTH.Core.Actions
{
    /// <summary>
    /// Bộ luật mặc định: 1 AP di chuyển (tối đa MovePoints ô), 1 AP đánh/chiêu/thủ,
    /// tầm đo Manhattan từ tâm, LOS chỉ khi chiêu yêu cầu.
    /// </summary>
    public sealed class ActionValidator : IActionValidator
    {
        private readonly IPathfinder _pathfinder;

        public ActionValidator(IPathfinder pathfinder = null)
        {
            _pathfinder = pathfinder;
        }

        private IPathfinder PathfinderFor(GameState state)
        {
            if (_pathfinder != null)
            {
                return _pathfinder;
            }

            return state.Pathfinder ?? new AStarPathfinder(state.Grid);
        }

        public ValidationResult Validate(ActionRequest request, GameState state)
        {
            if (state == null)
            {
                return ValidationResult.Fail(ActionError.StageEnded, "Chưa có state");
            }

            if (state.IsOver)
            {
                return ValidationResult.Fail(ActionError.ObjectiveAlreadyEnded, "Màn đã kết thúc");
            }

            if (!state.Units.TryGet(request.Actor, out var actor))
            {
                return ValidationResult.Fail(ActionError.UnknownActor, "Không có unit " + request.Actor.Value);
            }

            if (!actor.IsAlive)
            {
                return ValidationResult.Fail(ActionError.UnitDead, "Unit đã chết");
            }

            if (request.Kind == ActionKind.EndTurn)
            {
                if (state.Turn.Phase != TurnPhase.Player)
                {
                    return ValidationResult.Fail(ActionError.NotPlayerPhase, "Chưa tới lượt người chơi");
                }

                return ValidationResult.Ok();
            }

            if (request.Kind == ActionKind.ClashSkill)
            {
                return ValidateClash(actor, request, state);
            }

            if (state.Turn.Phase != TurnPhase.Player)
            {
                return ValidationResult.Fail(ActionError.NotPlayerPhase, "Chưa tới lượt người chơi");
            }

            if (actor.IsStaggered)
            {
                return ValidationResult.Fail(ActionError.ActorStunned, "Đang choáng");
            }

            switch (request.Kind)
            {
                case ActionKind.Move:
                    return ValidateMove(actor, request, state);
                case ActionKind.Attack:
                    return ValidateAttack(actor, request, state);
                case ActionKind.Skill:
                    return ValidateSkill(actor, request, state);
                case ActionKind.Guard:
                    return ValidateGuard(actor, state);
                default:
                    return ValidationResult.Fail(ActionError.InvalidTarget, "Hành động không hỗ trợ");
            }
        }

        public IReadOnlyList<GridCoord> GetValidTargets(ActionRequest request, GameState state)
        {
            var result = new List<GridCoord>();
            if (state == null || state.IsOver)
            {
                return result;
            }

            if (!state.Units.TryGet(request.Actor, out var actor) || !actor.IsAlive)
            {
                return result;
            }

            switch (request.Kind)
            {
                case ActionKind.Move:
                    return MoveTargets(actor, state);
                case ActionKind.Attack:
                    return AttackTargets(actor, state);
                case ActionKind.Skill:
                    return SkillTargets(actor, request, state);
                case ActionKind.Guard:
                case ActionKind.EndTurn:
                    result.Add(actor.Position);
                    return result;
                default:
                    return result;
            }
        }

        private ValidationResult ValidateMove(UnitState actor, ActionRequest request, GameState state)
        {
            if (actor.IsRooted)
            {
                return ValidationResult.Fail(ActionError.CannotMoveRooted, "Đang bị giữ chân");
            }

            if (actor.HasMovedThisTurn)
            {
                return ValidationResult.Fail(ActionError.AlreadyMoved, "Đã di chuyển trong lượt");
            }

            if (!state.Turn.CanSpend(actor.Id, 1))
            {
                return ValidationResult.Fail(ActionError.NoActionPoints, "Hết AP");
            }

            if (!request.MoveTo.IsValid || !state.Grid.InBounds(request.MoveTo))
            {
                return ValidationResult.Fail(ActionError.InvalidTarget, "Ô đích ngoài lưới");
            }

            if (state.Grid.IsBlocked(request.MoveTo) && !CanFlyOver(actor, state, request.MoveTo))
            {
                return ValidationResult.Fail(ActionError.CellBlocked, "Ô đích bị chặn");
            }

            var path = PathfinderFor(state).FindPath(new PathRequest(actor.Position, request.MoveTo,
                actor.Move, actor.MovePoints, actor.Id));
            if (path.Status != PathStatus.Found)
            {
                return ValidationResult.Fail(
                    path.Status == PathStatus.OutOfRange
                        ? ActionError.NotEnoughMovePoints
                        : ActionError.CellBlocked, "Không có đường tới ô đích");
            }

            return ValidationResult.Ok();
        }

        private ValidationResult ValidateAttack(UnitState actor, ActionRequest request, GameState state)
        {
            if (actor.HasActedThisTurn)
            {
                return ValidationResult.Fail(ActionError.AlreadyActed, "Đã hành động trong lượt");
            }

            if (!state.Turn.CanSpend(actor.Id, 1))
            {
                return ValidationResult.Fail(ActionError.NoActionPoints, "Hết AP");
            }

            if (!request.TargetCell.IsValid || !state.Grid.InBounds(request.TargetCell))
            {
                return ValidationResult.Fail(ActionError.InvalidTarget, "Ô mục tiêu không hợp lệ");
            }

            var target = state.Units.At(request.TargetCell);
            if (target == null || !target.IsAlive)
            {
                return ValidationResult.Fail(ActionError.TargetEmpty, "Ô mục tiêu trống");
            }

            if (target.Faction == actor.Faction)
            {
                return ValidationResult.Fail(ActionError.InvalidTarget, "Không đánh đồng minh");
            }

            if (Manhattan(actor.Center, target.Center) > 1)
            {
                return ValidationResult.Fail(ActionError.OutOfRange, "Ngoài tầm đánh");
            }

            return ValidationResult.Ok();
        }

        private ValidationResult ValidateSkill(UnitState actor, ActionRequest request, GameState state)
        {
            if (actor.HasActedThisTurn)
            {
                return ValidationResult.Fail(ActionError.AlreadyActed, "Đã hành động trong lượt");
            }

            if (state.Skills == null || !state.Skills.TryGet(request.SkillId, out var skill))
            {
                return ValidationResult.Fail(ActionError.SkillNotReady, "Không có chiêu " + request.SkillId);
            }

            if (!HasSkill(actor, skill.Id))
            {
                return ValidationResult.Fail(ActionError.SkillNotReady, "Unit không có chiêu này");
            }

            if (actor.GetCooldown(skill.Id) > 0)
            {
                return ValidationResult.Fail(ActionError.SkillOnCooldown, "Chiêu đang hồi");
            }

            if (!state.Turn.CanSpend(actor.Id, skill.ApCost))
            {
                return ValidationResult.Fail(ActionError.NoActionPoints, "Không đủ AP cho chiêu");
            }

            return ValidateSkillTarget(actor, skill, request.TargetCell, state);
        }

        private ValidationResult ValidateSkillTarget(UnitState actor, SkillDefinition skill,
            GridCoord cell, GameState state)
        {
            if (skill.Targeting == SkillTargeting.Self)
            {
                return cell == actor.Position
                    ? ValidationResult.Ok()
                    : ValidationResult.Fail(ActionError.InvalidTarget, "Chiêu chỉ dùng lên bản thân");
            }

            if (!cell.IsValid || !state.Grid.InBounds(cell))
            {
                return ValidationResult.Fail(ActionError.InvalidTarget, "Ô mục tiêu không hợp lệ");
            }

            if (skill.RequiresLineOfSight && !state.Grid.HasLineOfSight(actor.Center, cell))
            {
                return ValidationResult.Fail(ActionError.NoLineOfSight, "Không thấy mục tiêu");
            }

            var target = state.Units.At(cell);
            if (skill.Targeting == SkillTargeting.Unit && (target == null || !target.IsAlive))
            {
                return ValidationResult.Fail(ActionError.TargetEmpty, "Ô mục tiêu trống");
            }

            int distance = Manhattan(actor.Center, cell);
            if (distance < skill.RangeMin || distance > skill.RangeMax)
            {
                return ValidationResult.Fail(ActionError.OutOfRange, "Ngoài tầm chiêu");
            }

            return ValidationResult.Ok();
        }

        private ValidationResult ValidateGuard(UnitState actor, GameState state)
        {
            if (actor.HasActedThisTurn)
            {
                return ValidationResult.Fail(ActionError.AlreadyActed, "Đã hành động trong lượt");
            }

            if (!state.Turn.CanSpend(actor.Id, 1))
            {
                return ValidationResult.Fail(ActionError.NoActionPoints, "Hết AP");
            }

            return ValidationResult.Ok();
        }

        private ValidationResult ValidateClash(UnitState actor, ActionRequest request, GameState state)
        {
            if (state.Turn.Phase != TurnPhase.Clash || state.Clash == null)
            {
                return ValidationResult.Fail(ActionError.ClashNotAvailable, "Chưa tới nhịp Clash");
            }

            if (state.Skills == null || !state.Skills.TryGet(request.SkillId, out var skill) ||
                !skill.IsClashSkill || !HasSkill(actor, skill.Id))
            {
                return ValidationResult.Fail(ActionError.SkillNotReady, "Chiêu không dùng được trong Clash");
            }

            return ValidationResult.Ok();
        }

        private IReadOnlyList<GridCoord> MoveTargets(UnitState actor, GameState state)
        {
            var result = new List<GridCoord>();
            if (actor.IsRooted || !state.Turn.CanSpend(actor.Id, 1))
            {
                return result;
            }

            var pathfinder = PathfinderFor(state);
            var reachable = pathfinder.ComputeReachable(actor.Position, actor.Move, actor.MovePoints,
                actor.Id);
            var cells = reachable.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != actor.Position)
                {
                    result.Add(cells[i]);
                }
            }

            return result;
        }

        private IReadOnlyList<GridCoord> AttackTargets(UnitState actor, GameState state)
        {
            var result = new List<GridCoord>();
            var neighbours = GridShapes.Neighbours4(actor.Center);
            for (int i = 0; i < neighbours.Count; i++)
            {
                if (!state.Grid.InBounds(neighbours[i]))
                {
                    continue;
                }

                var unit = state.Units.At(neighbours[i]);
                if (unit != null && unit.IsAlive && unit.Faction != actor.Faction)
                {
                    result.Add(neighbours[i]);
                }
            }

            return result;
        }

        private IReadOnlyList<GridCoord> SkillTargets(UnitState actor, ActionRequest request,
            GameState state)
        {
            var result = new List<GridCoord>();
            if (state.Skills == null || !state.Skills.TryGet(request.SkillId, out var skill))
            {
                return result;
            }

            if (skill.Targeting == SkillTargeting.Self)
            {
                result.Add(actor.Position);
                return result;
            }

            for (int x = 0; x < state.Grid.Width; x++)
            {
                for (int y = 0; y < state.Grid.Height; y++)
                {
                    var cell = new GridCoord(x, y);
                    if (!ValidateSkillTarget(actor, skill, cell, state).IsValid)
                    {
                        continue;
                    }

                    if (skill.Targeting == SkillTargeting.Cell ||
                        state.Units.At(cell) != null)
                    {
                        result.Add(cell);
                    }
                }
            }

            return result;
        }

        private static bool HasSkill(UnitState actor, string skillId)
        {
            var ids = actor.Definition.SkillIds;
            if (ids == null)
            {
                return false;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], skillId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int Manhattan(GridCoord a, GridCoord b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private static bool CanFlyOver(UnitState actor, GameState state, GridCoord cell)
        {
            if (!actor.Move.CanFly || !state.Grid.TryGet(cell, out var c))
            {
                return false;
            }

            return !c.BlocksLineOfSight;
        }
    }
}
