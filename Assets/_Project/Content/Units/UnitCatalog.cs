using System.Collections.Generic;
using BTH.Core.Combat;
using BTH.Core.Units;

namespace BTH.Content.Units
{
    public static class UnitCatalog
    {
        public const string WandererId = "wanderer";
        public const string EosphorosId = "eosphoros";
        public const string ImpId = "imp";
        public const string GargoyleId = "gargoyle";
        public const string AshenExecutionerId = "boss-ashen";
        public const string PlagueWitchId = "boss-morbida";
        public const string AbyssKnightId = "boss-abyss";
        public const string DemonKingMalkorId = "boss-malkor";
        public const string FallenEosphorosId = "boss-fallen-eosphoros";

        public static UnitDefinition CreateWanderer() => new UnitDefinition
        {
            Id = WandererId,
            DisplayName = "Kẻ Lang Thang (Wanderer)",
            Faction = UnitFaction.Player,
            Archetype = UnitArchetype.Hero,
            MaxHp = 12,
            Attack = 4,
            Armor = 0,
            Initiative = 10,
            SizeX = 1,
            SizeY = 1,
            Move = MoveProfile.Walk(2),
            NativeElement = ElementId.Temporal,
            PortraitId = WandererId,
            SkillIds = new List<string>
            {
                "swift-slash",
                "blade-guard",
                "necrotic-burst",
                "necrotic-ward",
                "temporal-step",
                "dodge"
            }
        };

        public static UnitDefinition CreateEosphorosAlly() => new UnitDefinition
        {
            Id = EosphorosId,
            DisplayName = "Dũng Giả Eosphoros",
            Faction = UnitFaction.Ally,
            Archetype = UnitArchetype.Ally,
            MaxHp = 30,
            Attack = 6,
            Armor = 2,
            Initiative = 6,
            SizeX = 1,
            SizeY = 1,
            Move = MoveProfile.Walk(2),
            NativeElement = ElementId.Holy,
            PortraitId = EosphorosId,
            BrainId = "brain-ally",
            SkillIds = new List<string> { "holy-sword-slash", "holy-pillar", "divine-barrier" }
        };

        public static UnitDefinition CreateImp() => new UnitDefinition
        {
            Id = ImpId,
            DisplayName = "Tiểu Quỷ (Imp)",
            Faction = UnitFaction.Enemy,
            Archetype = UnitArchetype.Minion,
            MaxHp = 6,
            Attack = 2,
            Armor = 0,
            Initiative = 8,
            SizeX = 1,
            SizeY = 1,
            Move = MoveProfile.Walk(3),
            NativeElement = ElementId.Fire,
            PortraitId = ImpId,
            BrainId = "brain-melee",
            SkillIds = new List<string> { "imp-poke" }
        };

        public static UnitDefinition CreateGargoyle() => new UnitDefinition
        {
            Id = GargoyleId,
            DisplayName = "Quái Thạch (Gargoyle)",
            Faction = UnitFaction.Enemy,
            Archetype = UnitArchetype.Elite,
            MaxHp = 10,
            Attack = 3,
            Armor = 1,
            Initiative = 7,
            SizeX = 1,
            SizeY = 1,
            Move = new MoveProfile(2, canFly: true, ignoresHazard: true, canPassUnits: false),
            NativeElement = ElementId.Physical,
            PortraitId = GargoyleId,
            BrainId = "brain-ranged",
            SkillIds = new List<string> { "gargoyle-bolt" }
        };

        public static UnitDefinition CreateAshenExecutioner() => new UnitDefinition
        {
            Id = AshenExecutionerId,
            DisplayName = "Đao Phủ Tro Tàn",
            Faction = UnitFaction.Enemy,
            Archetype = UnitArchetype.StageBoss,
            MaxHp = 20,
            Attack = 5,
            Armor = 0,
            Initiative = 5,
            SizeX = 1,
            SizeY = 1,
            Move = MoveProfile.Walk(1),
            NativeElement = ElementId.Physical,
            PortraitId = AshenExecutionerId,
            BrainId = "brain-boss-ashen",
            SkillIds = new List<string> { "ashen-fury", "axe-cleave", "earth-crack" }
        };

        public static UnitDefinition CreatePlagueWitch() => new UnitDefinition
        {
            Id = PlagueWitchId,
            DisplayName = "Vu Nữ Hoại Tử Morbida",
            Faction = UnitFaction.Enemy,
            Archetype = UnitArchetype.StageBoss,
            MaxHp = 18,
            Attack = 4,
            Armor = 0,
            Initiative = 6,
            SizeX = 1,
            SizeY = 1,
            Move = MoveProfile.Walk(2),
            NativeElement = ElementId.Necrotic,
            PortraitId = PlagueWitchId,
            BrainId = "brain-boss-morbida",
            SkillIds = new List<string> { "plague-storm", "poison-dance", "grave-hands" }
        };

        public static UnitDefinition CreateAbyssKnight() => new UnitDefinition
        {
            Id = AbyssKnightId,
            DisplayName = "Kỵ Sĩ Hư Không",
            Faction = UnitFaction.Enemy,
            Archetype = UnitArchetype.StageBoss,
            MaxHp = 25,
            Attack = 6,
            Armor = 1,
            Initiative = 5,
            SizeX = 1,
            SizeY = 1,
            Move = MoveProfile.Walk(1),
            NativeElement = ElementId.Piercing,
            PortraitId = AbyssKnightId,
            BrainId = "brain-boss-abyss",
            SkillIds = new List<string> { "void-spear-beam", "spear-thrust", "eye-shield" }
        };

        public static UnitDefinition CreateDemonKingMalkor() => new UnitDefinition
        {
            Id = DemonKingMalkorId,
            DisplayName = "Quỷ Vương Malkor",
            Faction = UnitFaction.Enemy,
            Archetype = UnitArchetype.StageBoss,
            MaxHp = 45,
            Attack = 8,
            Armor = 1,
            Initiative = 4,
            SizeX = 2,
            SizeY = 2,
            Move = MoveProfile.Walk(1),
            NativeElement = ElementId.Fire,
            PortraitId = DemonKingMalkorId,
            BrainId = "brain-boss-malkor",
            SkillIds = new List<string> { "hellfire-greatsword", "fireball", "brimstone-meteor" }
        };

        public static UnitDefinition CreateFallenEosphoros() => new UnitDefinition
        {
            Id = FallenEosphorosId,
            DisplayName = "Eosphoros Sa Ngã",
            Faction = UnitFaction.Enemy,
            Archetype = UnitArchetype.FinalBoss,
            MaxHp = 30,
            Attack = 6,
            Armor = 1,
            Initiative = 7,
            SizeX = 1,
            SizeY = 1,
            Move = MoveProfile.Walk(2),
            NativeElement = ElementId.Holy,
            PortraitId = FallenEosphorosId,
            BrainId = "brain-boss-fallen",
            SkillIds = new List<string> { "holy-light-slash", "holy-strike", "holy-pillar" }
        };

        public static IReadOnlyList<UnitDefinition> All() => new List<UnitDefinition>
        {
            CreateWanderer(),
            CreateEosphorosAlly(),
            CreateImp(),
            CreateGargoyle(),
            CreateAshenExecutioner(),
            CreatePlagueWitch(),
            CreateAbyssKnight(),
            CreateDemonKingMalkor(),
            CreateFallenEosphoros()
        };
    }
}
