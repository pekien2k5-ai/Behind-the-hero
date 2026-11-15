using System.Collections.Generic;
using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>Kết quả loang vùng đi được trong N điểm di chuyển.</summary>
    public sealed class ReachableSet
    {
        private readonly Dictionary<GridCoord, int> _costs;
        private readonly List<GridCoord> _cells;

        public ReachableSet(GridCoord origin, Dictionary<GridCoord, int> costs)
        {
            Origin = origin;
            _costs = costs ?? new Dictionary<GridCoord, int>();
            _cells = new List<GridCoord>(_costs.Keys);
            _cells.Sort(CompareCell);
        }

        public GridCoord Origin { get; }

        public int Count => _cells.Count;

        /// <summary>Thứ tự ổn định (theo Y rồi X) để AI và test tất định.</summary>
        public IReadOnlyList<GridCoord> Cells => _cells;

        public IReadOnlyDictionary<GridCoord, int> Costs => _costs;

        public bool Contains(GridCoord coord)
        {
            return _costs.ContainsKey(coord);
        }

        public int CostTo(GridCoord coord)
        {
            return _costs.TryGetValue(coord, out var cost) ? cost : -1;
        }

        private static int CompareCell(GridCoord a, GridCoord b)
        {
            int byY = a.Y.CompareTo(b.Y);
            return byY != 0 ? byY : a.X.CompareTo(b.X);
        }
    }
}
