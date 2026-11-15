using BTH.Core.Grid;

namespace BTH.Core.Run
{
    /// <summary>Ghi đè địa hình khi dựng màn (hố, tường, ô sụp, bẫy độc...).</summary>
    public readonly struct TerrainOverride
    {
        public readonly GridCoord Coord;
        public readonly TerrainId Terrain;
        public readonly TerrainFlags Flags;
        public readonly int MovementCost;
        public readonly int HazardDamagePerTurn;
        public readonly int CollapseInTurns;

        public TerrainOverride(GridCoord coord, TerrainId terrain, TerrainFlags flags,
            int movementCost = 1, int hazardDamagePerTurn = 0, int collapseInTurns = 0)
        {
            Coord = coord;
            Terrain = terrain;
            Flags = flags;
            MovementCost = movementCost;
            HazardDamagePerTurn = hazardDamagePerTurn;
            CollapseInTurns = collapseInTurns;
        }
    }
}
