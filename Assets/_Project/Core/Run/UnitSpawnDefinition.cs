using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    /// <summary>Một unit cần spawn khi bắt đầu màn.</summary>
    public sealed class UnitSpawnDefinition
    {
        public UnitSpawnDefinition(UnitDefinition definition, GridCoord anchor)
        {
            Definition = definition;
            Anchor = anchor;
        }

        public UnitDefinition Definition { get; }

        public GridCoord Anchor { get; }
    }
}
