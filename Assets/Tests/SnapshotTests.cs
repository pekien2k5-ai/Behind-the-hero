using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;
using NUnit.Framework;

namespace BTH.Core.Tests
{
    [TestFixture]
    public sealed class SnapshotTests
    {
        [Test]
        public void Capture_Restore_RoundTripsWholeState()
        {
            var state = TestWorld.State();
            var player = state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player, hp: 12),
                new GridCoord(1, 1));
            var enemy = state.Units.Add(TestWorld.Unit("imp", UnitFaction.Enemy, hp: 8),
                new GridCoord(4, 4));
            player.Statuses.Add(new GuardStatus(), TestWorld.StatusContextFor(player, state));
            state.Units.MoveTo(enemy.Id, new GridCoord(5, 5));
            state.Grid.SetTerrain(new GridCoord(2, 2), TerrainId.Cracked, TerrainFlags.None, 2);
            state.Grid.LockArea(new[] { new GridCoord(6, 6), new GridCoord(7, 7) });

            var snapshot = state.Snapshots.Capture();

            state.Damage.Apply(new DamageRequest(UnitId.None, player.Id, 5, ElementId.Physical), state);
            state.Units.MoveTo(player.Id, new GridCoord(3, 3));
            state.Grid.SetTerrain(new GridCoord(2, 2), TerrainId.Floor, TerrainFlags.None);
            state.Grid.UnlockArea();

            state.Snapshots.Restore(snapshot);
            var after = state.Snapshots.Capture();

            Assert.That(after.ContentEquals(snapshot), Is.True, "Restore(Capture()) phải trùng từng trường.");
            Assert.That(state.Units.Player.Position, Is.EqualTo(new GridCoord(1, 1)));
            Assert.That(state.Units.Player.Hp, Is.EqualTo(12));
            Assert.That(state.Units.Get(enemy.Id).Position, Is.EqualTo(new GridCoord(5, 5)));
            Assert.That(state.Grid.Get(new GridCoord(2, 2)).Terrain.Value, Is.EqualTo(TerrainId.Cracked.Value));
            Assert.That(state.Grid.IsLocked(new GridCoord(6, 6)), Is.True);
        }

        [Test]
        public void Rewind_ReturnsToLatestCheckpoint_AndRaisesEvent()
        {
            var events = new EventBus();
            var state = TestWorld.State(events: events);
            var player = state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player, hp: 12),
                new GridCoord(0, 0));

            state.Snapshots.PushCheckpoint(SnapshotTrigger.TurnStart);
            Assert.That(state.Snapshots.CanRewind, Is.True);

            state.Damage.Apply(new DamageRequest(UnitId.None, player.Id, 5, ElementId.Physical), state);
            state.Units.MoveTo(player.Id, new GridCoord(3, 0));

            int rewinds = 0;
            SnapshotTrigger seen = SnapshotTrigger.StageStart;
            using (events.Subscribe<RewindPerformedEvent>((in RewindPerformedEvent e) =>
                   {
                       rewinds++;
                       seen = e.Trigger;
                   }))
            {
                state.Snapshots.Rewind();
            }

            Assert.That(state.Units.Player.Hp, Is.EqualTo(12));
            Assert.That(state.Units.Player.Position, Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(rewinds, Is.EqualTo(1));
            Assert.That(seen, Is.EqualTo(SnapshotTrigger.TurnStart));
            Assert.That(state.Snapshots.CanRewind, Is.False);
        }

        [Test]
        public void RunKnowledge_IsNotRemovedByRewind()
        {
            var state = TestWorld.State();
            state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player, hp: 12), new GridCoord(0, 0));
            state.Snapshots.PushCheckpoint(SnapshotTrigger.TurnStart);

            state.Knowledge.MarkSeen(RunKnowledge.BossSkillSeenPrefix + "doa-phu");
            state.Knowledge.RegisterDeath();

            state.Snapshots.Rewind();

            Assert.That(state.Knowledge.HasSeen(RunKnowledge.BossSkillSeenPrefix + "doa-phu"),
                Is.True);
            Assert.That(state.Knowledge.Deaths, Is.EqualTo(1));
        }

        [Test]
        public void Store_KeepsOnlyLatestCapacity()
        {
            var state = TestWorld.State();
            state.Units.Add(TestWorld.Unit("hero", UnitFaction.Player), new GridCoord(0, 0));
            var store = new SnapshotStore(3);

            for (int i = 0; i < 6; i++)
            {
                store.Push(state.Snapshots.Capture());
            }

            Assert.That(store.Capacity, Is.EqualTo(3));
            Assert.That(store.Count, Is.EqualTo(3));
            Assert.That(store.TryGetLatest(out var latest), Is.True);
            Assert.That(latest, Is.Not.Null);
            Assert.That(state.Grid.Width, Is.EqualTo(8));
        }
    }
}
