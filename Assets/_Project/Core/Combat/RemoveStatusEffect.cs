using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>Gỡ status (dùng khi boss thanh tẩy, hoặc Guard bị phá).</summary>
    public sealed class RemoveStatusEffect : IGameEffect
    {
        private readonly UnitId _target;
        private readonly StatusId _status;

        public RemoveStatusEffect(UnitId target, StatusId status)
        {
            _target = target;
            _status = status;
        }

        public int Priority => 21;

        public void Resolve(EffectContext ctx)
        {
            var target = _target.IsValid ? _target : ctx.Target;
            if (!target.IsValid || !ctx.Units.TryGet(target, out var unit))
            {
                return;
            }

            unit.Statuses.Remove(_status);
        }
    }
}
