using BTH.Core.Grid;

namespace BTH.Core.Combat
{
    /// <summary>Hẹn ô sụp sau N lượt (Earth Crack).</summary>
    public sealed class ArmCollapseEffect : IGameEffect
    {
        private readonly GridCoord _cell;
        private readonly int _turns;

        public ArmCollapseEffect(GridCoord cell, int turns)
        {
            _cell = cell;
            _turns = turns;
        }

        public int Priority => 41;

        public void Resolve(EffectContext ctx)
        {
            if (ctx.State.Grid.InBounds(_cell))
            {
                ctx.State.Grid.ArmCollapse(_cell, _turns);
            }
        }
    }
}
