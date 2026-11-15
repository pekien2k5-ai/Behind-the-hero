namespace BTH.Core.Combat
{
    /// <summary>Kết quả một lần tính sát thương, đủ để UI diễn hoạt và status phản ứng.</summary>
    public readonly struct DamageResult
    {
        public readonly int FinalDamage;
        public readonly float Multiplier;
        public readonly bool WasStaggered;
        public readonly bool WasGuarded;
        public readonly bool WasFlanked;
        public readonly bool Killed;

        public DamageResult(int finalDamage, float multiplier, bool wasStaggered, bool wasGuarded, bool wasFlanked, bool killed)
        {
            FinalDamage = finalDamage;
            Multiplier = multiplier;
            WasStaggered = wasStaggered;
            WasGuarded = wasGuarded;
            WasFlanked = wasFlanked;
            Killed = killed;
        }

        public bool IsZero => FinalDamage <= 0;
    }
}
