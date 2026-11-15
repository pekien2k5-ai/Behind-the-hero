namespace BTH.Core.Combat
{
    /// <summary>
    /// Dự đoán trước khi chọn chiêu: cùng công thức với Resolve, nên UI hiện badge
    /// xanh/vàng/đỏ mà người chơi không bị lừa.
    /// </summary>
    public readonly struct ClashPrediction
    {
        public ClashPrediction(ClashResult result, int playerPower, int bossPower, int bonus)
        {
            Result = result;
            PlayerPower = playerPower;
            BossPower = bossPower;
            Bonus = bonus;
        }

        public ClashResult Result { get; }

        public int PlayerPower { get; }

        public int BossPower { get; }

        public int Bonus { get; }
    }
}
