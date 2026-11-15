using BTH.Core.Grid;

namespace BTH.Core.Run
{
    public readonly struct CellChangedEvent
    {
        public readonly GridCoord Coord;
        public readonly TerrainId Terrain;
        public readonly TerrainFlags Flags;
        public readonly int HazardDamagePerTurn;

        public CellChangedEvent(GridCoord coord, TerrainId terrain, TerrainFlags flags, int hazardDamagePerTurn)
        {
            Coord = coord;
            Terrain = terrain;
            Flags = flags;
            HazardDamagePerTurn = hazardDamagePerTurn;
        }
    }
}
