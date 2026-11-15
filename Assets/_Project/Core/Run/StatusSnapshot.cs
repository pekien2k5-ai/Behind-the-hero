namespace BTH.Core.Run
{
    /// <summary>Một status được chụp lại: giữ nguyên effect và đồng hồ đếm lượt.</summary>
    public readonly struct StatusSnapshot
    {
        public StatusSnapshot(Combat.IStatusEffect effect, int remaining, bool canTick)
        {
            Effect = effect;
            Remaining = remaining;
            CanTick = canTick;
        }

        public Combat.IStatusEffect Effect { get; }

        public int Remaining { get; }

        public bool CanTick { get; }
    }
}
