using System;
using System.Collections.Generic;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Ai
{
    /// <summary>
    /// Cài đặt mặc định: ngưỡng lấy từ IGameRules.Phase2Threshold, khoá các ô sân do
    /// Content cung cấp, phát BossPhaseChangedEvent. Không tự đổi UnitDefinition.
    /// </summary>
    public sealed class BossPhaseController : IBossPhaseController
    {
        private readonly IGameRules _rules;
        private readonly Func<GameState, IReadOnlyList<GridCoord>> _arenaCells;

        public BossPhaseController(IGameRules rules,
            Func<GameState, IReadOnlyList<GridCoord>> arenaCells = null)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _arenaCells = arenaCells;
            Phase = 1;
        }

        public int Phase { get; private set; }

        public bool ShouldEnterPhase2(int hp, int maxHp)
        {
            if (maxHp <= 0 || hp <= 0)
            {
                return false;
            }

            return Phase == 1 && (float)hp <= maxHp * _rules.Phase2Threshold;
        }

        public void EnterPhase2(GameState state)
        {
            if (state == null || Phase != 1)
            {
                return;
            }

            Phase = 2;
            var boss = state.Units.Boss;
            UnitId bossId = boss != null ? boss.Id : UnitId.None;

            var cells = _arenaCells != null ? _arenaCells(state) : null;
            if (cells != null)
            {
                state.Grid.LockArea(cells);
            }

            state.Events?.Publish(new BossPhaseChangedEvent(bossId, Phase));
        }
    }
}
