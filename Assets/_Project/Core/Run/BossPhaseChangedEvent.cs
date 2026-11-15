using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct BossPhaseChangedEvent
    {
        public readonly UnitId Boss;
        public readonly int Phase;

        public BossPhaseChangedEvent(UnitId boss, int phase)
        {
            Boss = boss;
            Phase = phase;
        }
    }
}
