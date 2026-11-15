using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Ngữ cảnh chạy effect: state, hàng đợi, nguồn, mục tiêu, skill, RNG.
    /// Effect không tự tìm unit qua FindObjectOfType — mọi thứ đến từ đây.
    /// </summary>
    public sealed class EffectContext
    {
        public EffectContext(GameState state, EffectQueue queue, UnitId source, GridCoord origin,
            UnitId target, GridCoord targetCell, SkillDefinition skill, int power, ElementId element)
        {
            State = state;
            Queue = queue;
            Source = source;
            Origin = origin;
            Target = target;
            TargetCell = targetCell;
            Skill = skill;
            Power = power;
            Element = element;
        }

        public GameState State { get; }

        public EffectQueue Queue { get; }

        public IUnitRegistry Units => State.Units;

        public IEventBus Events => State.Events;

        public IRandomSource Random => State.Random;

        public UnitId Source { get; }

        public GridCoord Origin { get; }

        public UnitId Target { get; }

        public GridCoord TargetCell { get; }

        public SkillDefinition Skill { get; }

        public int Power { get; set; }

        public ElementId Element { get; set; }

        public UnitState TargetUnit
        {
            get
            {
                return Target.IsValid && Units.TryGet(Target, out var unit) ? unit : null;
            }
        }

        public GridCoord ResolvedCell
        {
            get
            {
                if (TargetCell.IsValid)
                {
                    return TargetCell;
                }

                var unit = TargetUnit;
                return unit != null ? unit.Center : Origin;
            }
        }

        public UnitState UnitAt(GridCoord cell)
        {
            return Units.At(cell);
        }
    }
}
