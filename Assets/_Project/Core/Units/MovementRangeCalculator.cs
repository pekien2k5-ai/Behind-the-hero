using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>
    /// Tiện ích loang vùng đi cho UI (tô ô xanh) và AI: bọc IPathfinder để
    /// không nơi nào tự viết BFS riêng.
    /// </summary>
    public static class MovementRangeCalculator
    {
        public static ReachableSet Compute(IPathfinder pathfinder, IGridMap grid, GridCoord start,
            MoveProfile profile, int movePoints, UnitId self = default)
        {
            if (pathfinder == null || grid == null || !grid.InBounds(start))
            {
                return new ReachableSet(start, null);
            }

            return pathfinder.ComputeReachable(start, profile, movePoints, self);
        }
    }
}
