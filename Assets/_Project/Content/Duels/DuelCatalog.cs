using System.Collections.Generic;
using BTH.Core.Combat;

namespace BTH.Content.Duels
{
    public static class DuelCatalog
    {
        public static ClashDefinition CreateClashStage1()
        {
            var rules = new ClashRuleTable { DefaultBonus = 3 };
            rules.Add(new ClashRule("blade-guard", "ashen-fury", 3));
            rules.Add(new ClashRule("swift-slash", "ashen-fury", 3));
            rules.Add(new ClashRule("necrotic-burst", "ashen-fury", 0));

            rules.Add(new ClashRule("blade-guard", "axe-cleave", -2));
            rules.Add(new ClashRule("swift-slash", "axe-cleave", -4));
            rules.Add(new ClashRule("necrotic-burst", "axe-cleave", -4));

            return new ClashDefinition
            {
                BossSkillId = "ashen-fury",
                BossPower = 15,
                BossElement = ElementId.Physical,
                Rules = rules,
                WinEffect = () => new StaggerStatus(),
                LoseEffect = () => new StaggerStatus()
            };
        }

        public static DuelDefinition CreateDuelStage1()
        {
            var clash = CreateClashStage1();
            return new DuelDefinition
            {
                Id = "duel-stage-1",
                BackgroundId = "bg_oakhaven_ruins",
                MusicId = "boss_theme",
                TransitionId = "transition_ashen",
                BossSkillIds = new List<string> { "ashen-fury", "axe-cleave" },
                PlayerSkillIds = new List<string> { "blade-guard", "swift-slash", "necrotic-burst" },
                DrawChipRatio = 0.10f,
                FinisherCount = 1,
                Rules = clash.Rules,
                WinEffect = () => new StaggerStatus(),
                LoseEffect = null
            };
        }

        public static ClashDefinition CreateClashStage2()
        {
            var rules = new ClashRuleTable { DefaultBonus = 3 };
            rules.Add(new ClashRule("necrotic-ward", "plague-storm", 3));
            rules.Add(new ClashRule("necrotic-burst", "plague-storm", 3));
            rules.Add(new ClashRule("blade-guard", "plague-storm", -2));

            rules.Add(new ClashRule("necrotic-ward", "poison-dance", 1));
            rules.Add(new ClashRule("necrotic-burst", "poison-dance", -4));
            rules.Add(new ClashRule("blade-guard", "poison-dance", -7));

            return new ClashDefinition
            {
                BossSkillId = "plague-storm",
                BossPower = 14,
                BossElement = ElementId.Necrotic,
                Rules = rules,
                WinEffect = () => new PoisonStatus(3, 2),
                LoseEffect = () => new PoisonStatus(3, 2)
            };
        }

        public static DuelDefinition CreateDuelStage2()
        {
            var clash = CreateClashStage2();
            return new DuelDefinition
            {
                Id = "duel-stage-2",
                BackgroundId = "bg_catacomb_toxic",
                MusicId = "boss_theme",
                TransitionId = "transition_morbida",
                BossSkillIds = new List<string> { "plague-storm", "poison-dance" },
                PlayerSkillIds = new List<string> { "necrotic-ward", "necrotic-burst", "blade-guard" },
                DrawChipRatio = 0.10f,
                FinisherCount = 1,
                Rules = clash.Rules,
                WinEffect = () => new PoisonStatus(3, 2),
                LoseEffect = null
            };
        }

        public static ClashDefinition CreateClashStage3()
        {
            var rules = new ClashRuleTable { DefaultBonus = 3 };
            rules.Add(new ClashRule("temporal-step", "void-spear-beam", 3));
            rules.Add(new ClashRule("dodge", "void-spear-beam", 3));
            rules.Add(new ClashRule("swift-slash", "void-spear-beam", 3));
            rules.Add(new ClashRule("blade-guard", "void-spear-beam", 0));

            rules.Add(new ClashRule("temporal-step", "spear-thrust", -2));
            rules.Add(new ClashRule("dodge", "spear-thrust", -2));
            rules.Add(new ClashRule("swift-slash", "spear-thrust", -3));
            rules.Add(new ClashRule("blade-guard", "spear-thrust", -5));

            return new ClashDefinition
            {
                BossSkillId = "void-spear-beam",
                BossPower = 16,
                BossElement = ElementId.Piercing,
                Rules = rules,
                WinEffect = () => new FlankedStatus(),
                LoseEffect = () => new StaggerStatus()
            };
        }

        public static DuelDefinition CreateDuelStage3()
        {
            var clash = CreateClashStage3();
            return new DuelDefinition
            {
                Id = "duel-stage-3",
                BackgroundId = "bg_void_arena",
                MusicId = "boss_theme",
                TransitionId = "transition_abyss",
                BossSkillIds = new List<string> { "void-spear-beam", "spear-thrust" },
                PlayerSkillIds = new List<string> { "temporal-step", "swift-slash", "blade-guard" },
                DrawChipRatio = 0.10f,
                FinisherCount = 1,
                Rules = clash.Rules,
                WinEffect = () => new FlankedStatus(),
                LoseEffect = null
            };
        }

        public static ClashDefinition CreateClashStage4()
        {
            var rules = new ClashRuleTable { DefaultBonus = 3 };
            rules.Add(new ClashRule("temporal-step", "hellfire-greatsword", 5));
            rules.Add(new ClashRule("dodge", "hellfire-greatsword", 5));
            rules.Add(new ClashRule("blade-guard", "hellfire-greatsword", 4));
            rules.Add(new ClashRule("swift-slash", "hellfire-greatsword", 1));

            rules.Add(new ClashRule("blade-guard", "fireball", 0));
            rules.Add(new ClashRule("temporal-step", "fireball", -4));
            rules.Add(new ClashRule("dodge", "fireball", -2));
            rules.Add(new ClashRule("swift-slash", "fireball", -3));

            return new ClashDefinition
            {
                BossSkillId = "hellfire-greatsword",
                BossPower = 18,
                BossElement = ElementId.Fire,
                Rules = rules,
                WinEffect = () => new StaggerStatus(),
                LoseEffect = () => new StaggerStatus()
            };
        }

        public static DuelDefinition CreateDuelStage4()
        {
            var clash = CreateClashStage4();
            return new DuelDefinition
            {
                Id = "duel-stage-4",
                BackgroundId = "bg_fire_arena",
                MusicId = "boss_theme",
                TransitionId = "transition_malkor",
                BossSkillIds = new List<string> { "hellfire-greatsword", "fireball" },
                PlayerSkillIds = new List<string> { "temporal-step", "blade-guard", "swift-slash" },
                DrawChipRatio = 0.10f,
                FinisherCount = 3,
                Rules = clash.Rules,
                WinEffect = () => new StaggerStatus(),
                LoseEffect = null
            };
        }

        public static ClashDefinition CreateClashStage5()
        {
            var rules = new ClashRuleTable { DefaultBonus = 3 };
            rules.Add(new ClashRule("blade-guard", "holy-light-slash", 4));
            rules.Add(new ClashRule("swift-slash", "holy-light-slash", 5));
            rules.Add(new ClashRule("necrotic-burst", "holy-light-slash", 0));

            rules.Add(new ClashRule("swift-slash", "holy-strike", 1));
            rules.Add(new ClashRule("blade-guard", "holy-strike", -3));
            rules.Add(new ClashRule("necrotic-burst", "holy-strike", -2));

            return new ClashDefinition
            {
                BossSkillId = "holy-light-slash",
                BossPower = 18,
                BossElement = ElementId.Holy,
                Rules = rules,
                WinEffect = () => new StaggerStatus(),
                LoseEffect = () => new StaggerStatus()
            };
        }

        public static DuelDefinition CreateDuelStage5()
        {
            var clash = CreateClashStage5();
            return new DuelDefinition
            {
                Id = "duel-stage-5",
                BackgroundId = "bg_mausoleum",
                MusicId = "boss_theme",
                TransitionId = "transition_fallen",
                BossSkillIds = new List<string> { "holy-light-slash", "holy-strike" },
                PlayerSkillIds = new List<string> { "blade-guard", "swift-slash", "necrotic-burst" },
                DrawChipRatio = 0.10f,
                FinisherCount = 1,
                Rules = clash.Rules,
                WinEffect = () => new StaggerStatus(),
                LoseEffect = null
            };
        }

        public static IReadOnlyList<DuelDefinition> All() => new List<DuelDefinition>
        {
            CreateDuelStage1(),
            CreateDuelStage2(),
            CreateDuelStage3(),
            CreateDuelStage4(),
            CreateDuelStage5()
        };
    }
}
