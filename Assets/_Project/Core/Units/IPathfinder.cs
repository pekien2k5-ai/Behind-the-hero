using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>
    /// Tìm đường và loang vùng đi. Chỉ đọc IGridMap; không sửa state.
    /// </summary>
    public interface IPathfinder
    {
        PathResult FindPath(PathRequest request);

        /// <summary>Loang từ start trong maxCost điểm; không gồm ô xuất phát.</summary>
        ReachableSet ComputeReachable(GridCoord start, MoveProfile profile, int maxCost, UnitId self = default);
    }
}
