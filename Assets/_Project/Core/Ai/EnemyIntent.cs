using System.Collections.Generic;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Ai
{
    /// <summary>
    /// Ý định đã công bố của một unit địch. Bất biến sau khi phát IntentDeclared —
    /// người chơi đọc danger cells để né, AI không được đổi ý giữa lượt.
    /// </summary>
    public sealed class EnemyIntent
    {
        public EnemyIntent(UnitId unit, string skillId, GridCoord targetCell,
            IReadOnlyList<GridCoord> dangerCells, IReadOnlyList<IGameEffect> effects, int priority)
        {
            Unit = unit;
            SkillId = skillId ?? string.Empty;
            TargetCell = targetCell;
            DangerCells = dangerCells ?? new List<GridCoord>();
            Effects = effects ?? new List<IGameEffect>();
            Priority = priority;
        }

        public UnitId Unit { get; }

        public string SkillId { get; }

        public GridCoord TargetCell { get; }

        public IReadOnlyList<GridCoord> DangerCells { get; }

        /// <summary>Effect dự kiến; executor dựng lại từ skill để tránh giữ state cũ.</summary>
        public IReadOnlyList<IGameEffect> Effects { get; }

        public int Priority { get; }

        public bool IsEmpty => !Unit.IsValid;

        public static EnemyIntent Empty(UnitId unit)
        {
            return new EnemyIntent(unit, string.Empty, GridCoord.None, null, null, 0);
        }
    }
}
