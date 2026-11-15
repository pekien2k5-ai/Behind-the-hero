namespace BTH.Core.Run
{
    /// <summary>Một vòng Duel mới bắt đầu: boss đã công bố chiêu và uy lực.</summary>
    public readonly struct DuelRoundStartedEvent
    {
        public readonly int Round;
        public readonly string BossSkillId;
        public readonly int BossPower;

        public DuelRoundStartedEvent(int round, string bossSkillId, int bossPower)
        {
            Round = round;
            BossSkillId = bossSkillId;
            BossPower = bossPower;
        }
    }
}
