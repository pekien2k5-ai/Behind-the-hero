using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Actions
{
    /// <summary>Dùng một chiêu trong SkillDefinition của actor.</summary>
    public sealed class SkillAction : IGameAction
    {
        public SkillAction(UnitId actor, string skillId, GridCoord targetCell)
        {
            Actor = actor;
            SkillId = skillId ?? string.Empty;
            Target = targetCell;
        }

        public string Id => "skill";

        public ActionKind Kind => ActionKind.Skill;

        public UnitId Actor { get; }

        public string SkillId { get; }

        public GridCoord Target { get; }
    }
}
