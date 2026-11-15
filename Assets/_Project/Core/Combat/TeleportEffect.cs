using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>Dịch chuyển tức thời tới ô hợp lệ (dùng cho ShadowDash/TemporalStep).</summary>
    public sealed class TeleportEffect : IGameEffect
    {
        private readonly UnitId _target;
        private readonly GridCoord _destination;

        public TeleportEffect(UnitId target, GridCoord destination)
        {
            _target = target;
            _destination = destination;
        }

        public int Priority => 35;

        public void Resolve(EffectContext ctx)
        {
            var target = _target.IsValid ? _target : ctx.Target;
            if (!target.IsValid || !_destination.IsValid ||
                !ctx.Units.TryGet(target, out var unit) || !unit.IsAlive)
            {
                return;
            }

            if (!ctx.State.Grid.InBounds(_destination) ||
                !ctx.Units.CanPlace(unit.Definition, _destination, unit.Id))
            {
                return;
            }

            var from = unit.Position;
            ctx.Units.MoveTo(unit.Id, _destination);
            ctx.Events?.Publish(new UnitMovedEvent(unit.Id, from, _destination, 0));
        }
    }
}
