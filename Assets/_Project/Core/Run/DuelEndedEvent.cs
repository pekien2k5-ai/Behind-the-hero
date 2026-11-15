using BTH.Core.Combat;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    /// <summary>Duel kết thúc: Winner là bên còn sống (None nếu hoà cả hai cùng chết).</summary>
    public readonly struct DuelEndedEvent
    {
        public readonly ClashResult LastResult;
        public readonly UnitId Winner;

        public DuelEndedEvent(ClashResult lastResult, UnitId winner)
        {
            LastResult = lastResult;
            Winner = winner;
        }
    }
}
