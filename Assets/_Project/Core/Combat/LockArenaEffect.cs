using System.Collections.Generic;
using BTH.Core.Grid;

namespace BTH.Core.Combat
{
    /// <summary>Khoá các ô sân đấu (boss phase 2).</summary>
    public sealed class LockArenaEffect : IGameEffect
    {
        private readonly IReadOnlyList<GridCoord> _cells;

        public LockArenaEffect(IReadOnlyList<GridCoord> cells)
        {
            _cells = cells;
        }

        public int Priority => 45;

        public void Resolve(EffectContext ctx)
        {
            ctx.State.Grid.LockArea(_cells);
        }
    }
}
