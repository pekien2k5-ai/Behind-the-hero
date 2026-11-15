namespace BTH.Core.Run
{
    /// <summary>Bắt đầu một màn: UI dựng lưới, spawn unit theo StageDefinition.</summary>
    public readonly struct StageStartedEvent
    {
        public readonly string StageId;
        public readonly int TurnLimit;

        public StageStartedEvent(string stageId, int turnLimit)
        {
            StageId = stageId;
            TurnLimit = turnLimit;
        }
    }
}
