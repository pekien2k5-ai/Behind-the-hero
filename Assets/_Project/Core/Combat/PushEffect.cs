using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Đẩy lùi theo 1 trong 4 hướng, từng ô một; dừng khi gặp tường/unit khác/ngoài lưới.
    /// Không gây sát thương va chạm (nằm ngoài phạm vi Core v1).
    /// </summary>
    public sealed class PushEffect : IGameEffect
    {
        private readonly UnitId _target;
        private readonly GridCoord _direction;
        private readonly int _distance;

        public PushEffect(UnitId target, GridCoord direction, int distance)
        {
            _target = target;
            _direction = direction;
            _distance = distance;
        }

        public int Priority => 30;

        public void Resolve(EffectContext ctx)
        {
            var target = _target.IsValid ? _target : ctx.Target;
            if (!target.IsValid || _distance <= 0 ||
                !ctx.Units.TryGet(target, out var unit) || !unit.IsAlive)
            {
                return;
            }

            var dir = Normalize(_direction);
            if (dir == GridCoord.None)
            {
                return;
            }

            for (int i = 0; i < _distance; i++)
            {
                var next = unit.Position + dir;
                if (!ctx.State.Grid.InBounds(next) ||
                    !ctx.Units.CanPlace(unit.Definition, next, unit.Id))
                {
                    break;
                }

                var from = unit.Position;
                ctx.Units.MoveTo(unit.Id, next);
                ctx.Events?.Publish(new UnitMovedEvent(unit.Id, from, next, 0));
            }
        }

        private static GridCoord Normalize(GridCoord direction)
        {
            if (direction.X != 0 && direction.Y == 0)
            {
                return new GridCoord(direction.X > 0 ? 1 : -1, 0);
            }

            if (direction.Y != 0 && direction.X == 0)
            {
                return new GridCoord(0, direction.Y > 0 ? 1 : -1);
            }

            return GridCoord.None;
        }
    }
}
