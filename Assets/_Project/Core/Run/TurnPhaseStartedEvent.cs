using BTH.Core.Turn;

namespace BTH.Core.Run
{
    public readonly struct TurnPhaseStartedEvent
    {
        public readonly TurnPhase Phase;
        public readonly int TurnIndex;

        public TurnPhaseStartedEvent(TurnPhase phase, int turnIndex)
        {
            Phase = phase;
            TurnIndex = turnIndex;
        }
    }
}
