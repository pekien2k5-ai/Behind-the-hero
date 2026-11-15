using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct DamageDealtEvent
    {
        public readonly UnitId Source;
        public readonly UnitId Target;
        public readonly int Amount;
        public readonly ElementId Element;
        public readonly bool WasStaggered;
        public readonly bool WasGuarded;
        public readonly bool WasFlanked;
        public readonly bool Killed;
        public readonly GridCoord At;

        public DamageDealtEvent(UnitId source, UnitId target, int amount, ElementId element,
            bool wasStaggered, bool wasGuarded, bool wasFlanked, bool killed, GridCoord at)
        {
            Source = source;
            Target = target;
            Amount = amount;
            Element = element;
            WasStaggered = wasStaggered;
            WasGuarded = wasGuarded;
            WasFlanked = wasFlanked;
            Killed = killed;
            At = at;
        }
    }
}
