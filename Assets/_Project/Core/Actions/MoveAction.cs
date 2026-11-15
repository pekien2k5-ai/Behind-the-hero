using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Actions
{
    /// <summary>Di chuyển tới ô đích trong tầm MovePoints.</summary>
    public sealed class MoveAction : IGameAction
    {
        public MoveAction(UnitId actor, GridCoord destination)
        {
            Actor = actor;
            Destination = destination;
        }

        public string Id => "move";

        public ActionKind Kind => ActionKind.Move;

        public UnitId Actor { get; }

        public GridCoord Destination { get; }

        public GridCoord Target => Destination;
    }
}
