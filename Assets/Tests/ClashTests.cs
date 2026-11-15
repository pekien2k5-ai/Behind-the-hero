using System.Collections.Generic;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;
using NUnit.Framework;

namespace BTH.Core.Tests
{
    [TestFixture]
    public sealed class ClashTests
    {
        private static readonly IReadOnlyList<string> SkillIds = new[] { "swift", "protect", "weak" };

        private static GameState World(out UnitState player, out UnitState boss, out SkillLibrary skills)
        {
            var state = TestWorld.State();
            player = state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player), new GridCoord(0, 0));
            boss = state.Units.Add(TestWorld.Boss("doa-phu"), new GridCoord(6, 6));

            skills = new SkillLibrary();
            skills.Add(new SkillDefinition
            {
                Id = "swift", DisplayName = "Swift Slash", Power = 15, IsClashSkill = true,
                Element = ElementId.Physical
            });
            skills.Add(new SkillDefinition
            {
                Id = "protect", DisplayName = "Protect", Power = 15, IsClashSkill = true,
                Element = ElementId.Holy
            });
            skills.Add(new SkillDefinition
            {
                Id = "weak", DisplayName = "Weak", Power = 10, IsClashSkill = false
            });
            state.Skills = skills;
            return state;
        }

        private static ClashDefinition Definition(int bossPower = 15)
        {
            return new ClashDefinition
            {
                BossSkillId = "doa-phu:slash",
                BossPower = bossPower,
                BossElement = ElementId.Physical
            };
        }

        [Test]
        public void RuleTable_DefaultsToThree()
        {
            var table = new ClashRuleTable();
            Assert.That(table.GetBonus("any", "boss", ElementId.Physical, ElementId.Fire),
                Is.EqualTo(3));
        }

        [Test]
        public void RuleTable_Pair_ThenElement_ThenDefault()
        {
            var table = new ClashRuleTable();
            table.Add(new ClashRule("swift", "boss", 5));
            table.AddElement(ElementId.Holy, ElementId.Necrotic, 4);

            Assert.That(table.GetBonus("swift", "boss", ElementId.Physical, ElementId.Physical),
                Is.EqualTo(5));
            Assert.That(table.GetBonus("protect", "boss", ElementId.Holy, ElementId.Necrotic),
                Is.EqualTo(4));
            Assert.That(table.GetBonus("protect", "boss", ElementId.Fire, ElementId.Fire),
                Is.EqualTo(3));
        }

        [Test]
        public void Predict_MatchesResolve_AndUsesEffectivePower()
        {
            var state = World(out var player, out var boss, out var skills);
            var clash = Definition();
            var skill = skills.Get("swift");
            var resolver = new ClashResolver();

            var prediction = resolver.Predict(player, boss, skill, clash);
            var outcome = resolver.Resolve(player, boss, skill, clash);

            Assert.That(prediction.PlayerPower, Is.EqualTo(18));
            Assert.That(prediction.BossPower, Is.EqualTo(15));
            Assert.That(prediction.Bonus, Is.EqualTo(3));
            Assert.That(prediction.Result, Is.EqualTo(ClashResult.Win));
            Assert.That(outcome.PlayerPower, Is.EqualTo(prediction.PlayerPower));
            Assert.That(outcome.Result, Is.EqualTo(ClashResult.Win));
            Assert.That(state.Units.Player, Is.Not.Null);
        }

        [Test]
        public void Resolve_Win_AppliesStaggerToBoss()
        {
            World(out var player, out var boss, out var skills);
            var outcome = new ClashResolver().Resolve(player, boss, skills.Get("swift"), Definition());

            Assert.That(outcome.Result, Is.EqualTo(ClashResult.Win));
            Assert.That(outcome.AppliedToBoss, Is.True);
            Assert.That(outcome.AppliedEffect, Is.Not.Null);
            Assert.That(outcome.AppliedEffect.Id.Value, Is.EqualTo(StatusIds.Stagger.Value));
        }

        [Test]
        public void Resolve_Lose_AppliesEffectToPlayer()
        {
            World(out var player, out var boss, out var skills);
            var clash = Definition(30);
            var outcome = new ClashResolver().Resolve(player, boss, skills.Get("swift"), clash);

            Assert.That(outcome.Result, Is.EqualTo(ClashResult.Lose));
            Assert.That(outcome.AppliedToBoss, Is.False);
            Assert.That(outcome.AppliedEffect, Is.Not.Null);
        }

        [Test]
        public void Resolve_Draw_AppliesNothing()
        {
            World(out var player, out var boss, out var skills);
            var clash = Definition(18);
            var outcome = new ClashResolver().Resolve(player, boss, skills.Get("swift"), clash);

            Assert.That(outcome.Result, Is.EqualTo(ClashResult.Draw));
            Assert.That(outcome.AppliedEffect, Is.Null);
        }

        [Test]
        public void GetOptions_OnlyClashSkills_WithPrediction()
        {
            World(out var player, out var boss, out var skills);
            var options = new ClashResolver().GetOptions(player, boss, SkillIds, skills, Definition());

            Assert.That(options.Count, Is.EqualTo(2));
            Assert.That(options[0].SkillId, Is.EqualTo("swift"));
            Assert.That(options[0].Prediction.Result, Is.EqualTo(ClashResult.Win));
            Assert.That(options[1].SkillId, Is.EqualTo("protect"));
        }
    }
}
