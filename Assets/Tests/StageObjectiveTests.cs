using BTH.Core.Actions;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Turn;
using BTH.Core.Units;
using NUnit.Framework;

namespace BTH.Core.Tests
{
    [TestFixture]
    public sealed class StageObjectiveTests
    {
        private static StageDefinition BasicStage(int turnLimit = 25)
        {
            var stage = TestWorld.Stage(turnLimit: turnLimit);
            stage.Player = TestWorld.Unit("hero", UnitFaction.Player, hp: 12, attack: 4);
            stage.PlayerSpawn = new GridCoord(0, 0);
            return stage;
        }

        private static GameSession StartSession(StageDefinition stage)
        {
            var session = new GameSession();
            session.RegisterStage(stage);
            session.StartStage(stage.Id);
            return session;
        }

        [Test]
        public void StartStage_OpensPlayerPhase_WithFullActionPoints()
        {
            var session = StartSession(BasicStage());

            Assert.That(session.State, Is.Not.Null);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.Player));
            Assert.That(session.State.Turn.TurnIndex, Is.EqualTo(1));
            Assert.That(session.State.Units.Player.Position, Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(session.State.Units.Player.ActionPoints, Is.EqualTo(2));
        }

        [Test]
        public void Move_SpendsOneActionPoint_AndRelocates()
        {
            var session = StartSession(BasicStage());
            var player = session.State.Units.Player;

            var result = session.TryExecute(ActionRequest.Move(player.Id, new GridCoord(2, 0)));

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(player.Position, Is.EqualTo(new GridCoord(2, 0)));
            Assert.That(player.ActionPoints, Is.EqualTo(1));
            Assert.That(player.HasMovedThisTurn, Is.True);
        }

        [Test]
        public void Move_IntoWall_IsRejectedAsCellBlocked()
        {
            var stage = BasicStage();
            stage.Terrains.Add(new TerrainOverride(new GridCoord(1, 0), TerrainId.Wall,
                TerrainFlags.BlocksMove | TerrainFlags.BlocksLineOfSight));
            var session = StartSession(stage);
            var player = session.State.Units.Player;

            var result = session.TryExecute(ActionRequest.Move(player.Id, new GridCoord(1, 0)));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ActionError.CellBlocked));
            Assert.That(player.Position, Is.EqualTo(new GridCoord(0, 0)));
        }

        [Test]
        public void Attack_NonAdjacentTarget_IsRejectedAsOutOfRange()
        {
            var stage = BasicStage();
            var boss = TestWorld.Boss("doa-phu", hp: 10);
            stage.Spawns.Add(new UnitSpawnDefinition(boss, new GridCoord(3, 0)));
            stage.BossId = boss.Id;
            var session = StartSession(stage);
            var player = session.State.Units.Player;

            var result = session.TryExecute(ActionRequest.Attack(player.Id, new GridCoord(3, 0)));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ActionError.OutOfRange));
        }

        [Test]
        public void Attack_KillsBoss_EndsStageWithVictory()
        {
            var stage = BasicStage();
            var boss = TestWorld.Boss("doa-phu", hp: 1);
            stage.Spawns.Add(new UnitSpawnDefinition(boss, new GridCoord(1, 0)));
            stage.BossId = boss.Id;
            var session = StartSession(stage);
            var player = session.State.Units.Player;
            var bossUnit = session.State.Units.Boss;

            var result = session.TryExecute(ActionRequest.Attack(player.Id, bossUnit.Position));

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(bossUnit.IsAlive, Is.False);
            Assert.That(session.State.Result, Is.EqualTo(StageResult.Victory));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.StageEnded));
        }

        [Test]
        public void TurnLimit_Expired_EndsStageWithDefeat()
        {
            var session = StartSession(BasicStage(turnLimit: 1));

            var result = session.EndTurn();

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(session.State.Result, Is.EqualTo(StageResult.DefeatTurnLimit));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.StageEnded));
        }

        [Test]
        public void PlayerDeath_EndsStageWithDefeat()
        {
            var stage = BasicStage();
            var boss = TestWorld.Boss("doa-phu", hp: 20);
            stage.Spawns.Add(new UnitSpawnDefinition(boss, new GridCoord(6, 6)));
            stage.BossId = boss.Id;
            var session = StartSession(stage);
            var player = session.State.Units.Player;

            session.State.Damage.Apply(new DamageRequest(UnitId.None, player.Id, 999,
                ElementId.Physical), session.State);
            var result = session.EndTurn();

            Assert.That(player.IsAlive, Is.False);
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(session.State.Result, Is.EqualTo(StageResult.DefeatPlayerDead));
        }
    }
}
