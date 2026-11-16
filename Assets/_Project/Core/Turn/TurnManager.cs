using System;
using System.Collections.Generic;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Turn
{
    /// <summary>
    /// Quản lý nhịp lượt cho một màn. Không tự chạy AI: nó chỉ mở/đóng phase và
    /// cung cấp thứ tự địch; driver (GameSession/AllyBrain) gọi brain rồi executor.
    /// </summary>
    public sealed class TurnManager : ITurnManager
    {
        private readonly GameState _state;
        private readonly IInitiativePolicy _initiative;
        private readonly IObjectiveEvaluator _objective;
        private readonly List<UnitId> _enemyOrder = new List<UnitId>();
        private int _enemyIndex;

        public TurnManager(GameState state, IInitiativePolicy initiative = null,
            IObjectiveEvaluator objective = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _initiative = initiative ?? new InitiativePolicy();
            _objective = objective ?? new ObjectiveEvaluator();
            Phase = TurnPhase.None;
            _enemyIndex = 0;
        }

        public TurnPhase Phase { get; private set; }

        public int TurnIndex { get; private set; }

        public int TurnLimit => _state.Stage != null ? _state.Stage.TurnLimit : _state.Rules.TurnLimit;

        public UnitId ActiveUnit
        {
            get
            {
                if (Phase == TurnPhase.Player)
                {
                    var player = _state.Units.Player;
                    return player != null ? player.Id : UnitId.None;
                }

                if (Phase == TurnPhase.Enemy && _enemyIndex < _enemyOrder.Count)
                {
                    return _enemyOrder[_enemyIndex];
                }

                return UnitId.None;
            }
        }

        public IReadOnlyList<UnitId> EnemyOrder => _enemyOrder;

        /// <summary>Chỉ SnapshotService dùng: dựng lại lượt/phase mà không phát event.</summary>
        internal void RestoreState(int turnIndex, TurnPhase phase)
        {
            TurnIndex = turnIndex;
            Phase = phase;
        }

        public void StartStage(StageDefinition stage)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            _state.Stage = stage;
            _state.Result = StageResult.None;
            TurnIndex = 1;
            _enemyOrder.Clear();
            _enemyIndex = 0;
            Phase = TurnPhase.StageIntro;
            _state.Events.Publish(new StageStartedEvent(stage.Id, TurnLimit));
            _state.Events.Publish(new TurnPhaseStartedEvent(Phase, TurnIndex));
            BeginTelegraph();
        }

        public void BeginTelegraph()
        {
            if (_state.IsOver)
            {
                return;
            }

            SetPhase(TurnPhase.Telegraph);
        }

        public void BeginPlayerPhase()
        {
            if (_state.IsOver)
            {
                return;
            }

            ResetActionPoints();
            SetPhase(TurnPhase.Player);
        }

        public void EndPlayerPhase()
        {
            if (Phase == TurnPhase.Player)
            {
                SetPhase(TurnPhase.TurnEnd);
            }
        }

        public void BeginEnemyPhase()
        {
            if (_state.IsOver)
            {
                return;
            }

            RebuildEnemyOrder();
            _enemyIndex = 0;
            for (int i = 0; i < _enemyOrder.Count; i++)
            {
                ResetUnit(_enemyOrder[i]);
            }

            SetPhase(TurnPhase.Enemy);
        }

        public void EndEnemyPhase()
        {
            if (Phase == TurnPhase.Enemy)
            {
                SetPhase(TurnPhase.TurnEnd);
            }
        }

        public void EndTurn()
        {
            if (_state.IsOver || Phase == TurnPhase.Duel)
            {
                return;
            }

            SetPhase(TurnPhase.TurnEnd);
            TickUnits();
            TickTerrain();

            TurnIndex++;
            _state.Events.Publish(new TurnAdvancedEvent(TurnIndex));

            var result = _objective.Evaluate(_state, _state.Stage);
            if (result != StageResult.None)
            {
                FinishStage(result);
                return;
            }

            ResetActionPoints();
            BeginTelegraph();
        }

        public void BeginClash()
        {
            if (_state.IsOver)
            {
                return;
            }

            SetPhase(TurnPhase.Clash);
            if (_state.Snapshots != null)
            {
                // Checkpoint trước khi chốt kết quả Clash: rewind sẽ trả về đúng thời điểm
                // người chơi còn cơ hội chọn lại chiêu ứng biến.
                _state.Snapshots.PushCheckpoint(SnapshotTrigger.ClashStart);
            }
        }

        /// <summary>Phase 2: chuyển sang Duel — từ đây không còn AP/lượt/bàn cờ.</summary>
        public void BeginDuel()
        {
            if (_state.IsOver)
            {
                return;
            }

            SetPhase(TurnPhase.Duel);
        }

        public void FinishStage(StageResult result)
        {
            _state.Result = result;
            Phase = TurnPhase.StageEnded;
            _state.Events.Publish(new TurnPhaseStartedEvent(Phase, TurnIndex));
            _state.Events.Publish(new StageEndedEvent(result, TurnIndex));
        }

        public bool CanSpend(UnitId unit, int cost)
        {
            if (cost <= 0 || !_state.Units.TryGet(unit, out var state) || !state.IsAlive)
            {
                return false;
            }

            if (state.IsStaggered)
            {
                return false;
            }

            return state.ActionPoints >= cost;
        }

        public bool TrySpend(UnitId unit, int cost)
        {
            if (!CanSpend(unit, cost))
            {
                return false;
            }

            var state = _state.Units.Get(unit);
            state.ActionPoints -= cost;
            _state.Events.Publish(new ApChangedEvent(unit, state.ActionPoints));
            return true;
        }

        public void ResetActionPoints()
        {
            var units = _state.Units.All;
            for (int i = 0; i < units.Count; i++)
            {
                ResetUnit(units[i].Id);
            }
        }

        public UnitId NextEnemy()
        {
            while (_enemyIndex < _enemyOrder.Count)
            {
                var id = _enemyOrder[_enemyIndex++];
                if (_state.Units.TryGet(id, out var unit) && unit.IsAlive)
                {
                    return id;
                }
            }

            return UnitId.None;
        }

        private void RebuildEnemyOrder()
        {
            _enemyOrder.Clear();
            var enemies = _state.Units.Alive(UnitFaction.Enemy);
            var order = _initiative.Order(enemies);
            for (int i = 0; i < order.Count; i++)
            {
                _enemyOrder.Add(order[i]);
            }
        }

        private void ResetUnit(UnitId id)
        {
            if (!_state.Units.TryGet(id, out var unit) || !unit.IsAlive)
            {
                return;
            }

            unit.ResetTurnState();
            unit.ActionPoints = _state.Rules.ApPerTurn;
            _state.Events.Publish(new ApChangedEvent(id, unit.ActionPoints));
        }

        /// <summary>Tick status của mọi unit còn sống: hết lượt chủ thể rồi mở lượt kế.</summary>
        private void TickUnits()
        {
            var units = _state.Units.All;
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.IsAlive)
                {
                    continue;
                }

                var ctx = new StatusContext(unit, _state.Events, _state.Units);
                unit.Statuses.TickTurnEnd(ctx);
                unit.TickCooldowns();
                if (unit.IsAlive)
                {
                    unit.Statuses.TickTurnStart(ctx);
                }
            }
        }

        /// <summary>Bẫy/địa hình gây sát thương rồi mới tick sụp ô.</summary>
        private void TickTerrain()
        {
            if (_state.Damage == null)
            {
                _state.Grid.TickCollapse();
                return;
            }

            var units = _state.Units.All;
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.IsAlive || unit.Move.IgnoresHazard)
                {
                    continue;
                }

                var cells = unit.Footprint;
                int hazardDamage = 0;
                var hazardCell = GridCoord.None;
                for (int j = 0; j < cells.Count; j++)
                {
                    if (!_state.Grid.TryGet(cells[j], out var cell) || !cell.IsHazard)
                    {
                        continue;
                    }

                    // Hazard tính MỘT lần mỗi unit mỗi lượt, lấy ô nguy hiểm mạnh nhất
                    // trong footprint — đứng trên 2 ô độc không bị trừ HP gấp đôi.
                    if (cell.HazardDamagePerTurn > hazardDamage)
                    {
                        hazardDamage = cell.HazardDamagePerTurn;
                        hazardCell = cells[j];
                    }
                }

                if (hazardDamage <= 0)
                {
                    continue;
                }

                _state.Events.Publish(new HazardTriggeredEvent(hazardCell, unit.Id, hazardDamage));
                var request = new DamageRequest(UnitId.None, unit.Id, hazardDamage, ElementId.None,
                    false, true, hazardCell);
                _state.Damage.Apply(request, _state);
                if (!unit.IsAlive)
                {
                    continue;
                }
            }

            _state.Grid.TickCollapse();
        }

        private void SetPhase(TurnPhase phase)
        {
            Phase = phase;
            _state.Events.Publish(new TurnPhaseStartedEvent(phase, TurnIndex));
        }
    }
}
