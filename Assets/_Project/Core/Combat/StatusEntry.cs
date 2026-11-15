namespace BTH.Core.Combat
{
    /// <summary>
    /// Bản ghi runtime của một status trong StatusSet: effect gốc + thời gian còn lại.
    /// Tách khỏi IStatusEffect để effect giữ immutable.
    /// </summary>
    internal sealed class StatusEntry
    {
        public StatusEntry(IStatusEffect effect, int remaining, bool canTick)
        {
            Effect = effect;
            Remaining = remaining;
            CanTick = canTick;
        }

        public IStatusEffect Effect { get; }

        public int Remaining { get; set; }

        /// <summary>Sai khi status vừa được gắn trong lượt hiện tại: bỏ qua lần trừ duration kế tiếp.</summary>
        public bool CanTick { get; set; }
    }
}
