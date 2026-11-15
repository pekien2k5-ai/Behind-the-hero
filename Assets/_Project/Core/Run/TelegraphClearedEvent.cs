namespace BTH.Core.Run
{
    public readonly struct TelegraphClearedEvent
    {
        public readonly int Count;

        public TelegraphClearedEvent(int count)
        {
            Count = count;
        }
    }
}
