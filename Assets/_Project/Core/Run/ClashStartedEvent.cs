using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct ClashStartedEvent
    {
        public readonly UnitId Boss;
        public readonly string BossSkillId;

        public ClashStartedEvent(UnitId boss, string bossSkillId)
        {
            Boss = boss;
            BossSkillId = bossSkillId;
        }
    }
}
