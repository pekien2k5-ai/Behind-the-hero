using System.Collections.Generic;
using BTH.Core.Ai;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Turn;
using BTH.Core.Units;
using NUnit.Framework;

namespace BTH.Core.Tests
{
    [TestFixture]
    public sealed class AiDeterminismTests
    {
        private static EnemyIntent Intent(UnitId unit, params GridCoord[] danger)
        {
            return new EnemyIntent(unit, "boss:cleave", new GridCoord(3, 3), danger,
                new List<IGameEffect>(), 1);
        }

        [Test]
        public void Telegraph_DangerCells_SortedByYThenX()
        {
            var events = new EventBus();
            var telegraph = new TelegraphService(events);
            var unit = new UnitId(1);

            telegraph.Declare(Intent(unit, new GridCoord(2, 1), new GridCoord(0, 0),
                new GridCoord(1, 1)));

            Assert.That(telegraph.DangerCells, Is.EqualTo(new[]
            {
                new GridCoord(0, 0), new GridCoord(1, 1), new GridCoord(2, 1)
            }));
        }

        [Test]
        public void Telegraph_DeclareTwice_Replaces_AndClearPublishesCount()
        {
            var events = new EventBus();
            var telegraph = new TelegraphService(events);
            var unit = new UnitId(1);
            int cleared = 0;
            using (events.Subscribe<TelegraphClearedEvent>((in TelegraphClearedEvent e) => cleared = e.Count))
            {
                telegraph.Declare(Intent(unit, new GridCoord(1, 1)));
                telegraph.Declare(Intent(unit, new GridCoord(4, 4)));
                Assert.That(telegraph.DangerCells, Is.EqualTo(new[] { new GridCoord(4, 4) }));
                Assert.That(telegraph.TryGet(unit, out _), Is.True);

                telegraph.Clear();
            }

            Assert.That(telegraph.DangerCells.Count, Is.EqualTo(0));
            Assert.That(cleared, Is.EqualTo(1));
        }

        [Test]
        public void BossPhase_EntersAtThreshold_AndLocksArena()
        {
            var events = new EventBus();
            var state = TestWorld.State(events: events);
            var boss = state.Units.Add(TestWorld.Boss("malkor", hp: 45), new GridCoord(4, 4));
            var arena = new[] { new GridCoord(1, 1), new GridCoord(2, 2) };
            var controller = new BossPhaseController(state.Rules, _ => arena);

            var changed = 0;
            using (events.Subscribe<BossPhaseChangedEvent>((in BossPhaseChangedEvent e) => changed++))
            {
                Assert.That(controller.ShouldEnterPhase2(45, 45), Is.False);
                Assert.That(controller.ShouldEnterPhase2(31, 45), Is.True);

                controller.EnterPhase2(state);
                controller.EnterPhase2(state);
            }

            Assert.That(boss.IsAlive, Is.True);
            Assert.That(controller.Phase, Is.EqualTo(2));
            Assert.That(state.Grid.IsLocked(new GridCoord(1, 1)), Is.True);
            Assert.That(state.Grid.IsLocked(new GridCoord(2, 2)), Is.True);
            Assert.That(changed, Is.EqualTo(1), "Vào phase 2 chỉ một lần.");
        }

        [Test]
        public void Initiative_Order_IsRepeatable()
        {
            var state = TestWorld.State();
            var a = state.Units.Add(TestWorld.Unit("a", UnitFaction.Enemy, initiative: 7),
                new GridCoord(0, 0));
            var b = state.Units.Add(TestWorld.Unit("b", UnitFaction.Enemy, initiative: 7),
                new GridCoord(1, 0));
            var c = state.Units.Add(TestWorld.Unit("c", UnitFaction.Enemy, initiative: 9),
                new GridCoord(2, 0));
            var policy = new InitiativePolicy();
            var alive = state.Units.Alive(UnitFaction.Enemy);

            var first = policy.Order(alive);
            var second = policy.Order(alive);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.EqualTo(new[] { c.Id, a.Id, b.Id }));
            Assert.That(state.Turn, Is.Not.Null);
        }
    }
}
