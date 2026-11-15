using System.Collections.Generic;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Turn
{
    /// <summary>
    /// Nhịp 4 thì: Telegraph → Player → Enemy → TurnEnd, lặp lại.
    /// EndTurn là nơi duy nhất được tick status/địa hình và tăng TurnIndex.
    /// </summary>
    public interface ITurnManager
    {
        TurnPhase Phase { get; }

        int TurnIndex { get; }

        int TurnLimit { get; }

        UnitId ActiveUnit { get; }

        /// <summary>Thứ tự địch của lượt hiện tại (tính lại ở BeginEnemyPhase).</summary>
        IReadOnlyList<UnitId> EnemyOrder { get; }

        void StartStage(StageDefinition stage);

        void BeginTelegraph();

        void BeginPlayerPhase();

        void EndPlayerPhase();

        void BeginEnemyPhase();

        void EndEnemyPhase();

        /// <summary>Tick status → tick địa hình → tăng lượt → đánh giá mục tiêu → snapshot.</summary>
        void EndTurn();

        void BeginClash();

        void FinishStage(StageResult result);

        bool CanSpend(UnitId unit, int cost);

        bool TrySpend(UnitId unit, int cost);

        void ResetActionPoints();

        /// <summary>Unit địch kế tiếp trong EnemyOrder; None khi hết.</summary>
        UnitId NextEnemy();
    }
}
