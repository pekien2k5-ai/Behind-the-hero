using System.Collections.Generic;
using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    /// <summary>Dữ liệu một màn. Core thuần POCO; Content tự dựng từ ScriptableObject.</summary>
    public sealed class StageDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public int Width { get; set; } = 8;

        public int Height { get; set; } = 8;

        public UnitDefinition Player { get; set; }

        public GridCoord PlayerSpawn { get; set; }

        public List<UnitSpawnDefinition> Spawns { get; } = new List<UnitSpawnDefinition>();

        public List<TerrainOverride> Terrains { get; } = new List<TerrainOverride>();

        public int TurnLimit { get; set; } = 25;

        /// <summary>Id của UnitDefinition được coi là boss của màn (dùng cho dữ liệu/UI).</summary>
        public string BossId { get; set; } = string.Empty;

        /// <summary>Dữ liệu Clash của màn; null nếu màn không có Clash.</summary>
        public Combat.ClashDefinition Clash { get; set; }

        /// <summary>Dữ liệu Duel phase 2; null nếu màn không có Duel.</summary>
        public Combat.DuelDefinition Duel { get; set; }
    }
}
