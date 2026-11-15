using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct UnitDiedEvent
    {
        public readonly UnitId Unit;
        public readonly GridCoord At;

        public UnitDiedEvent(UnitId unit, GridCoord at)
        {
            Unit = unit;
            At = at;
        }
    }
}
