namespace BTH.Core.Run
{
    /// <summary>
    /// Mục tiêu mặc định: giết boss để thắng, người chơi chết hoặc quá số lượt là thua.
    /// EndingA/EndingB do DialogueGraph quyết định sau khi thắng.
    /// </summary>
    public sealed class ObjectiveEvaluator : IObjectiveEvaluator
    {
        public StageResult Evaluate(GameState state, StageDefinition stage)
        {
            if (state == null)
            {
                return StageResult.None;
            }

            if (state.Result != StageResult.None)
            {
                return state.Result;
            }

            var player = state.Units.Player;
            if (player == null || !player.IsAlive)
            {
                return StageResult.DefeatPlayerDead;
            }

            var boss = state.Units.Boss;
            if (boss != null && !boss.IsAlive)
            {
                return StageResult.Victory;
            }

            if (stage != null && state.Turn != null && state.Turn.TurnIndex > state.Turn.TurnLimit)
            {
                return StageResult.DefeatTurnLimit;
            }

            return StageResult.None;
        }
    }
}
