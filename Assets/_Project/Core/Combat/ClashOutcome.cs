namespace BTH.Core.Combat
{
    /// <summary>Kết quả một lần so Clash. PlayerPower/BossPower là power hiệu dụng đã cộng bonus.</summary>
    public sealed class ClashOutcome
    {
        public ClashOutcome(ClashResult result, int playerPower, int bossPower,
            IStatusEffect appliedEffect, bool appliedToBoss)
        {
            Result = result;
            PlayerPower = playerPower;
            BossPower = bossPower;
            AppliedEffect = appliedEffect;
            AppliedToBoss = appliedToBoss;
        }

        public ClashResult Result { get; }

        public int PlayerPower { get; }

        public int BossPower { get; }

        /// <summary>Status gắn sau Clash (null khi hoà).</summary>
        public IStatusEffect AppliedEffect { get; }

        public bool AppliedToBoss { get; }
    }
}
