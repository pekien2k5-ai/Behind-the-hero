namespace BTH.Core.Run
{
    public readonly struct SnapshotPushedEvent
    {
        public readonly SnapshotTrigger Trigger;
        public readonly int TurnIndex;

        public SnapshotPushedEvent(SnapshotTrigger trigger, int turnIndex)
        {
            Trigger = trigger;
            TurnIndex = turnIndex;
        }
    }
}
