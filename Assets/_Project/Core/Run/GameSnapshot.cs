using System;
using System.Collections.Generic;
using BTH.Core.Ai;
using BTH.Core.Grid;
using BTH.Core.Turn;

namespace BTH.Core.Run
{
    /// <summary>
    /// Ảnh chụp toàn bộ state cần để tua lại. Không serialize xuống đĩa (ADR-05);
    /// chỉ sống trong bộ nhớ của một run.
    /// </summary>
    public sealed class GameSnapshot
    {
        public GameSnapshot(int turnIndex, TurnPhase phase, StageResult result, uint randomState,
            CellState[] cells, IReadOnlyList<GridCoord> lockedCells, UnitSnapshot[] units,
            IReadOnlyList<EnemyIntent> intents, DuelSnapshot duel = null)
        {
            TurnIndex = turnIndex;
            Phase = phase;
            Result = result;
            RandomState = randomState;
            Cells = cells ?? Array.Empty<CellState>();
            LockedCells = lockedCells ?? Array.Empty<GridCoord>();
            Units = units ?? Array.Empty<UnitSnapshot>();
            Intents = intents ?? Array.Empty<EnemyIntent>();
            Duel = duel;
        }

        public int TurnIndex { get; }

        public TurnPhase Phase { get; }

        public StageResult Result { get; }

        public uint RandomState { get; }

        public CellState[] Cells { get; }

        public IReadOnlyList<GridCoord> LockedCells { get; }

        public UnitSnapshot[] Units { get; }

        public IReadOnlyList<EnemyIntent> Intents { get; }

        /// <summary>Ảnh chụp Duel; null nếu snapshot được chụp ngoài Duel.</summary>
        public DuelSnapshot Duel { get; }

        /// <summary>So sánh nội dung — dùng trong SnapshotTests: Restore(Capture()) phải bằng nhau.</summary>
        public bool ContentEquals(GameSnapshot other)
        {
            if (other == null || TurnIndex != other.TurnIndex || Phase != other.Phase ||
                Result != other.Result || RandomState != other.RandomState ||
                LockedCells.Count != other.LockedCells.Count || Units.Length != other.Units.Length ||
                Cells.Length != other.Cells.Length)
            {
                return false;
            }

            for (int i = 0; i < LockedCells.Count; i++)
            {
                if (LockedCells[i] != other.LockedCells[i])
                {
                    return false;
                }
            }

            for (int i = 0; i < Cells.Length; i++)
            {
                if (!CellEquals(Cells[i], other.Cells[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < Units.Length; i++)
            {
                if (!UnitEquals(Units[i], other.Units[i]))
                {
                    return false;
                }
            }

            if (Duel == null || other.Duel == null)
            {
                return Duel == other.Duel;
            }

            return Duel.ContentEquals(other.Duel);
        }

        private static bool CellEquals(CellState a, CellState b)
        {
            return a.Coord == b.Coord && a.Terrain == b.Terrain && a.Flags == b.Flags &&
                   a.MovementCost == b.MovementCost &&
                   a.HazardDamagePerTurn == b.HazardDamagePerTurn &&
                   a.TurnsUntilCollapse == b.TurnsUntilCollapse && a.Occupant == b.Occupant;
        }

        private static bool UnitEquals(UnitSnapshot a, UnitSnapshot b)
        {
            if (a.Id != b.Id || a.Position != b.Position || a.Hp != b.Hp ||
                a.ActionPoints != b.ActionPoints || a.HasMovedThisTurn != b.HasMovedThisTurn ||
                a.HasActedThisTurn != b.HasActedThisTurn || a.Statuses.Count != b.Statuses.Count ||
                a.Cooldowns.Count != b.Cooldowns.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Statuses.Count; i++)
            {
                var left = a.Statuses[i];
                var right = b.Statuses[i];
                if (left.Effect.Id != right.Effect.Id || left.Remaining != right.Remaining ||
                    left.CanTick != right.CanTick)
                {
                    return false;
                }
            }

            foreach (var pair in a.Cooldowns)
            {
                if (!b.Cooldowns.TryGetValue(pair.Key, out var turns) || turns != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
