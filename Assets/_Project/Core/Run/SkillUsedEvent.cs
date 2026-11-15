using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    public readonly struct SkillUsedEvent
    {
        public readonly UnitId Unit;
        public readonly string SkillId;
        public readonly GridCoord Target;

        public SkillUsedEvent(UnitId unit, string skillId, GridCoord target)
        {
            Unit = unit;
            SkillId = skillId;
            Target = target;
        }
    }
}
