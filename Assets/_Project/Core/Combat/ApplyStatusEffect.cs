using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>Gắn status lên unit (choáng, độc, flanked, root, guard).</summary>
    public sealed class ApplyStatusEffect : IGameEffect
    {
        private readonly UnitId _target;
        private readonly IStatusEffect _effect;

        public ApplyStatusEffect(UnitId target, IStatusEffect effect)
        {
            _target = target;
            _effect = effect;
        }

        public int Priority => 20;

        public void Resolve(EffectContext ctx)
        {
            var target = _target.IsValid ? _target : ctx.Target;
            if (!target.IsValid || _effect == null ||
                !ctx.Units.TryGet(target, out var unit) || !unit.IsAlive)
            {
                return;
            }

            unit.Statuses.Add(_effect, new StatusContext(unit, ctx.Events, ctx.Units));
        }
    }
}
