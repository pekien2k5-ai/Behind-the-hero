namespace BTH.Core.Combat
{
    /// <summary>Trói chân: không thể di chuyển nhưng vẫn hành động được.</summary>
    public sealed class RootStatus : IStatusEffect
    {
        public RootStatus(int duration = 1)
        {
            Duration = duration < 1 ? 1 : duration;
        }

        public StatusId Id => StatusIds.Root;

        public int Duration { get; }

        public float DamageTakenMultiplier => 1f;

        public bool PreventsAction => false;

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
