using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct UnitMovedEvent
    {
        public readonly UnitId Unit;
        public readonly GridCoord From;
        public readonly GridCoord To;

        /// <summary>Số điểm di chuyển đã tiêu (0 với teleport/push).</summary>
        public readonly int Cost;

        public UnitMovedEvent(UnitId unit, GridCoord from, GridCoord to, int cost)
        {
            Unit = unit;
            From = from;
            To = to;
            Cost = cost;
        }
    }
}
