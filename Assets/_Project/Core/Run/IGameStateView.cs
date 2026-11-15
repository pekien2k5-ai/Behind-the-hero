using BTH.Core.Grid;
using BTH.Core.Turn;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    /// <summary>Giao diện chỉ-đọc mà UI/Presenter được phép chạm vào.</summary>
    public interface IGameStateView
    {
        IGridMap Grid { get; }

        IUnitRegistry Units { get; }

        ITurnManager Turn { get; }

        IGameRules Rules { get; }
    }
}
