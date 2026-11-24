using System;
using System.Collections.Generic;
using BTH.Core.Grid;
using BTH.Core.Run;

namespace BTH.Core.Units
{
    /// <summary>
    /// Hiện thực mặc định của IUnitRegistry. Giữ thứ tự spawn ổn định để AI và
    /// snapshot tất định.
    /// </summary>
    public sealed class UnitRegistry : IUnitRegistry
    {
        private readonly IGridMap _grid;
        private readonly IEventBus _events;
        private readonly List<UnitState> _all = new List<UnitState>();
        private readonly Dictionary<UnitId, UnitState> _byId = new Dictionary<UnitId, UnitState>();
        private readonly OccupancyIndex _occupancy = new OccupancyIndex();
        private int _nextId = 1;

        public UnitRegistry(IGridMap grid, IEventBus events = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _events = events;
        }

        public IReadOnlyList<UnitState> All => _all;

        internal Action<UnitState, GridCoord> OnUnitEnteredCell { get; set; }

        public UnitState Player => FirstByFaction(UnitFaction.Player);

        public UnitState Boss
        {
            get
            {
                for (int i = 0; i < _all.Count; i++)
                {
                    if (_all[i].IsBoss)
                    {
                        return _all[i];
                    }
                }

                return null;
            }
        }

        public IReadOnlyList<UnitState> Alive(UnitFaction faction)
        {
            var result = new List<UnitState>();
            for (int i = 0; i < _all.Count; i++)
            {
                var unit = _all[i];
                if (unit.IsAlive && unit.Faction == faction)
                {
                    result.Add(unit);
                }
            }

            return result;
        }

        public UnitState Get(UnitId id)
        {
            if (!_byId.TryGetValue(id, out var unit))
            {
                throw new KeyNotFoundException("Không có unit " + id);
            }

            return unit;
        }

        public bool TryGet(UnitId id, out UnitState unit)
        {
            return _byId.TryGetValue(id, out unit);
        }

        public UnitState At(GridCoord coord)
        {
            return _occupancy.TryGet(coord, out var id) && _byId.TryGetValue(id, out var unit)
                ? unit
                : null;
        }

        public bool IsOccupied(GridCoord coord)
        {
            return _occupancy.IsOccupied(coord);
        }

        public bool CanPlace(UnitDefinition definition, GridCoord anchor)
        {
            return CanPlace(definition, anchor, UnitId.None);
        }

        public bool CanPlace(UnitDefinition definition, GridCoord anchor, UnitId ignore)
        {
            return CanPlaceInternal(definition, anchor, ignore);
        }

        public UnitState Add(UnitDefinition definition, GridCoord anchor)
        {
            if (!CanPlaceInternal(definition, anchor, UnitId.None))
            {
                throw new InvalidOperationException("Không đặt được " + definition?.Id + " tại " + anchor);
            }

            var id = new UnitId(_nextId++);
            var unit = new UnitState(id, definition, anchor);
            _all.Add(unit);
            _byId[id] = unit;
            _occupancy.Add(unit);
            ApplyOccupancy(unit);
            _events?.Publish(new UnitSpawnedEvent(unit.Id, unit.Position));
            return unit;
        }

        public void Remove(UnitId id)
        {
            if (!_byId.TryGetValue(id, out var unit))
            {
                return;
            }

            ClearOccupancy(unit);
            _occupancy.Remove(unit);
            _all.Remove(unit);
            _byId.Remove(id);
        }

        public void MoveTo(UnitId id, GridCoord anchor)
        {
            var unit = Get(id);
            if (!CanPlaceInternal(unit.Definition, anchor, id))
            {
                throw new InvalidOperationException("Không dời được " + id + " tới " + anchor);
            }

            var oldPosition = unit.Position;
            var oldCells = new List<GridCoord>(unit.Footprint);
            ClearOccupancyCells(oldCells);
            unit.SetPosition(anchor);
            _occupancy.Move(id, oldCells, unit.Footprint);
            ApplyOccupancy(unit);
            if (oldPosition != anchor)
            {
                OnUnitEnteredCell?.Invoke(unit, anchor);
            }
        }

        public IReadOnlyList<UnitState> UnitsIn(IReadOnlyList<GridCoord> cells)
        {
            var result = new List<UnitState>();
            if (cells == null)
            {
                return result;
            }

            var seen = new HashSet<UnitId>();
            for (int i = 0; i < cells.Count; i++)
            {
                if (_occupancy.TryGet(cells[i], out var id) && seen.Add(id) && _byId.TryGetValue(id, out var unit))
                {
                    result.Add(unit);
                }
            }

            return result;
        }

        public IReadOnlyList<UnitState> AdjacentTo(GridCoord coord)
        {
            var result = new List<UnitState>();
            for (int i = 0; i < _all.Count; i++)
            {
                var unit = _all[i];
                if (!unit.IsAlive)
                {
                    continue;
                }

                var footprint = unit.Footprint;
                for (int j = 0; j < footprint.Count; j++)
                {
                    if (GridCoord.Manhattan(footprint[j], coord) == 1)
                    {
                        result.Add(unit);
                        break;
                    }
                }
            }

            return result;
        }

        public void Kill(UnitId id)
        {
            if (!_byId.TryGetValue(id, out var unit) || unit.DeathPublished)
            {
                return;
            }

            unit.SetHp(0);
            unit.DeathPublished = true;
            ClearOccupancy(unit);
            _occupancy.Remove(unit);
            _events?.Publish(new UnitDiedEvent(unit.Id, unit.Center));
        }

        /// <summary>Xoá sạch không phát event — SnapshotService dùng khi restore.</summary>
        internal void ClearSilently()
        {
            _all.Clear();
            _byId.Clear();
            _occupancy.Clear();
            _nextId = 1;
        }

        /// <summary>Thêm lại unit với đúng Id/HP/AP khi restore; không phát event.</summary>
        internal UnitState RestoreAdd(UnitDefinition definition, GridCoord anchor, int id, int hp, int actionPoints)
        {
            var unitId = new UnitId(id);
            var unit = new UnitState(unitId, definition, anchor);
            unit.SetHp(hp);
            unit.ActionPoints = actionPoints;
            unit.DeathPublished = !unit.IsAlive;
            _all.Add(unit);
            _byId[unitId] = unit;
            if (unit.IsAlive)
            {
                // Xác unit chết không được chiếm ô: nếu add vào occupancy, các ô trống
                // sẽ không thể đặt quân sau khi rewind/restore.
                _occupancy.Add(unit);
            }
            if (id >= _nextId)
            {
                _nextId = id + 1;
            }

            return unit;
        }

        private UnitState FirstByFaction(UnitFaction faction)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].Faction == faction)
                {
                    return _all[i];
                }
            }

            return null;
        }

        private bool CanPlaceInternal(UnitDefinition definition, GridCoord anchor, UnitId ignore)
        {
            if (definition == null)
            {
                return false;
            }

            var cells = GridShapes.Footprint(anchor, definition.SizeX, definition.SizeY);
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (!_grid.InBounds(cell))
                {
                    return false;
                }

                var state = _grid.Get(cell);
                if (state.BlocksMove && !(definition.Move.CanFly && !state.BlocksLineOfSight))
                {
                    return false;
                }

                if (_occupancy.TryGet(cell, out var occupant) && occupant != ignore)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyOccupancy(UnitState unit)
        {
            var footprint = unit.Footprint;
            for (int i = 0; i < footprint.Count; i++)
            {
                _grid.SetOccupant(footprint[i], unit.Id);
            }
        }

        private void ClearOccupancy(UnitState unit)
        {
            ClearOccupancyCells(unit.Footprint);
        }

        private void ClearOccupancyCells(IReadOnlyList<GridCoord> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (_grid.InBounds(cells[i]) && _grid.Get(cells[i]).Occupant.IsValid)
                {
                    _grid.SetOccupant(cells[i], UnitId.None);
                }
            }
        }
    }
}
