using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Actions
{
    /// <summary>
    /// Một mệnh lệnh đã được dựng từ ActionRequest. Mọi thay đổi state phải đi qua
    /// executor — đây chỉ là dữ liệu mô tả lệnh.
    /// </summary>
    public interface IGameAction
    {
        string Id { get; }

        ActionKind Kind { get; }

        UnitId Actor { get; }

        GridCoord Target { get; }
    }
}
