using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct ApChangedEvent
    {
        public readonly UnitId Unit;
        public readonly int ActionPoints;

        public ApChangedEvent(UnitId unit, int actionPoints)
        {
            Unit = unit;
            ActionPoints = actionPoints;
        }
    }
}
