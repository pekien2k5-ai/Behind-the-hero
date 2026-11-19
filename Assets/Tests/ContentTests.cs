using System.IO;
using System.Linq;
using BTH.Content;
using BTH.Content.Dialogues;
using BTH.Content.Duels;
using BTH.Content.Skills;
using BTH.Content.Stages;
using BTH.Content.Terrain;
using BTH.Content.Units;
using BTH.Core.Actions;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Turn;
using NUnit.Framework;

namespace BTH.Core.Tests
{
    [TestFixture]
    public sealed class ContentTests
    {
        [Test]
        public void Catalogs_ContainAllExpectedUnitsSkillsStagesAndTerrains()
        {
            Assert.AreEqual(9, UnitCatalog.All().Count);
            Assert.GreaterOrEqual(SkillCatalog.All().Count, 24);
            Assert.AreEqual(5, StageCatalog.All().Count);
            Assert.AreEqual(5, DuelCatalog.All().Count);
            Assert.AreEqual(8, TerrainCatalog.All.Count);
        }

        [Test]
        public void AllFiveStages_CanStartAndDeclareTelegraphIntents()
        {
            var session = ContentCatalog.CreateSession();
            foreach (var stage in StageCatalog.All())
            {
                session.StartStage(stage.Id);
                Assert.IsNotNull(session.State.Units.Player);
                Assert.IsNotNull(session.State.Units.Boss);
                Assert.AreEqual(TurnPhase.Player, session.State.Turn.Phase);
                Assert.Greater(session.State.Telegraph.DangerCells.Count, 0, $"Stage {stage.Id} should have danger cells");
            }
        }

        [Test]
        public void Stage1_CanBePlayedFromBoardToDuelVictory_WithoutUnity()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-1");

            var player = session.State.Units.Player;
            var boss = session.State.Units.Boss;
            Assert.IsNotNull(player);
            Assert.IsNotNull(boss);

            var moveRes = session.TryExecute(ActionRequest.Move(player.Id, new GridCoord(3, 2)));
            Assert.IsTrue(moveRes.Success, moveRes.Message);

            var hit1 = session.TryExecute(ActionRequest.Skill(player.Id, "swift-slash", boss.Position));
            Assert.IsTrue(hit1.Success, hit1.Message);

            session.EndTurn();
            var hit2 = session.TryExecute(ActionRequest.Skill(player.Id, "swift-slash", boss.Position));
            Assert.IsTrue(hit2.Success, hit2.Message);

            if (session.State.Turn.Phase != TurnPhase.Duel)
            {
                session.EndTurn();
                var hit3 = session.TryExecute(ActionRequest.Attack(player.Id, boss.Position));
                Assert.IsTrue(hit3.Success, hit3.Message);
            }

            Assert.AreEqual(TurnPhase.Duel, session.State.Turn.Phase);
            Assert.IsTrue(session.State.Duel.IsActive);

            int safety = 0;
            while (!session.State.IsOver && safety++ < 10)
            {
                var options = session.State.Duel.GetOptions();
                var winOpt = options.First(o => o.Prediction.Result == ClashResult.Win);
                var duelRes = session.TryExecute(ActionRequest.ClashSkill(player.Id, winOpt.SkillId));
                Assert.IsTrue(duelRes.Success, duelRes.Message);
            }

            Assert.AreEqual(StageResult.Victory, session.State.Result);
        }

        [Test]
        public void Stage5_OutroDialogue_ValidatesAndSupportsBothEndings()
        {
            var graph = DialogueCatalog.GetOutro("stage-5");
            Assert.IsNotNull(graph);
            Assert.IsEmpty(graph.Validate());

            var knowledge = new RunKnowledge();
            var runnerA = new DialogueRunner(graph, knowledge);
            runnerA.Start();
            runnerA.Choose(0);
            runnerA.Advance();
            Assert.AreEqual(StageResult.EndingA, runnerA.Ending);

            var runnerB = new DialogueRunner(graph, knowledge);
            runnerB.Start();
            runnerB.Choose(1);
            runnerB.Advance();
            Assert.AreEqual(StageResult.EndingB, runnerB.Ending);
        }

        [Test]
        public void Stage2_DuelWin_AppliesPoisonReverseToPlagueWitch()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-2");
            session.EnterDuel();

            var boss = session.State.Units.Boss;
            var winOpt = session.State.Duel.GetOptions().First(o => o.Prediction.Result == ClashResult.Win);
            session.TryExecute(ActionRequest.ClashSkill(session.State.Units.Player.Id, winOpt.SkillId));
            Assert.IsTrue(boss.Statuses.Has(StatusIds.Poison));
        }

        [Test]
        public void Stage3_DuelWin_AppliesFlankedToAbyssKnight()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-3");
            session.EnterDuel();

            var boss = session.State.Units.Boss;
            var winOpt = session.State.Duel.GetOptions().First(o => o.Prediction.Result == ClashResult.Win);
            session.TryExecute(ActionRequest.ClashSkill(session.State.Units.Player.Id, winOpt.SkillId));
            Assert.Less(boss.Hp, boss.MaxHp);
        }

        [Test]
        public void Stage4_MalkorBoss_Occupies2x2FootprintAndDuelHas3Finishers()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-4");
            var boss = session.State.Units.Boss;
            Assert.AreEqual(2, boss.SizeX);
            Assert.AreEqual(2, boss.SizeY);
            Assert.AreEqual(4, boss.Footprint.Count);
            Assert.AreEqual(3, session.State.Stage.Duel.FinisherCount);
        }

        [Test]
        public void Stage1_CrackedFloor_CollapsesToPitAfterThreeTurns()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-1");
            var crackedCoord = new GridCoord(3, 4);
            Assert.AreEqual(TerrainId.Cracked, session.State.Grid.Get(crackedCoord).Terrain);

            session.EndTurn();
            session.EndTurn();
            session.EndTurn();

            Assert.AreEqual(TerrainId.Pit, session.State.Grid.Get(crackedCoord).Terrain);
            Assert.IsTrue(session.State.Grid.Get(crackedCoord).BlocksMove);
        }

        [Test]
        public void TilesetBehaviors_ChestHealsPlayer_AndCrackedTileArmsCollapse()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-1");
            var state = session.State;
            var player = state.Units.Player;

            state.Damage.Apply(new DamageRequest(state.Units.Boss.Id, player.Id, 6, ElementId.Physical, ignoresArmor: true), state);
            int hpDamaged = player.Hp;
            Assert.Less(hpDamaged, player.MaxHp);

            var chestCell = new GridCoord(3, 6);
            Assert.AreEqual(TerrainId.Chest, state.Grid.Get(chestCell).Terrain);
            TerrainCatalog.HandleUnitEnteredCell(player, chestCell, state);

            Assert.AreEqual(hpDamaged + ChestTerrainBehaviour.HealAmount, player.Hp);
            Assert.AreEqual(TerrainId.Floor, state.Grid.Get(chestCell).Terrain);

            var crackedCell = new GridCoord(3, 4);
            TerrainCatalog.HandleUnitEnteredCell(player, crackedCell, state);
            Assert.AreEqual(1, state.Grid.Get(crackedCell).TurnsUntilCollapse);
        }

        [Test]
        public void ArtAssetCatalog_SpritesheetsVfxTilesetsAndPalette_AreComplete()
        {
            var sheets = ArtAssetCatalog.GetSpritesheetSpecs();
            Assert.AreEqual(9, sheets.Count);
            foreach (var s in sheets)
            {
                Assert.AreEqual(4, s.States.Count);
                CollectionAssert.AreEqual(new[] { "Idle", "Walk", "Attack", "Death" }, s.States);
            }

            var vfx = ArtAssetCatalog.GetVfxSpecs();
            Assert.AreEqual(12, vfx.Count);

            var palette = ArtAssetCatalog.GetHudColorPalette();
            Assert.AreEqual(5, palette.Count);
            Assert.AreEqual("#E5C158", palette[0].Hex);
            Assert.AreEqual("#2C4270", palette[1].Hex);
            Assert.AreEqual("#6B3BA7", palette[2].Hex);
            Assert.AreEqual("#8B1E1E", palette[3].Hex);
            Assert.AreEqual("#3A3E45", palette[4].Hex);

            var terrains = ArtAssetCatalog.GetRequiredTerrainTypes();
            Assert.AreEqual(8, terrains.Count);
        }

        [Test]
        public void GameMatrixAndRules_MatchGddSection7()
        {
            var rules = GameMatrixCatalog.CreateBalancedRules();
            Assert.AreEqual(2, rules.ApPerTurn);
            Assert.AreEqual(2, rules.MovePoints);
            Assert.AreEqual(25, rules.TurnLimit);
            Assert.AreEqual(0.70f, rules.Phase2Threshold, 0.001f);
            Assert.AreEqual(2.0f, rules.StaggerMultiplier, 0.001f);
            Assert.AreEqual(0.20f, rules.GuardMultiplier, 0.001f);
            Assert.AreEqual(1.50f, rules.FlankMultiplier, 0.001f);

            var matrix = GameMatrixCatalog.GetUnitMatrix();
            Assert.AreEqual(9, matrix.Count);
        }

        [Test]
        public void TacticalNavigator_PreviewsAStarPathAndRewindsState()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-1");

            var reachable = TacticalNavigator.GetPlayerReachableSet(session.State);
            Assert.IsNotNull(reachable);
            Assert.Greater(reachable.Count, 0);

            var path = TacticalNavigator.PreviewPlayerPath(session.State, new GridCoord(3, 2));
            Assert.GreaterOrEqual(path.Count, 2);

            var player = session.State.Units.Player;
            var startPos = player.Position;
            session.TryExecute(ActionRequest.Move(player.Id, new GridCoord(3, 2)));
            Assert.AreEqual(new GridCoord(3, 2), session.State.Units.Player.Position);

            bool rewound = TacticalNavigator.TryRewind(session);
            Assert.IsTrue(rewound);
            Assert.AreEqual(startPos, session.State.Units.Player.Position);
        }

        [Test]
        public void ThreeActsLoreAndAllStageDialogues_AreValid()
        {
            var acts = DialogueCatalog.GetThreeActsLore();
            Assert.AreEqual(3, acts.Count);

            foreach (var stage in StageCatalog.All())
            {
                var intro = DialogueCatalog.GetIntro(stage.Id);
                Assert.IsNotNull(intro, $"Intro dialogue missing for {stage.Id}");
                Assert.IsEmpty(intro.Validate(), $"Intro dialogue invalid for {stage.Id}");

                var outro = DialogueCatalog.GetOutro(stage.Id);
                Assert.IsNotNull(outro, $"Outro dialogue missing for {stage.Id}");
                Assert.IsEmpty(outro.Validate(), $"Outro dialogue invalid for {stage.Id}");
            }
        }

        [Test]
        public void AllArtAndAudioAssets_PhysicallyExistInUnityProject()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            while (!string.IsNullOrEmpty(dir) && !Directory.Exists(Path.Combine(dir, "Assets")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            Assert.IsNotNull(dir, "Không tìm thấy thư mục gốc chứa Assets/");
            string resRoot = Path.Combine(dir, "Assets", "Resources");

            foreach (var sheet in ArtAssetCatalog.GetSpritesheetSpecs())
            {
                string p = Path.Combine(resRoot, sheet.ResourcePath.Replace('/', Path.DirectorySeparatorChar) + ".png");
                Assert.IsTrue(File.Exists(p), $"Thiếu file Spritesheet: {p}");
            }

            foreach (var vfx in ArtAssetCatalog.GetVfxSpecs())
            {
                string p = Path.Combine(resRoot, vfx.ResourcePath.Replace('/', Path.DirectorySeparatorChar) + ".png");
                Assert.IsTrue(File.Exists(p), $"Thiếu file VFX: {p}");
            }

            foreach (var tid in ArtAssetCatalog.GetRequiredTerrainTypes())
            {
                var spec = TerrainCatalog.Get(tid);
                string p = Path.Combine(resRoot, "Art", "Tilesets", spec.TileSpriteName + ".png");
                Assert.IsTrue(File.Exists(p), $"Thiếu file Tile địa hình ({tid.Value}): {p}");
            }
            Assert.IsTrue(File.Exists(Path.Combine(resRoot, "Art", "Tilesets", "dungeon_tileset_atlas.png")));

            foreach (var pid in ArtAssetCatalog.GetPortraitIds())
            {
                string p = Path.Combine(resRoot, "Art", "Portraits", pid + ".png");
                Assert.IsTrue(File.Exists(p), $"Thiếu file Portrait: {p}");
            }
            foreach (var duel in DuelCatalog.All())
            {
                string p = Path.Combine(resRoot, "Art", "Backgrounds", duel.BackgroundId + ".png");
                Assert.IsTrue(File.Exists(p), $"Thiếu file Background Duel: {p}");
            }
        }
    }
}
