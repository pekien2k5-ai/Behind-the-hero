namespace BTH.Core.Run
{
    public readonly struct ArenaLockedEvent
    {
        public readonly int CellCount;

        public ArenaLockedEvent(int cellCount)
        {
            CellCount = cellCount;
        }
    }
}
