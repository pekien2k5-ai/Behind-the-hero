using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>Nút trong hàng đợi A*; so sánh tất định khi f bằng nhau.</summary>
    internal readonly struct PathNode
    {
        public readonly GridCoord Coord;
        public readonly int Cost;
        public readonly int Estimate;
        public readonly int Index;

        public PathNode(GridCoord coord, int cost, int estimate, int index)
        {
            Coord = coord;
            Cost = cost;
            Estimate = estimate;
            Index = index;
        }

        public int Total => Cost + Estimate;

        public bool Before(in PathNode other)
        {
            if (Total != other.Total)
            {
                return Total < other.Total;
            }

            if (Estimate != other.Estimate)
            {
                return Estimate < other.Estimate;
            }

            return Index < other.Index;
        }
    }
}
