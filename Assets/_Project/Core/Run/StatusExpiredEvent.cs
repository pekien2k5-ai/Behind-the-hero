using BTH.Core.Combat;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct StatusExpiredEvent
    {
        public readonly UnitId Unit;
        public readonly StatusId Status;

        public StatusExpiredEvent(UnitId unit, StatusId status)
        {
            Unit = unit;
            Status = status;
        }
    }
}
