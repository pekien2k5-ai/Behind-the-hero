using BTH.Core.Units;

namespace BTH.Core.Grid
{
    /// <summary>
    /// Trạng thái một ô lưới. Chỉ GridMap và UnitRegistry được sửa (setter internal);
    /// phần còn lại của game đọc qua IGridMap.Get/TryGet.
    /// </summary>
    public sealed class CellState
    {
        internal CellState(GridCoord coord)
        {
            Coord = coord;
            Terrain = TerrainId.Floor;
            Flags = TerrainFlags.None;
            MovementCost = 1;
            Occupant = UnitId.None;
        }

        /// <summary>Copy dùng cho snapshot; không chia sẻ tham chiếu với ô gốc.</summary>
        internal CellState(CellState source)
        {
            Coord = source.Coord;
            Terrain = source.Terrain;
            Flags = source.Flags;
            HazardDamagePerTurn = source.HazardDamagePerTurn;
            TurnsUntilCollapse = source.TurnsUntilCollapse;
            MovementCost = source.MovementCost;
            Occupant = source.Occupant;
            IsLocked = source.IsLocked;
        }

        public GridCoord Coord { get; }

        public TerrainId Terrain { get; internal set; }

        public TerrainFlags Flags { get; internal set; }

        /// <summary>Sát thương hazard mỗi lượt đứng trên ô (0 = không có).</summary>
        public int HazardDamagePerTurn { get; internal set; }

        /// <summary>Số lượt còn lại trước khi ô sụp; 0 = không hẹn giờ.</summary>
        public int TurnsUntilCollapse { get; internal set; }

        /// <summary>Chi phí đi vào ô (mặc định 1).</summary>
        public int MovementCost { get; internal set; }

        public UnitId Occupant { get; internal set; }

        /// <summary>Ô bị khoá bởi hiệu ứng khoá sân: không thể đi vào.</summary>
        public bool IsLocked { get; internal set; }

        public bool IsOccupied => Occupant.IsValid;

        public bool BlocksMove => (Flags & TerrainFlags.BlocksMove) != 0;

        public bool BlocksLineOfSight => (Flags & TerrainFlags.BlocksLineOfSight) != 0;

        public bool IsHazard => (Flags & TerrainFlags.IsHazard) != 0;

        public bool IsInteractive => (Flags & TerrainFlags.IsInteractive) != 0;

        internal void RestoreFrom(CellState source)
        {
            Terrain = source.Terrain;
            Flags = source.Flags;
            HazardDamagePerTurn = source.HazardDamagePerTurn;
            TurnsUntilCollapse = source.TurnsUntilCollapse;
            MovementCost = source.MovementCost;
            Occupant = source.Occupant;
            IsLocked = source.IsLocked;
        }

        public override string ToString()
        {
            return Coord + " " + Terrain.Value;
        }
    }
}
