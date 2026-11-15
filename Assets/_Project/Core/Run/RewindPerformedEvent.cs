namespace BTH.Core.Run
{
    public readonly struct RewindPerformedEvent
    {
        public readonly SnapshotTrigger Trigger;
        public readonly int TurnIndex;

        public RewindPerformedEvent(SnapshotTrigger trigger, int turnIndex)
        {
            Trigger = trigger;
            TurnIndex = turnIndex;
        }
    }
}
