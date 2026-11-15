using System;
using System.Collections.Generic;
using BTH.Core.Units;

namespace BTH.Core.Grid
{
    /// <summary>
    /// Hợp đồng lưới. Lớp Core chỉ biết các cờ địa hình chung, không biết
    /// "cracked" hay "poison" cụ thể là gì — hành vi nằm ở ITerrainBehaviour.
    /// </summary>
    public interface IGridMap
    {
        int Width { get; }

        int Height { get; }

        bool InBounds(GridCoord coord);

        /// <summary>Trả về ô; ném ArgumentOutOfRangeException nếu toạ độ ngoài lưới.</summary>
        CellState Get(GridCoord coord);

        bool TryGet(GridCoord coord, out CellState cell);

        /// <summary>Địa hình chặn di chuyển (không xét unit).</summary>
        bool IsBlocked(GridCoord coord);

        /// <summary>Ô có thể đi vào với profile này (xét địa hình, khoá sân, unit đang đứng).</summary>
        bool IsWalkable(GridCoord coord, MoveProfile profile);

        bool HasLineOfSight(GridCoord from, GridCoord to);

        int MovementCost(GridCoord coord);

        bool IsLocked(GridCoord coord);

        void SetTerrain(GridCoord coord, TerrainId terrain, TerrainFlags flags,
            int movementCost = 1, int hazardDamagePerTurn = 0);

        /// <summary>Hẹn ô sụp sau turns lượt; 0 = huỷ hẹn.</summary>
        void ArmCollapse(GridCoord coord, int turns);

        /// <summary>Giảm bộ đếm sụp 1 lượt và biến các ô đến hạn thành pit.</summary>
        void TickCollapse();

        void LockArea(IEnumerable<GridCoord> cells);

        void UnlockArea();

        /// <summary>UnitRegistry ghi nhận chỗ đứng; phần còn lại của game không gọi hàm này.</summary>
        void SetOccupant(GridCoord coord, UnitId unit);

        /// <summary>Bắn sau mọi thay đổi địa hình của một ô.</summary>
        event Action<GridCoord> CellChanged;
    }
}
