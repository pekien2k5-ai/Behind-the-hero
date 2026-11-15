namespace BTH.Core.Combat
{
    /// <summary>
    /// Công thức Clash dùng chung cho dự đoán, resolve và Duel — không RNG.
    /// </summary>
    public static class ClashMath
    {
        public static int EffectivePower(SkillDefinition skill, int bonus)
        {
            return skill == null ? 0 : skill.Power + bonus;
        }

        public static ClashResult Compare(int playerPower, int bossPower)
        {
            if (playerPower > bossPower)
            {
                return ClashResult.Win;
            }

            return playerPower == bossPower ? ClashResult.Draw : ClashResult.Lose;
        }
    }
}
