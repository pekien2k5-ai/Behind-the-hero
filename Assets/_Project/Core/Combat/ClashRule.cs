namespace BTH.Core.Combat
{
    /// <summary>Một luật cộng điểm cho cặp chiêu player–boss cụ thể.</summary>
    public readonly struct ClashRule
    {
        public ClashRule(string playerSkillId, string bossSkillId, int bonus)
        {
            PlayerSkillId = playerSkillId;
            BossSkillId = bossSkillId;
            Bonus = bonus;
        }

        public string PlayerSkillId { get; }

        public string BossSkillId { get; }

        public int Bonus { get; }
    }
}
