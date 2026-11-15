using System.Collections.Generic;
using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Ai
{
    /// <summary>
    /// Công bố ý định địch và vùng nguy hiểm để UI vẽ telegraph. Chỉ lưu ý định,
    /// không tự thi hành — executor vẫn là nơi đổi state.
    /// </summary>
    public interface ITelegraphService
    {
        IReadOnlyCollection<EnemyIntent> All { get; }

        IReadOnlyList<GridCoord> DangerCells { get; }

        bool TryGet(UnitId unit, out EnemyIntent intent);

        void Declare(EnemyIntent intent);

        /// <summary>Xoá hết ý định, phát TelegraphClearedEvent với số ý định đã xoá.</summary>
        void Clear();
    }
}
