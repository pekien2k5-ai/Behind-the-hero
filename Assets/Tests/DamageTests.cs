using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;
using NUnit.Framework;

namespace BTH.Core.Tests
{
    [TestFixture]
    public sealed class DamageTests
    {
        private static UnitState Spawn(GameState state, UnitDefinition definition)
        {
            return state.Units.Add(definition, new GridCoord(3, 3));
        }

        private static DamageResult Hit(GameState state, UnitState target, int power,
            ElementId element)
        {
            return state.Damage.Apply(new DamageRequest(UnitId.None, target.Id, power, element),
                state);
        }

        [Test]
        public void Armor_Subtracts_AfterElement()
        {
            var state = TestWorld.State();
            var target = Spawn(state, TestWorld.Unit("imp", UnitFaction.Enemy, hp: 20, armor: 2));

            var result = Hit(state, target, 4, ElementId.Physical);

            Assert.That(result.FinalDamage, Is.EqualTo(2));
            Assert.That(target.Hp, Is.EqualTo(18));
        }

        [Test]
        public void Guard_ReducesToMinimumOne()
        {
            var state = TestWorld.State();
            var target = Spawn(state, TestWorld.Unit("imp", UnitFaction.Enemy, hp: 20));
            target.Statuses.Add(new GuardStatus(), TestWorld.StatusContextFor(target, state));

            var result = Hit(state, target, 4, ElementId.Physical);

            Assert.That(result.WasGuarded, Is.True);
            Assert.That(result.FinalDamage, Is.EqualTo(1));
        }

        [Test]
        public void Stagger_Doubles_And_Flanked_Multiplies()
        {
            var state = TestWorld.State();
            var staggered = Spawn(state, TestWorld.Unit("imp-a", UnitFaction.Enemy, hp: 20));
            staggered.Statuses.Add(new StaggerStatus(), TestWorld.StatusContextFor(staggered, state));
            var flanked = state.Units.Add(TestWorld.Unit("imp-b", UnitFaction.Enemy, hp: 20),
                new GridCoord(5, 5));
            flanked.Statuses.Add(new FlankedStatus(), TestWorld.StatusContextFor(flanked, state));

            Assert.That(Hit(state, staggered, 4, ElementId.Physical).FinalDamage, Is.EqualTo(8));
            Assert.That(Hit(state, flanked, 4, ElementId.Physical).FinalDamage, Is.EqualTo(6));
        }

        [Test]
        public void Holy_VersusNecrotic_DealsFiftyPercentMore()
        {
            var state = TestWorld.State();
            var target = Spawn(state, TestWorld.Unit("undead", UnitFaction.Enemy, hp: 20,
                element: ElementId.Necrotic));

            var result = Hit(state, target, 4, ElementId.Holy);

            Assert.That(result.FinalDamage, Is.EqualTo(6));
            Assert.That(result.Multiplier, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void IgnoresArmor_Skips_Reduction()
        {
            var state = TestWorld.State();
            var target = Spawn(state, TestWorld.Unit("imp", UnitFaction.Enemy, hp: 20, armor: 5));

            var result = state.Damage.Apply(new DamageRequest(UnitId.None, target.Id, 6,
                ElementId.Piercing, false, true), state);

            Assert.That(result.FinalDamage, Is.EqualTo(6));
        }

        [Test]
        public void Lethal_Damage_Kills_AndPublishesDeath()
        {
            var events = new EventBus();
            var state = TestWorld.State(events: events);
            var target = Spawn(state, TestWorld.Unit("imp", UnitFaction.Enemy, hp: 3));

            int died = 0;
            using (events.Subscribe<UnitDiedEvent>((in UnitDiedEvent e) =>
                   {
                       if (e.Unit == target.Id)
                       {
                           died++;
                       }
                   }))
            {
                var result = Hit(state, target, 10, ElementId.Physical);
                Assert.That(result.Killed, Is.True);
            }

            Assert.That(target.IsAlive, Is.False);
            Assert.That(target.Hp, Is.EqualTo(0));
            Assert.That(died, Is.EqualTo(1));
        }

        [Test]
        public void ComputePower_RoundsHalfAwayFromZero()
        {
            Assert.That(CombatMath.ComputePower(5, 0, 0.5f, true, 1f), Is.EqualTo(3),
                "2.5 phải làm tròn lên 3, không phải banker's 2.");
            Assert.That(CombatMath.ComputePower(5, 0, 0.7f, true, 1f), Is.EqualTo(4));
        }
    }
}
