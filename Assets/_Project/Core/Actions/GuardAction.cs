using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Actions
{
    /// <summary>Thủ: giảm 80% sát thương nhận trong lượt, tốn 1 AP.</summary>
    public sealed class GuardAction : IGameAction
    {
        public GuardAction(UnitId actor)
        {
            Actor = actor;
        }

        public string Id => "guard";

        public ActionKind Kind => ActionKind.Guard;

        public UnitId Actor { get; }

        public GridCoord Target => GridCoord.None;
    }
}
