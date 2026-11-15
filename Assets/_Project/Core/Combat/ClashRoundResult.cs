namespace BTH.Core.Combat
{
    /// <summary>Kết quả một vòng Duel: power hiệu dụng và sát thương thực đã gây.</summary>
    public readonly struct ClashRoundResult
    {
        public ClashRoundResult(ClashResult result, int playerPower, int bossPower,
            int damageToPlayer, int damageToBoss, IStatusEffect applied)
        {
            Result = result;
            PlayerPower = playerPower;
            BossPower = bossPower;
            DamageToPlayer = damageToPlayer;
            DamageToBoss = damageToBoss;
            Applied = applied;
        }

        public ClashResult Result { get; }

        public int PlayerPower { get; }

        public int BossPower { get; }

        public int DamageToPlayer { get; }

        public int DamageToBoss { get; }

        public IStatusEffect Applied { get; }
    }
}
