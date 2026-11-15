using System.Collections.Generic;
using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>
    /// Bản đồ ô → UnitId cho unit chiếm nhiều ô (Malkor 2×2). Chỉ UnitRegistry dùng;
    /// phần còn lại hỏi qua IUnitRegistry.At / IGridMap.Get(...).Occupant.
    /// </summary>
    public sealed class OccupancyIndex
    {
        private readonly Dictionary<GridCoord, UnitId> _cells = new Dictionary<GridCoord, UnitId>();

        public int Count => _cells.Count;

        public void Add(UnitState unit)
        {
            if (unit == null)
            {
                return;
            }

            var footprint = unit.Footprint;
            for (int i = 0; i < footprint.Count; i++)
            {
                _cells[footprint[i]] = unit.Id;
            }
        }

        public void Remove(UnitState unit)
        {
            if (unit == null)
            {
                return;
            }

            var footprint = unit.Footprint;
            for (int i = 0; i < footprint.Count; i++)
            {
                if (_cells.TryGetValue(footprint[i], out var occupant) && occupant == unit.Id)
                {
                    _cells.Remove(footprint[i]);
                }
            }
        }

        public void Move(UnitId id, IReadOnlyList<GridCoord> from, IReadOnlyList<GridCoord> to)
        {
            for (int i = 0; i < from.Count; i++)
            {
                if (_cells.TryGetValue(from[i], out var occupant) && occupant == id)
                {
                    _cells.Remove(from[i]);
                }
            }

            for (int i = 0; i < to.Count; i++)
            {
                _cells[to[i]] = id;
            }
        }

        public bool TryGet(GridCoord coord, out UnitId unit)
        {
            return _cells.TryGetValue(coord, out unit);
        }

        public bool IsOccupied(GridCoord coord)
        {
            return _cells.ContainsKey(coord);
        }

        public UnitId At(GridCoord coord)
        {
            return _cells.TryGetValue(coord, out var unit) ? unit : UnitId.None;
        }

        public void Clear()
        {
            _cells.Clear();
        }
    }
}
