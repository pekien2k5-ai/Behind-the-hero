using System;

namespace BTH.Core.Grid
{
    /// <summary>
    /// Thuộc tính địa hình. Core chỉ phản ứng với các cờ chung này;
    /// hành vi đặc thù của từng loại địa hình nằm ở ITerrainBehaviour (Content).
    /// </summary>
    [Flags]
    public enum TerrainFlags
    {
        None = 0,
        BlocksMove = 1 << 0,
        BlocksLineOfSight = 1 << 1,
        IsHazard = 1 << 2,
        IsCover = 1 << 3,
        IsInteractive = 1 << 4
    }
}
