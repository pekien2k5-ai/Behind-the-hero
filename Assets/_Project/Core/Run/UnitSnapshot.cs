using System.Collections.Generic;
using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    /// <summary>Ảnh chụp một unit: vị trí, HP, AP, cờ lượt, status và cooldown.</summary>
    public sealed class UnitSnapshot
    {
        public UnitSnapshot(UnitId id, UnitDefinition definition, GridCoord position, int hp,
            int actionPoints, bool hasMoved, bool hasActed, IReadOnlyList<StatusSnapshot> statuses,
            IReadOnlyDictionary<string, int> cooldowns)
        {
            Id = id;
            Definition = definition;
            Position = position;
            Hp = hp;
            ActionPoints = actionPoints;
            HasMovedThisTurn = hasMoved;
            HasActedThisTurn = hasActed;
            Statuses = statuses ?? new List<StatusSnapshot>();
            Cooldowns = cooldowns ?? new Dictionary<string, int>();
        }

        public UnitId Id { get; }

        public UnitDefinition Definition { get; }

        public GridCoord Position { get; }

        public int Hp { get; }

        public int ActionPoints { get; }

        public bool HasMovedThisTurn { get; }

        public bool HasActedThisTurn { get; }

        public IReadOnlyList<StatusSnapshot> Statuses { get; }

        public IReadOnlyDictionary<string, int> Cooldowns { get; }
    }
}
