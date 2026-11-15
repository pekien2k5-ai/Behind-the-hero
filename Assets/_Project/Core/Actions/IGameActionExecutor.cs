using BTH.Core.Run;

namespace BTH.Core.Actions
{
    /// <summary>Chạy một ActionRequest hợp lệ; phát event sau khi state đã ổn định.</summary>
    public interface IGameActionExecutor
    {
        ActionResult Execute(ActionRequest request, GameState state);
    }
}
