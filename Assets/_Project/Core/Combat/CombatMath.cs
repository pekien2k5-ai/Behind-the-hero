using System;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Công thức sát thương tập trung một chỗ: power × hệ → trừ giáp → nhân status.
    /// Làm tròn .5 lên, luôn tối thiểu 1 nếu gốc dương.
    /// </summary>
    public static class CombatMath
    {
        public static int ComputePower(int basePower, int armor, float elementMultiplier, bool ignoresArmor,
            float statusMultiplier)
        {
            if (basePower <= 0)
            {
                return 0;
            }

            float power = basePower * elementMultiplier;
            if (!ignoresArmor && armor > 0)
            {
                power -= armor;
            }

            if (power < 1f)
            {
                power = 1f;
            }

            power *= statusMultiplier;
            return Math.Max(1, (int)Math.Round(power, MidpointRounding.AwayFromZero));
        }

        public static float CombineMultipliers(float elementMultiplier, float statusMultiplier)
        {
            return elementMultiplier * statusMultiplier;
        }
    }
}
