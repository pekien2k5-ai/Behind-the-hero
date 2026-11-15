namespace BTH.Core.Combat
{
    /// <summary>
    /// Bị hở sườn: đòn đánh kế tiếp nhận 150% sát thương rồi trạng thái tan.
    /// </summary>
    public sealed class FlankedStatus : IStatusEffect
    {
        public FlankedStatus(int duration = 2)
        {
            Duration = duration < 1 ? 1 : duration;
        }

        public StatusId Id => StatusIds.Flanked;

        public int Duration { get; }

        public float DamageTakenMultiplier => 1.5f;

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
            if (ctx.Owner != null)
            {
                ctx.Owner.Statuses.Remove(StatusIds.Flanked);
            }
        }
    }
}
