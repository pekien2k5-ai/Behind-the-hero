using System;
using System.Collections.Generic;
using BTH.Core.Ai;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Turn;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    /// <summary>
    /// Chụp/tua state của một run. Chỉ sống trong RAM; checkpoint theo SnapshotTrigger.
    /// Rewind đưa game về checkpoint gần nhất đã đẩy (StageStart / TurnStart / ClashStart).
    /// </summary>
    public sealed class SnapshotService : ISnapshotService
    {
        private readonly GameState _state;
        private readonly IGridMap _grid;
        private readonly IUnitRegistry _units;
        private readonly ITurnManager _turn;
        private readonly ITelegraphService _telegraph;
        private readonly ISnapshotStore _store;
        private readonly IEventBus _events;
        private readonly List<GameSnapshot> _checkpoints = new List<GameSnapshot>();
        private readonly List<SnapshotTrigger> _checkpointTriggers = new List<SnapshotTrigger>();

        public SnapshotService(GameState state, ISnapshotStore store = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _grid = state.Grid;
            _units = state.Units;
            _turn = state.Turn;
            _telegraph = state.Telegraph;
            _events = state.Events;
            _store = store ?? new SnapshotStore();
        }

        public bool CanRewind => _checkpoints.Count > 0;

        public GameSnapshot Capture()
        {
            var cells = CopyCells();
            var locked = CopyLockedCells();
            var units = CaptureUnits();
            var intents = CaptureIntents();

            return new GameSnapshot(_turn.TurnIndex, _turn.Phase, _state.Result, _state.Random.State,
                cells, locked, units, intents);
        }

        public void Restore(GameSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Cells.Length != _grid.Width * _grid.Height)
            {
                throw new ArgumentException("Snapshot không khớp kích thước lưới hiện tại.", nameof(snapshot));
            }

            _state.Random.State = snapshot.RandomState;

            if (_grid is GridMap concreteGrid)
            {
                concreteGrid.RestoreCells(snapshot.Cells, snapshot.LockedCells);
            }

            if (_units is UnitRegistry concreteUnits)
            {
                concreteUnits.ClearSilently();
                for (int i = 0; i < snapshot.Units.Length; i++)
                {
                    var unit = snapshot.Units[i];
                    concreteUnits.RestoreAdd(unit.Definition, unit.Position, unit.Id.Value, unit.Hp,
                        unit.ActionPoints);
                }
            }

            for (int i = 0; i < snapshot.Units.Length; i++)
            {
                var captured = snapshot.Units[i];
                if (!_units.TryGet(captured.Id, out var unit))
                {
                    continue;
                }

                unit.SetHp(captured.Hp);
                unit.SetPosition(captured.Position);
                unit.ActionPoints = captured.ActionPoints;
                unit.HasMovedThisTurn = captured.HasMovedThisTurn;
                unit.HasActedThisTurn = captured.HasActedThisTurn;
                unit.RestoreCooldowns(captured.Cooldowns);
                unit.Statuses.Restore(ToEntries(captured.Statuses));
            }

            if (_telegraph != null)
            {
                if (_telegraph is TelegraphService concreteTelegraph)
                {
                    concreteTelegraph.RestoreSilently(snapshot.Intents);
                }
            }

            _state.Result = snapshot.Result;
            if (_state.Turn is TurnManager concreteTurn)
            {
                concreteTurn.RestoreState(snapshot.TurnIndex, snapshot.Phase);
            }
        }

        public void PushCheckpoint(SnapshotTrigger trigger)
        {
            var snapshot = Capture();
            _checkpoints.Add(snapshot);
            _checkpointTriggers.Add(trigger);
            _store.Push(snapshot);
            _events?.Publish(new SnapshotPushedEvent(trigger, _turn.TurnIndex));
        }

        public void Rewind()
        {
            if (_checkpoints.Count == 0)
            {
                return;
            }

            int index = _checkpoints.Count - 1;
            var trigger = _checkpointTriggers[index];
            var snapshot = _checkpoints[index];
            _checkpoints.RemoveAt(index);
            _checkpointTriggers.RemoveAt(index);

            Restore(snapshot);
            _events?.Publish(new RewindPerformedEvent(trigger, _turn.TurnIndex));
        }

        public void ClearCheckpoints()
        {
            _checkpoints.Clear();
            _checkpointTriggers.Clear();
            _store.Clear();
        }

        private CellState[] CopyCells()
        {
            if (_grid is GridMap concrete)
            {
                return concrete.CopyCells();
            }

            var cells = new CellState[_grid.Width * _grid.Height];
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    cells[y * _grid.Width + x] = new CellState(_grid.Get(coord));
                }
            }

            return cells;
        }

        private IReadOnlyList<GridCoord> CopyLockedCells()
        {
            if (_grid is GridMap concrete)
            {
                return concrete.CopyLockedCells();
            }

            var locked = new List<GridCoord>();
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    if (_grid.IsLocked(coord))
                    {
                        locked.Add(coord);
                    }
                }
            }

            return locked;
        }

        private UnitSnapshot[] CaptureUnits()
        {
            var all = _units.All;
            var units = new UnitSnapshot[all.Count];
            for (int i = 0; i < all.Count; i++)
            {
                var unit = all[i];
                units[i] = new UnitSnapshot(unit.Id, unit.Definition, unit.Position, unit.Hp,
                    unit.ActionPoints, unit.HasMovedThisTurn, unit.HasActedThisTurn,
                    CaptureStatuses(unit), CopyCooldowns(unit));
            }

            return units;
        }

        private static IReadOnlyList<StatusSnapshot> CaptureStatuses(UnitState unit)
        {
            var entries = unit.Statuses.Capture();
            if (entries.Length == 0)
            {
                return Array.Empty<StatusSnapshot>();
            }

            var result = new StatusSnapshot[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                result[i] = new StatusSnapshot(entries[i].Effect, entries[i].Remaining, entries[i].CanTick);
            }

            return result;
        }

        private static StatusEntry[] ToEntries(IReadOnlyList<StatusSnapshot> statuses)
        {
            if (statuses == null || statuses.Count == 0)
            {
                return Array.Empty<StatusEntry>();
            }

            var entries = new StatusEntry[statuses.Count];
            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                entries[i] = new StatusEntry(status.Effect, status.Remaining, status.CanTick);
            }

            return entries;
        }

        private static IReadOnlyDictionary<string, int> CopyCooldowns(UnitState unit)
        {
            var copy = unit.CopyCooldowns();
            return copy.Count == 0 ? new Dictionary<string, int>() : copy;
        }

        private IReadOnlyList<EnemyIntent> CaptureIntents()
        {
            if (_telegraph == null)
            {
                return Array.Empty<EnemyIntent>();
            }

            var all = _telegraph.All;
            if (all.Count == 0)
            {
                return Array.Empty<EnemyIntent>();
            }

            var list = new List<EnemyIntent>(all.Count);
            list.AddRange(all);
            return list;
        }
    }
}
