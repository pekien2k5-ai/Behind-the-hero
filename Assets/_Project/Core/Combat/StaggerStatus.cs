namespace BTH.Core.Combat
{
    /// <summary>
    /// Choáng: không hành động được và nhận 200% sát thương trong 1 lượt.
    /// Thường do thắng Clash.
    /// </summary>
    public sealed class StaggerStatus : IStatusEffect
    {
        public StaggerStatus(int duration = 1)
        {
            Duration = duration < 1 ? 1 : duration;
        }

        public StatusId Id => StatusIds.Stagger;

        public int Duration { get; }

        public float DamageTakenMultiplier => 2f;

        public bool PreventsAction => true;

        public bool PreventsMovement => true;

        public void OnApply(StatusContext ctx)
        {
        }

        public void OnTurnStart(StatusContext ctx)
        {
        }

        public void OnTurnEnd(StatusContext ctx)
        {
        }

        public void OnRemoved(StatusContext ctx)
        {
        }

        public void OnDamaged(StatusContext ctx, DamageResult result)
        {
        }
    }
}
