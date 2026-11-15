using System;
using System.Collections.Generic;
using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>
    /// Định nghĩa tĩnh của một loại unit (POCO — Content nạp từ ScriptableObject/asset).
    /// Không chứa state thay đổi trong run; state nằm ở UnitState.
    /// </summary>
    public sealed class UnitDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public UnitFaction Faction { get; set; } = UnitFaction.Enemy;

        public UnitArchetype Archetype { get; set; } = UnitArchetype.Minion;

        public int MaxHp { get; set; } = 1;

        public int Attack { get; set; }

        public int Armor { get; set; }

        public int Initiative { get; set; }

        public int SizeX { get; set; } = 1;

        public int SizeY { get; set; } = 1;

        public MoveProfile Move { get; set; } = MoveProfile.Walk(2);

        public Combat.ElementId NativeElement { get; set; } = Combat.ElementId.None;

        public IReadOnlyList<string> SkillIds { get; set; } = Array.Empty<string>();

        public string BrainId { get; set; } = string.Empty;

        public string PortraitId { get; set; } = string.Empty;

        public bool IsBoss => Archetype == UnitArchetype.StageBoss || Archetype == UnitArchetype.FinalBoss;
    }
}
