namespace BTH.Core.Turn
{
    /// <summary>
    /// Điểm hành động của một unit trong lượt: 2 AP = 1 di chuyển + 1 hành động.
    /// Bất biến — mọi thay đổi đi qua ITurnManager.TrySpend.
    /// </summary>
    public readonly struct ActionPoint
    {
        public ActionPoint(int current, int max)
        {
            Current = current < 0 ? 0 : current;
            Max = max < 0 ? 0 : max;
        }

        public int Current { get; }

        public int Max { get; }

        public bool IsEmpty => Current <= 0;

        public bool CanSpend(int cost)
        {
            return cost > 0 && Current >= cost;
        }

        public ActionPoint Spend(int cost)
        {
            return new ActionPoint(Current - cost, Max);
        }

        public override string ToString()
        {
            return Current + "/" + Max;
        }
    }
}
