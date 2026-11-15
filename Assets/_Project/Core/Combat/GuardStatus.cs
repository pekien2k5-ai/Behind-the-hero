namespace BTH.Core.Combat
{
    /// <summary>Thủ: giảm 80% sát thương nhận vào (hệ số 0.2) trong lượt.</summary>
    public sealed class GuardStatus : IStatusEffect
    {
        public GuardStatus(int duration = 1)
        {
            Duration = duration < 1 ? 1 : duration;
        }

        public StatusId Id => StatusIds.Guard;

        public int Duration { get; }

        public float DamageTakenMultiplier => 0.2f;

        public bool PreventsAction => false;

        public bool PreventsMovement => false;

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
