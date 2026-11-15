namespace BTH.Core.Run
{
    public readonly struct StageEndedEvent
    {
        public readonly StageResult Result;
        public readonly int TurnIndex;

        public StageEndedEvent(StageResult result, int turnIndex)
        {
            Result = result;
            TurnIndex = turnIndex;
        }
    }
}
