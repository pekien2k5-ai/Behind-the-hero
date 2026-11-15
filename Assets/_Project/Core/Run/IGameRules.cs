namespace BTH.Core.Run
{
    /// <summary>
    /// Toàn bộ con số cân bằng của game nằm ở đây. Core không hard-code 2 AP,
    /// 3 sát thương độc... ở bất kỳ chỗ nào khác.
    /// </summary>
    public interface IGameRules
    {
        int ApPerTurn { get; }

        int MovePoints { get; }

        int TurnLimit { get; }

        float Phase2Threshold { get; }

        float GuardMultiplier { get; }

        float StaggerMultiplier { get; }

        float FlankMultiplier { get; }

        int PoisonPerTurn { get; }
    }
}
