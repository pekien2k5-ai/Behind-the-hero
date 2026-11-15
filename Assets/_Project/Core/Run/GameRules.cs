namespace BTH.Core.Run
{
    /// <summary>Bộ luật mặc định theo bảng cân bằng trong tài liệu kiến trúc.</summary>
    public sealed class GameRules : IGameRules
    {
        public int ApPerTurn { get; set; } = 2;

        public int MovePoints { get; set; } = 2;

        public int TurnLimit { get; set; } = 25;

        public float Phase2Threshold { get; set; } = 0.7f;

        public float GuardMultiplier { get; set; } = 0.2f;

        public float StaggerMultiplier { get; set; } = 2f;

        public float FlankMultiplier { get; set; } = 1.5f;

        public int PoisonPerTurn { get; set; } = 3;
    }
}
