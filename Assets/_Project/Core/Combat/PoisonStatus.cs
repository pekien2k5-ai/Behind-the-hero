using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Độc: mất HP mỗi lần EndTurn của chủ thể. Sát thương bỏ qua giáp và hệ số hệ.
    /// </summary>
    public sealed class PoisonStatus : IStatusEffect
    {
        private readonly int _damagePerTurn;

        public PoisonStatus(int damagePerTurn, int duration)
        {
            _damagePerTurn = damagePerTurn < 0 ? 0 : damagePerTurn;
            Duration = duration < 1 ? 1 : duration;
        }

        public StatusId Id => StatusIds.Poison;

        public int Duration { get; }

        public int DamagePerTurn => _damagePerTurn;

        public float DamageTakenMultiplier => 1f;

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
            var owner = ctx.Owner;
            if (owner == null || !owner.IsAlive || _damagePerTurn <= 0)
            {
                return;
            }

            int before = owner.Hp;
            owner.SetHp(before - _damagePerTurn);
            int dealt = before - owner.Hp;
            bool killed = owner.Hp <= 0;

            ctx.Events?.Publish(new DamageDealtEvent(UnitId.None, owner.Id, dealt, ElementId.Necrotic,
                false, false, false, killed, owner.Position));

            if (killed)
            {
                ctx.Units?.Kill(owner.Id);
            }
        }

        public void OnRemoved(StatusContext ctx)
        {
        }

        public void OnDamaged(StatusContext ctx, DamageResult result)
        {
        }
    }
}
