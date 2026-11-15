namespace BTH.Core.Run
{
    /// <summary>Đánh giá thắng/thua sau mỗi lần state đổi. Không sửa state.</summary>
    public interface IObjectiveEvaluator
    {
        StageResult Evaluate(GameState state, StageDefinition stage);
    }
}
