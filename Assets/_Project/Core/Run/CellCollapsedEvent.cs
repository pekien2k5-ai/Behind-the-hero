using BTH.Core.Grid;

namespace BTH.Core.Run
{
    public readonly struct CellCollapsedEvent
    {
        public readonly GridCoord Coord;

        public CellCollapsedEvent(GridCoord coord)
        {
            Coord = coord;
        }
    }
}
