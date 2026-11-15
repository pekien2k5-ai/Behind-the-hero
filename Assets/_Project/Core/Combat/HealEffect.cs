using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>Hồi HP, clamp ở MaxHp.</summary>
    public sealed class HealEffect : IGameEffect
    {
        private readonly UnitId _target;
        private readonly int _amount;

        public HealEffect(UnitId target, int amount)
        {
            _target = target;
            _amount = amount;
        }

        public int Priority => 12;

        public void Resolve(EffectContext ctx)
        {
            var target = _target.IsValid ? _target : ctx.Target;
            if (!target.IsValid || _amount <= 0 || !ctx.Units.TryGet(target, out var unit) || !unit.IsAlive)
            {
                return;
            }

            unit.SetHp(unit.Hp + _amount);
        }
    }
}
