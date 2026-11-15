using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct HazardTriggeredEvent
    {
        public readonly GridCoord Coord;
        public readonly UnitId Unit;
        public readonly int Damage;

        public HazardTriggeredEvent(GridCoord coord, UnitId unit, int damage)
        {
            Coord = coord;
            Unit = unit;
            Damage = damage;
        }
    }
}
