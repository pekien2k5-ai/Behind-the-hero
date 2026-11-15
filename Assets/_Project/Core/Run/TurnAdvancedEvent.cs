namespace BTH.Core.Run
{
    public readonly struct TurnAdvancedEvent
    {
        public readonly int TurnIndex;

        public TurnAdvancedEvent(int turnIndex)
        {
            TurnIndex = turnIndex;
        }
    }
}
