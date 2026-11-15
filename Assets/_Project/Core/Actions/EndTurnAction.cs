using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Actions
{
    /// <summary>Kết thúc lượt người chơi; EndTurn của ITurnManager làm phần tick thật.</summary>
    public sealed class EndTurnAction : IGameAction
    {
        public EndTurnAction(UnitId actor)
        {
            Actor = actor;
        }

        public string Id => "end-turn";

        public ActionKind Kind => ActionKind.EndTurn;

        public UnitId Actor { get; }

        public GridCoord Target => GridCoord.None;
    }
}
