using BTH.Core.Combat;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct ClashResolvedEvent
    {
        public readonly UnitId Boss;
        public readonly string PlayerSkillId;
        public readonly ClashResult Result;
        public readonly int PlayerPower;
        public readonly int BossPower;

        public ClashResolvedEvent(UnitId boss, string playerSkillId, ClashResult result, int playerPower, int bossPower)
        {
            Boss = boss;
            PlayerSkillId = playerSkillId;
            Result = result;
            PlayerPower = playerPower;
            BossPower = bossPower;
        }
    }
}
