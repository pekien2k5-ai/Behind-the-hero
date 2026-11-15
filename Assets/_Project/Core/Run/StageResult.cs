namespace BTH.Core.Run
{
    /// <summary>
    /// Kết quả đánh giá mục tiêu màn. EndingA/EndingB do dialogue quyết định,
    /// không do ObjectiveEvaluator suy ra.
    /// </summary>
    public enum StageResult
    {
        None = 0,
        Victory = 1,
        DefeatPlayerDead = 2,
        DefeatTurnLimit = 3,
        EndingA = 4,
        EndingB = 5
    }
}
