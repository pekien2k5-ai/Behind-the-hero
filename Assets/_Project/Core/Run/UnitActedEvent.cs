using BTH.Core.Actions;
using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct UnitActedEvent
    {
        public readonly UnitId Unit;
        public readonly ActionKind Kind;
        public readonly GridCoord Target;

        public UnitActedEvent(UnitId unit, ActionKind kind, GridCoord target)
        {
            Unit = unit;
            Kind = kind;
            Target = target;
        }
    }
}
