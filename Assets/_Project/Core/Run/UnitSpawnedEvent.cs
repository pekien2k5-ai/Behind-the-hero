using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct UnitSpawnedEvent
    {
        public readonly UnitId Unit;
        public readonly GridCoord Position;

        public UnitSpawnedEvent(UnitId unit, GridCoord position)
        {
            Unit = unit;
            Position = position;
        }
    }
}
