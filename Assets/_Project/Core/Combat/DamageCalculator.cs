using System;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    public sealed class DamageCalculator : IDamageCalculator
    {
        private readonly ElementMatrix _matrix;

        public DamageCalculator(ElementMatrix matrix = null)
        {
            _matrix = matrix ?? ElementMatrix.CreateDefault();
        }

        public DamageResult Compute(DamageRequest request, GameState state)
        {
            if (state == null || !state.Units.TryGet(request.Target, out var target) || !target.IsAlive)
            {
                return new DamageResult(0, 1f, false, false, false, false);
            }

            int armor = request.IgnoresArmor ? 0 : target.Definition.Armor;
            float element = _matrix.GetMultiplier(request.Element, target.Definition.NativeElement);
            float statuses = target.Statuses.DamageTakenMultiplier;
            float multiplier = CombatMath.CombineMultipliers(element, statuses);
            int damage = CombatMath.ComputePower(request.BasePower, armor, element, request.IgnoresArmor, statuses);
            bool killed = target.Hp - damage <= 0;

            return new DamageResult(damage, multiplier, target.IsStaggered, target.IsGuarding,
                target.IsFlanked, killed);
        }

        public DamageResult Apply(DamageRequest request, GameState state)
        {
            var computed = Compute(request, state);
            if (computed.FinalDamage <= 0)
            {
                return computed;
            }

            if (!state.Units.TryGet(request.Target, out var target) || !target.IsAlive)
            {
                return computed;
            }

            int hpBefore = target.Hp;
            target.SetHp(hpBefore - computed.FinalDamage);
            int dealt = hpBefore - target.Hp;
            bool killed = target.Hp <= 0;
            var result = new DamageResult(dealt, computed.Multiplier, computed.WasStaggered,
                computed.WasGuarded, computed.WasFlanked, killed);

            target.Statuses.NotifyDamaged(
                new StatusContext(target, state.Events, state.Units), result);

            state.Events?.Publish(new DamageDealtEvent(request.Source, target.Id, dealt,
                request.Element, result.WasStaggered, result.WasGuarded, result.WasFlanked,
                killed, target.Center));

            if (killed)
            {
                state.Units.Kill(target.Id);
            }

            return result;
        }
    }
}
