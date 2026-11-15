using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Actions
{
    /// <summary>
    /// Yêu cầu hành động từ UI/AI. Là dữ liệu thuần, không tham chiếu scene;
    /// validator và executor là nơi duy nhất diễn giải nó.
    /// </summary>
    public readonly struct ActionRequest
    {
        public ActionRequest(UnitId actor, ActionKind kind, GridCoord targetCell, string skillId,
            GridCoord moveTo)
        {
            Actor = actor;
            Kind = kind;
            TargetCell = targetCell;
            SkillId = skillId ?? string.Empty;
            MoveTo = moveTo;
        }

        public UnitId Actor { get; }

        public ActionKind Kind { get; }

        public GridCoord TargetCell { get; }

        public string SkillId { get; }

        public GridCoord MoveTo { get; }

        public static ActionRequest Move(UnitId actor, GridCoord destination)
        {
            return new ActionRequest(actor, ActionKind.Move, destination, string.Empty, destination);
        }

        public static ActionRequest Attack(UnitId actor, GridCoord target)
        {
            return new ActionRequest(actor, ActionKind.Attack, target, string.Empty, GridCoord.None);
        }

        public static ActionRequest Skill(UnitId actor, string skillId, GridCoord target)
        {
            return new ActionRequest(actor, ActionKind.Skill, target, skillId, GridCoord.None);
        }

        public static ActionRequest Guard(UnitId actor)
        {
            return new ActionRequest(actor, ActionKind.Guard, GridCoord.None, string.Empty, GridCoord.None);
        }

        public static ActionRequest EndTurn(UnitId actor)
        {
            return new ActionRequest(actor, ActionKind.EndTurn, GridCoord.None, string.Empty, GridCoord.None);
        }

        public static ActionRequest ClashSkill(UnitId actor, string skillId)
        {
            return new ActionRequest(actor, ActionKind.ClashSkill, GridCoord.None, skillId, GridCoord.None);
        }

        public override string ToString()
        {
            return Kind + " by " + Actor.Value;
        }
    }
}
