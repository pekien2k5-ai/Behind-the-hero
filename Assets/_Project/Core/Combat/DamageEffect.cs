using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>Gây sát thương lên một unit qua DamageCalculator (đã áp giáp/hệ/status).</summary>
    public sealed class DamageEffect : IGameEffect
    {
        private readonly UnitId _target;
        private readonly int _power;
        private readonly ElementId _element;
        private readonly bool _isSkill;
        private readonly bool _ignoresArmor;

        public DamageEffect(UnitId target, int power, ElementId element, bool isSkill = false,
            bool ignoresArmor = false)
        {
            _target = target;
            _power = power;
            _element = element;
            _isSkill = isSkill;
            _ignoresArmor = ignoresArmor;
        }

        public int Priority => 10;

        public void Resolve(EffectContext ctx)
        {
            var target = _target.IsValid ? _target : ctx.Target;
            int power = _power > 0 ? _power : ctx.Power;
            if (!target.IsValid || power <= 0)
            {
                return;
            }

            var request = new DamageRequest(ctx.Source, target, power, _element, _isSkill,
                _ignoresArmor, ctx.Origin);

            ctx.State.Damage.Apply(request, ctx.State);
        }
    }
}
