using System.Collections.Generic;
using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>
    /// Sổ unit của run. Là nơi duy nhất giữ danh sách unit và bản đồ chiếm ô;
    /// mọi hành động đi qua đây để không ai tự sửa vị trí/HP.
    /// </summary>
    public interface IUnitRegistry
    {
        IReadOnlyList<UnitState> All { get; }

        UnitState Player { get; }

        UnitState Boss { get; }

        IReadOnlyList<UnitState> Alive(UnitFaction faction);

        UnitState Get(UnitId id);

        bool TryGet(UnitId id, out UnitState unit);

        /// <summary>Unit đang chiếm ô (kể cả ô thứ 2 của boss 2×2).</summary>
        UnitState At(GridCoord coord);

        bool IsOccupied(GridCoord coord);

        /// <summary>Footprint có nằm trọn trong lưới, không bị chặn và không đè unit khác.</summary>
        bool CanPlace(UnitDefinition definition, GridCoord anchor);

        /// <summary>Như CanPlace nhưng bỏ qua ô của chính unit ignore (dùng khi dời/push).</summary>
        bool CanPlace(UnitDefinition definition, GridCoord anchor, UnitId ignore);

        /// <summary>Spawn unit; UnitId tự cấp. Phát UnitSpawnedEvent.</summary>
        UnitState Add(UnitDefinition definition, GridCoord anchor);

        /// <summary>Rút unit khỏi sân (despawn), không phát event chết.</summary>
        void Remove(UnitId id);

        /// <summary>Dời unit; không phát event — action gọi sau khi state đã ổn định.</summary>
        void MoveTo(UnitId id, GridCoord anchor);

        IReadOnlyList<UnitState> UnitsIn(IReadOnlyList<GridCoord> cells);

        /// <summary>Unit sống có footprint kề 4 hướng với ô đã cho.</summary>
        IReadOnlyList<UnitState> AdjacentTo(GridCoord coord);

        /// <summary>HP về 0 và phát UnitDiedEvent.</summary>
        void Kill(UnitId id);
    }
}
