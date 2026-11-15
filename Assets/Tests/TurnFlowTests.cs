using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Turn;
using BTH.Core.Units;
using NUnit.Framework;

namespace BTH.Core.Tests
{
    [TestFixture]
    public sealed class TurnFlowTests
    {
        [Test]
        public void Initiative_HighFirst_ThenUnitId()
        {
            var state = TestWorld.State();
            var player = state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player, initiative: 12),
                new GridCoord(0, 0));
            var first = state.Units.Add(TestWorld.Unit("imp-a", UnitFaction.Enemy, initiative: 10),
                new GridCoord(5, 5));
            var second = state.Units.Add(TestWorld.Unit("imp-b", UnitFaction.Enemy, initiative: 10),
                new GridCoord(6, 5));
            var slow = state.Units.Add(TestWorld.Unit("imp-c", UnitFaction.Enemy, initiative: 5),
                new GridCoord(7, 5));

            state.Turn.StartStage(TestWorld.Stage());
            state.Turn.BeginEnemyPhase();

            Assert.That(player.IsAlive, Is.True);
            Assert.That(state.Turn.EnemyOrder, Is.EqualTo(new[] { first.Id, second.Id, slow.Id }));
        }

        [Test]
        public void PlayerPhase_GrantsAp_AndSpendRaisesEvent()
        {
            var events = new EventBus();
            var state = TestWorld.State(events: events);
            var player = state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player),
                new GridCoord(0, 0));

            state.Turn.StartStage(TestWorld.Stage());
            state.Turn.BeginPlayerPhase();

            Assert.That(state.Turn.Phase, Is.EqualTo(TurnPhase.Player));
            Assert.That(state.Turn.TurnIndex, Is.EqualTo(1));
            Assert.That(player.ActionPoints, Is.EqualTo(2));

            int events100 = 0;
            using (events.Subscribe<ApChangedEvent>((in ApChangedEvent e) =>
                   {
                       if (e.Unit == player.Id)
                       {
                           events100++;
                       }
                   }))
            {
                Assert.That(state.Turn.TrySpend(player.Id, 1), Is.True);
            }

            Assert.That(player.ActionPoints, Is.EqualTo(1));
            Assert.That(events100, Is.EqualTo(1));
        }

        [Test]
        public void Staggered_Unit_CannotSpendAp()
        {
            var state = TestWorld.State();
            var player = state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player),
                new GridCoord(0, 0));
            player.Statuses.Add(new StaggerStatus(), TestWorld.StatusContextFor(player, state));

            Assert.That(state.Turn.CanSpend(player.Id, 1), Is.False);
        }

        [Test]
        public void EndTurn_IncrementsTurn_AndAppliesPoison()
        {
            var state = TestWorld.State();
            var player = state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player, hp: 12, initiative: 9),
                new GridCoord(0, 0));

            state.Turn.StartStage(TestWorld.Stage());
            state.Turn.BeginPlayerPhase();
            player.Statuses.Add(new PoisonStatus(3, 2), TestWorld.StatusContextFor(player, state));

            state.Turn.EndTurn();
            Assert.That(state.Turn.TurnIndex, Is.EqualTo(2));
            Assert.That(player.Hp, Is.EqualTo(12), "Status vừa gắn chưa tick ở cuối lượt hiện tại.");

            state.Turn.EndTurn();
            Assert.That(player.Hp, Is.EqualTo(9), "Poison 3 HP mỗi lượt chủ thể.");
            Assert.That(player.Statuses.GetRemaining(StatusIds.Poison), Is.EqualTo(1));
        }

        [Test]
        public void BeginEnemyPhase_ResetsAp_AndOrdersEnemies()
        {
            var state = TestWorld.State();
            var player = state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player),
                new GridCoord(0, 0));
            var enemy = state.Units.Add(TestWorld.Unit("imp", UnitFaction.Enemy),
                new GridCoord(5, 5));

            state.Turn.StartStage(TestWorld.Stage());
            state.Turn.BeginPlayerPhase();
            state.Turn.TrySpend(player.Id, 2);
            state.Turn.TrySpend(enemy.Id, 1);

            state.Turn.BeginEnemyPhase();
            Assert.That(state.Turn.Phase, Is.EqualTo(TurnPhase.Enemy));
            Assert.That(enemy.ActionPoints, Is.EqualTo(2));
            Assert.That(state.Turn.NextEnemy(), Is.EqualTo(enemy.Id));
            Assert.That(state.Turn.NextEnemy(), Is.EqualTo(UnitId.None));
        }
    }
}
