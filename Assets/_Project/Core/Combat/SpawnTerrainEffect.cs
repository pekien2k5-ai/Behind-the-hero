using BTH.Core.Grid;

namespace BTH.Core.Combat
{
    /// <summary>Đổi địa hình một ô (Earth Crack, Brimstone...) và hẹn sụp nếu cần.</summary>
    public sealed class SpawnTerrainEffect : IGameEffect
    {
        private readonly GridCoord _cell;
        private readonly TerrainId _terrain;
        private readonly TerrainFlags _flags;
        private readonly int _movementCost;
        private readonly int _hazardDamagePerTurn;
        private readonly int _collapseInTurns;

        public SpawnTerrainEffect(GridCoord cell, TerrainId terrain, TerrainFlags flags,
            int movementCost = 1, int hazardDamagePerTurn = 0, int collapseInTurns = 0)
        {
            _cell = cell;
            _terrain = terrain;
            _flags = flags;
            _movementCost = movementCost;
            _hazardDamagePerTurn = hazardDamagePerTurn;
            _collapseInTurns = collapseInTurns;
        }

        public int Priority => 40;

        public void Resolve(EffectContext ctx)
        {
            if (!ctx.State.Grid.InBounds(_cell))
            {
                return;
            }

            ctx.State.Grid.SetTerrain(_cell, _terrain, _flags, _movementCost, _hazardDamagePerTurn);
            if (_collapseInTurns > 0)
            {
                ctx.State.Grid.ArmCollapse(_cell, _collapseInTurns);
            }
        }
    }
}
