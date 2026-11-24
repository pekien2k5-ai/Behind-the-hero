using System;
using System.Collections.Generic;
using BTH.Core.Actions;
using BTH.Core.Ai;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Turn;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    /// <summary>
    /// Composition root duy nhất của Core: dựng state + service cho một màn, giữ cửa vào
    /// TryExecute/EndTurn, và là nơi duy nhất biết cách chạy phase địch bằng brain.
    /// Không tham chiếu UnityEngine; Unity chỉ gọi vào đây.
    /// </summary>
    public sealed class GameSession
    {
        private readonly Dictionary<string, StageDefinition> _stages =
            new Dictionary<string, StageDefinition>(StringComparer.Ordinal);

        private readonly IGameRules _rules;
        private readonly ISkillLibrary _skills;
        private readonly IEnemyBrainLibrary _brains;
        private readonly IClashResolver _clash;
        private readonly ElementMatrix _elements;
        private readonly IObjectiveEvaluator _objective;
        private readonly IInitiativePolicy _initiative;
        private readonly int _snapshotCapacity;
        private readonly int _seed;
        private readonly Action<UnitState, GridCoord, GameState> _onUnitEnteredCell;
        private IRandomSource _random;
        private GameActionExecutor _executor;

        public GameSession(IGameRules rules = null, ISkillLibrary skills = null,
            IEnemyBrainLibrary brains = null, IClashResolver clash = null,
            IRandomSource random = null, ElementMatrix elements = null,
            IObjectiveEvaluator objective = null, IInitiativePolicy initiative = null,
            int snapshotCapacity = 8, int seed = 20260101,
            Action<UnitState, GridCoord, GameState> onUnitEnteredCell = null)
        {
            _rules = rules ?? new GameRules();
            _skills = skills ?? new SkillLibrary();
            _brains = brains ?? new EnemyBrainLibrary();
            _clash = clash;
            _elements = elements;
            _objective = objective ?? new ObjectiveEvaluator();
            _initiative = initiative;
            _snapshotCapacity = snapshotCapacity;
            _seed = seed;
            _onUnitEnteredCell = onUnitEnteredCell;
            _random = random;
        }

        public GameState State { get; private set; }

        public IEventBus Events => State?.Events;

        public ISkillLibrary Skills => _skills;

        public IEnemyBrainLibrary Brains => _brains;

        public IRunKnowledge Knowledge => State?.Knowledge;

        public void RegisterStage(StageDefinition stage)
        {
            if (stage == null || string.IsNullOrEmpty(stage.Id))
            {
                throw new ArgumentException("Stage phải có Id.", nameof(stage));
            }

            _stages[stage.Id] = stage;
        }

        public void StartStage(string stageId)
        {
            if (!_stages.TryGetValue(stageId, out var stage))
            {
                throw new KeyNotFoundException("Chưa đăng ký màn: " + stageId);
            }

            StartStage(stage);
        }

        public void StartStage(StageDefinition stage)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            RegisterStage(stage);
            State = Build(stage);
        }

        /// <summary>Chơi lại màn hiện tại từ đầu: state mới, bộ nhớ run mới (không lưu đĩa).</summary>
        public void Restart()
        {
            if (State?.Stage == null)
            {
                return;
            }

            var stage = State.Stage;
            if (_random is XorShiftRandom)
            {
                _random = new XorShiftRandom(_seed);
            }

            State = Build(stage);
        }

        /// <summary>Tên hợp đồng trong tài liệu kiến trúc; hành vi giống Restart.</summary>
        public void Reset()
        {
            Restart();
        }

        /// <summary>Thực thi một lệnh; validate nằm trong executor nên lỗi trả về ActionResult.</summary>
        public ActionResult TryExecute(ActionRequest request)
        {
            if (State == null)
            {
                return ActionResult.Fail(ActionError.StageEnded, "Chưa bắt đầu màn");
            }

            var result = _executor.Execute(request, State);
            if (result.Success)
            {
                TryEnterDuel();
            }

            return result;
        }

        /// <summary>
        /// Kết thúc lượt người chơi theo đúng nhịp: phase địch (brain tự hành động) →
        /// tick cuối lượt → sang lượt mới → mở phase người chơi.
        /// Lệnh EndTurn đi qua TryExecute chỉ kết thúc lượt mà KHÔNG chạy AI (dùng cho test).
        /// </summary>
        public ActionResult EndTurn()
        {
            if (State == null)
            {
                return ActionResult.Fail(ActionError.StageEnded, "Chưa bắt đầu màn");
            }

            if (State.IsOver)
            {
                return ActionResult.Fail(ActionError.StageEnded, "Màn đã kết thúc");
            }

            if (State.Turn.Phase == TurnPhase.Duel)
            {
                return ActionResult.Fail(ActionError.WrongPhase, "Đang trong Duel");
            }

            if (State.Turn.Phase != TurnPhase.Player)
            {
                return ActionResult.Fail(ActionError.NotPlayerPhase, "Không phải lượt người chơi");
            }

            State.Turn.BeginEnemyPhase();
            RunEnemyBrains();
            if (State.IsOver)
            {
                return ActionResult.Ok();
            }

            State.Turn.EndEnemyPhase();
            State.Turn.EndTurn();
            if (State.IsOver)
            {
                return ActionResult.Ok();
            }

            PlanEnemyIntents(State);
            State.Turn.BeginPlayerPhase();
            // TurnStart checkpoint ở phase Player + telegraph mới → rewind luôn hành động được.
            State.Snapshots.PushCheckpoint(SnapshotTrigger.TurnStart);
            return ActionResult.Ok();
        }

        /// <summary>Mở phase người chơi (dùng khi driver tự chạy AI bên ngoài EndTurn).</summary>
        public void BeginPlayerPhase()
        {
            if (State != null && !State.IsOver && State.Turn.Phase != TurnPhase.Duel)
            {
                State.Turn.BeginPlayerPhase();
            }
        }

        private GameState Build(StageDefinition stage)
        {
            var events = new EventBus();
            var grid = new GridMap(stage.Width, stage.Height, events);
            var units = new UnitRegistry(grid, events);
            _random = _random ?? new XorShiftRandom(_seed);

            var state = new GameState(grid, units, _rules, events, _random);
            state.Stage = stage;
            state.Result = StageResult.None;
            state.Skills = _skills;
            state.Pathfinder = new AStarPathfinder(grid);
            state.Damage = new DamageCalculator(_elements);
            state.Telegraph = new TelegraphService(events);
            state.Clash = _clash ?? new ClashResolver();
            state.Duel = new ClashDuel(state);
            state.BossPhase = new BossPhaseController(_rules);
            state.Knowledge = new RunKnowledge(events);
            state.Turn = new TurnManager(state, _initiative, _objective);
            state.Snapshots = new SnapshotService(state, new SnapshotStore(_snapshotCapacity));

            if (_onUnitEnteredCell != null)
            {
                units.OnUnitEnteredCell = (unit, cell) => _onUnitEnteredCell(unit, cell, state);
            }

            events.Subscribe<UnitDiedEvent>((in UnitDiedEvent e) =>
            {
                if (units.TryGet(e.Unit, out var unit) && unit.Faction == UnitFaction.Player)
                {
                    state.Knowledge.RegisterDeath();
                }
            });

            ApplyTerrains(stage, grid);
            SpawnUnits(stage, units);

            _executor = new GameActionExecutor(new ActionValidator(state.Pathfinder), _objective);

            state.Turn.StartStage(stage);
            PlanEnemyIntents(state);
            state.Turn.BeginPlayerPhase();
            // Checkpoint StageStart phải ở phase Player (đã có telegraph) để rewind
            // trả về đúng thời điểm người chơi còn ra lệnh được.
            state.Snapshots.PushCheckpoint(SnapshotTrigger.StageStart);
            return state;
        }

        private static void ApplyTerrains(StageDefinition stage, IGridMap grid)
        {
            for (int i = 0; i < stage.Terrains.Count; i++)
            {
                var terrain = stage.Terrains[i];
                grid.SetTerrain(terrain.Coord, terrain.Terrain, terrain.Flags,
                    terrain.MovementCost, terrain.HazardDamagePerTurn);
                if (terrain.CollapseInTurns > 0)
                {
                    grid.ArmCollapse(terrain.Coord, terrain.CollapseInTurns);
                }
            }
        }

        private static void SpawnUnits(StageDefinition stage, IUnitRegistry units)
        {
            if (stage.Player != null)
            {
                units.Add(stage.Player, stage.PlayerSpawn);
            }

            for (int i = 0; i < stage.Spawns.Count; i++)
            {
                var spawn = stage.Spawns[i];
                if (spawn?.Definition != null)
                {
                    units.Add(spawn.Definition, spawn.Anchor);
                }
            }
        }

        /// <summary>
        /// Công bố ý định của toàn bộ địch TRƯỚC lượt người chơi: danger cells hiện ra
        /// để người chơi né, đúng nhịp Telegraph → Player. Ý định không đổi trong lượt.
        /// </summary>
        private void PlanEnemyIntents(GameState state)
        {
            state.Telegraph.Clear();
            var enemies = state.Units.Alive(UnitFaction.Enemy);
            for (int i = 0; i < enemies.Count; i++)
            {
                var unit = enemies[i];
                var brainId = unit.Definition.BrainId;
                if (string.IsNullOrEmpty(brainId) || !_brains.TryGet(brainId, out var brain))
                {
                    continue;
                }

                var intent = brain.Plan(unit, state);
                if (intent != null && !intent.IsEmpty)
                {
                    state.Telegraph.Declare(intent);
                }
            }
        }

        private void RunEnemyBrains()
        {
            var id = State.Turn.NextEnemy();
            while (id.IsValid)
            {
                var unit = State.Units.Get(id);
                if (unit != null && State.Telegraph.TryGet(id, out var intent))
                {
                    ResolveIntent(unit, intent);
                }

                if (State.IsOver)
                {
                    return;
                }

                id = State.Turn.NextEnemy();
            }
        }

        private void ResolveIntent(UnitState unit, EnemyIntent intent)
        {
            if (intent.Effects == null || intent.Effects.Count == 0)
            {
                return;
            }

            _skills.TryGet(intent.SkillId, out var skill);
            var queue = new EffectQueue();
            for (int i = 0; i < intent.Effects.Count; i++)
            {
                queue.Enqueue(intent.Effects[i]);
            }

            int power = skill != null ? skill.Power : 0;
            var element = skill != null ? skill.Element : unit.Definition.NativeElement;
            var context = new EffectContext(State, queue, unit.Id, intent.TargetCell, UnitId.None,
                intent.TargetCell, skill, power, element);
            queue.Run(context);
        }

        /// <summary>
        /// Vào Duel phase 2 ngay (bỏ bàn cờ): clear telegraph, khoá sân qua BossPhase,
        /// bắt đầu phiên đấu chiêu, chuyển TurnPhase sang Duel.
        /// </summary>
        public void EnterDuel()
        {
            if (State == null || State.IsOver || State.Turn.Phase == TurnPhase.Duel)
            {
                return;
            }

            var stage = State.Stage;
            var player = State.Units.Player;
            var boss = State.Units.Boss;
            if (stage?.Duel == null || player == null || boss == null || !player.IsAlive || !boss.IsAlive)
            {
                return;
            }

            State.Telegraph?.Clear();
            State.BossPhase?.EnterPhase2(State);
            State.Duel.Start(State, stage.Duel, player, boss);
            State.Turn.BeginDuel();
        }

        private void TryEnterDuel()
        {
            if (State == null || State.IsOver || State.Turn.Phase == TurnPhase.Duel)
            {
                return;
            }

            var stage = State.Stage;
            var boss = State.Units.Boss;
            if (stage?.Duel == null || boss == null || !boss.IsAlive || boss.MaxHp <= 0)
            {
                return;
            }

            if (boss.HpRatio < State.Rules.Phase2Threshold)
            {
                EnterDuel();
            }
        }
    }
}
