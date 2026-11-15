using BTH.Core.Combat;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct StatusAppliedEvent
    {
        public readonly UnitId Unit;
        public readonly StatusId Status;
        public readonly int Duration;

        public StatusAppliedEvent(UnitId unit, StatusId status, int duration)
        {
            Unit = unit;
            Status = status;
            Duration = duration;
        }
    }
}
