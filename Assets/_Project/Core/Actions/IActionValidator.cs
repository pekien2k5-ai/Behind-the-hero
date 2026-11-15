using System.Collections.Generic;
using BTH.Core.Grid;
using BTH.Core.Run;

namespace BTH.Core.Actions
{
    /// <summary>
    /// Kiểm tra yêu cầu hành động bằng luật thuần (không đụng UI/scene).
    /// GetValidTargets dùng cùng một bộ luật để UI không phải tự suy.
    /// </summary>
    public interface IActionValidator
    {
        ValidationResult Validate(ActionRequest request, GameState state);

        IReadOnlyList<GridCoord> GetValidTargets(ActionRequest request, GameState state);
    }
}
