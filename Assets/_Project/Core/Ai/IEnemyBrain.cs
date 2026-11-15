using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Ai
{
    /// <summary>
    /// Bộ não địch. Content cài đặt (MeleeRusher, RangedLurker, Boss...);
    /// Plan phải thuần: cùng state → cùng intent để test tất định.
    /// </summary>
    public interface IEnemyBrain
    {
        string Id { get; }

        EnemyIntent Plan(UnitState self, GameState state);
    }
}
