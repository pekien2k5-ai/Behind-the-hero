using BTH.Core.Ai;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Turn;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    /// <summary>
    /// Gói state của một màn đang chạy. Mọi service (turn, snapshot, clash, AI)
    /// nhận tham chiếu tới đây thay vì singleton.
    /// </summary>
    public sealed class GameState : IGameStateView
    {
        public GameState(IGridMap grid, IUnitRegistry units, IGameRules rules, IEventBus events,
            IRandomSource random)
        {
            Grid = grid;
            Units = units;
            Rules = rules;
            Events = events;
            Random = random;
        }

        public IGridMap Grid { get; }

        public IUnitRegistry Units { get; }

        public IPathfinder Pathfinder { get; internal set; }

        public ITurnManager Turn { get; internal set; }

        public IGameRules Rules { get; }

        public IEventBus Events { get; }

        public IRandomSource Random { get; }

        public ISkillLibrary Skills { get; internal set; }

        public IDamageCalculator Damage { get; internal set; }

        public ITelegraphService Telegraph { get; internal set; }

        public IClashResolver Clash { get; internal set; }

        public IBossPhaseController BossPhase { get; internal set; }

        public ISnapshotService Snapshots { get; internal set; }

        public IRunKnowledge Knowledge { get; internal set; }

        public StageDefinition Stage { get; internal set; }

        public StageResult Result { get; internal set; }

        public bool IsOver => Result != StageResult.None;
    }
}
