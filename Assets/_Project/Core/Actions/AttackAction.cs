using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Actions
{
    /// <summary>Đánh thường bằng vũ khí, tầm 1, tốn 1 AP.</summary>
    public sealed class AttackAction : IGameAction
    {
        public AttackAction(UnitId actor, GridCoord targetCell)
        {
            Actor = actor;
            Target = targetCell;
        }

        public string Id => "attack";

        public ActionKind Kind => ActionKind.Attack;

        public UnitId Actor { get; }

        public GridCoord Target { get; }
    }
}
