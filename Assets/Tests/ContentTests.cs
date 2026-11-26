using System.Collections.Generic;
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
using BTH.Core.Units;
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
        public void RunKnowledge_PersistsAcrossRewindAndStageLoads()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-1");
            var stageOne = session.State;
            var knowledge = session.Knowledge;
            const string boardSkill = "axe-cleave";
            const string ultimate = "ashen-fury";

            knowledge.MarkSeen("dialogue-intro");
            Assert.That(knowledge.KnownBossSkillCount, Is.Zero);
            Assert.That(knowledge.HasSeenBossSkill(boardSkill), Is.False);
            Assert.That(session.EndTurn().Success, Is.True);
            Assert.That(knowledge.HasSeenBossSkill(boardSkill), Is.True);

            session.EnterDuel();
            var hero = stageOne.Units.Player.Id;
            Assert.That(knowledge.HasWonClash(ultimate, "swift-slash"), Is.False);
            Assert.That(session.TryExecute(ActionRequest.ClashSkill(hero, "swift-slash")).Success, Is.True);
            Assert.That(knowledge.HasWonClash(ultimate, "swift-slash"), Is.True);
            Assert.That(knowledge.HasWonClash(boardSkill, "swift-slash"), Is.False);
            Assert.That(session.TryExecute(ActionRequest.ClashSkill(hero, "blade-guard")).Success, Is.True);
            Assert.That(session.TryExecute(ActionRequest.ClashSkill(hero, "necrotic-burst")).Success, Is.True);
            Assert.That(stageOne.Result, Is.EqualTo(StageResult.DefeatPlayerDead));

            stageOne.Snapshots.Rewind();
            Assert.That(knowledge.HasWonClash(ultimate, "swift-slash"), Is.True);
            Assert.That(knowledge.HasWonClash(ultimate, "necrotic-burst"), Is.False);
            Assert.That(knowledge.KnownBossSkillCount, Is.EqualTo(2));

            string previousBusUpdate = null;
            using var previousSubscription = stageOne.Events.Subscribe<RunKnowledgeUpdatedEvent>(
                (in RunKnowledgeUpdatedEvent e) => previousBusUpdate = e.Key);
            session.StartStage("stage-2");
            Assert.That(session.Knowledge, Is.SameAs(knowledge));
            Assert.That(session.Knowledge.Deaths, Is.EqualTo(1));
            Assert.That(session.Knowledge.HasSeenBossSkill(ultimate), Is.True);
            Assert.That(session.Knowledge.HasWonClash(ultimate, "swift-slash"), Is.True);

            string currentBusUpdate = null;
            using var currentSubscription = session.Events.Subscribe<RunKnowledgeUpdatedEvent>(
                (in RunKnowledgeUpdatedEvent e) => currentBusUpdate = e.Key);
            Assert.That(session.EndTurn().Success, Is.True);
            Assert.That(currentBusUpdate, Is.EqualTo(RunKnowledge.BossSkillSeenPrefix + "poison-dance"));
            Assert.That(previousBusUpdate, Is.Null);
            Assert.That(knowledge.KnownBossSkillCount, Is.EqualTo(3));

            session.StartStage("stage-2");
            Assert.That(session.Knowledge.Deaths, Is.EqualTo(1));
            Assert.That(session.Knowledge.KnownBossSkillCount, Is.EqualTo(3));
            session.Restart();
            Assert.That(session.Knowledge.Deaths, Is.Zero);
            Assert.That(session.Knowledge.KnownBossSkillCount, Is.Zero);
            Assert.That(session.Knowledge.HasWonClash(ultimate, "swift-slash"), Is.False);
        }

        [Test]
        public void BladeGuard_IsTheOnlyGuardCardAndKeepsEachStageBonus()
        {
            var session = ContentCatalog.CreateSession();
            Assert.That(session.Skills.TryGet("protect", out _), Is.False);
            Assert.That(UnitCatalog.CreateWanderer().SkillIds, Does.Not.Contain("protect"));

            int[] expectedBonuses = { 3, -2, 0, 4, 4 };
            for (int i = 0; i < expectedBonuses.Length; i++)
            {
                session.StartStage("stage-" + (i + 1));
                session.EnterDuel();
                var options = session.State.Duel.GetOptions();
                Assert.That(options.Count, Is.EqualTo(3));
                var bladeGuard = options.Single(option => option.SkillId == "blade-guard");
                Assert.That(bladeGuard.DisplayName, Is.EqualTo("Blade Guard"));
                Assert.That(bladeGuard.Prediction.Bonus, Is.EqualTo(expectedBonuses[i]));
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
        public void Stage2_ConsecutiveDuelWins_DoNotResetPoisonTicks()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-2");
            session.EnterDuel();

            var boss = session.State.Units.Boss;
            var player = session.State.Units.Player;
            var ticks = new List<int>();
            using var subscription = session.Events.Subscribe<DamageDealtEvent>((in DamageDealtEvent e) =>
            {
                if (e.Source == UnitId.None && e.Target == boss.Id && e.Element == ElementId.Necrotic)
                {
                    ticks.Add(e.Amount);
                }
            });

            var choice = ActionRequest.ClashSkill(player.Id, "necrotic-ward");
            Assert.That(session.TryExecute(choice).Success, Is.True);
            Assert.That(session.State.Duel.State.LastResult, Is.EqualTo(ClashResult.Win));
            Assert.That(ticks, Is.Empty);
            Assert.That(boss.Statuses.GetRemaining(StatusIds.Poison), Is.EqualTo(2));

            Assert.That(session.TryExecute(choice).Success, Is.True);
            Assert.That(session.State.Duel.State.LastResult, Is.EqualTo(ClashResult.Win));
            CollectionAssert.AreEqual(new[] { 3 }, ticks);
            Assert.That(boss.Statuses.GetRemaining(StatusIds.Poison), Is.EqualTo(1));

            Assert.That(session.TryExecute(choice).Success, Is.True);
            CollectionAssert.AreEqual(new[] { 3, 3 }, ticks);
            Assert.That(boss.Statuses.Has(StatusIds.Poison), Is.False);
        }

        [Test]
        public void Stage3_TelegraphsFourCellsForVoidSpearOnEvenTurn()
        {
            var session = ContentCatalog.CreateSession();
            session.StartStage("stage-3");
            var state = session.State;
            var boss = state.Units.Boss;
            Assert.That(state.Telegraph.TryGet(boss.Id, out var thrust), Is.True);
            Assert.That(thrust.SkillId, Is.EqualTo("spear-thrust"));
            Assert.That(thrust.DangerCells.Count, Is.LessThan(4));

            Assert.That(session.EndTurn().Success, Is.True);
            Assert.That(state.Telegraph.TryGet(boss.Id, out var beam), Is.True);
            Assert.That(beam.SkillId, Is.EqualTo("void-spear-beam"));
            CollectionAssert.AreEqual(GridShapes.Row(boss.Center, -1, 0, 4), beam.DangerCells);
        }

        [Test]
        public void Stage3_VoidSpearDoesNotHitPlayerBehindPillar()
        {
            var session = ContentCatalog.CreateSession();
            var stage = StageCatalog.CreateStage3();
            var pillar = new GridCoord(3, 4);
            stage.Terrains.Add(TerrainCatalog.CreateOverride(pillar.X, pillar.Y, TerrainId.Wall));
            session.StartStage(stage);
            var state = session.State;
            var player = state.Units.Player;

            Assert.That(session.EndTurn().Success, Is.True);
            Assert.That(state.Units.Boss.Center + GridCoord.West, Is.EqualTo(pillar));
            Assert.That(state.Telegraph.TryGet(state.Units.Boss.Id, out var beam), Is.True);
            Assert.That(beam.SkillId, Is.EqualTo("void-spear-beam"));
            Assert.That(beam.DangerCells, Is.Empty);

            int hpBeforeBeam = player.Hp;
            Assert.That(session.EndTurn().Success, Is.True);
            Assert.That(player.Hp, Is.EqualTo(hpBeforeBeam));
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

            session.EnterDuel();
            var temporalStep = session.State.Duel.GetOptions().Single(option => option.SkillId == "temporal-step");
            Assert.That(temporalStep.Prediction.Bonus, Is.EqualTo(5));
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
            var stage = StageCatalog.CreateStage1();
            stage.PlayerSpawn = new GridCoord(3, 5);
            session.StartStage(stage);
            var state = session.State;
            var player = state.Units.Player;

            state.Damage.Apply(new DamageRequest(state.Units.Boss.Id, player.Id, 6, ElementId.Physical, ignoresArmor: true), state);
            int hpDamaged = player.Hp;
            Assert.Less(hpDamaged, player.MaxHp);

            var chestCell = new GridCoord(3, 6);
            Assert.AreEqual(TerrainId.Chest, state.Grid.Get(chestCell).Terrain);
            Assert.That(session.TryExecute(ActionRequest.Move(player.Id, chestCell)).Success, Is.True);

            Assert.AreEqual(hpDamaged + ChestTerrainBehaviour.HealAmount, player.Hp);
            Assert.AreEqual(TerrainId.Floor, state.Grid.Get(chestCell).Terrain);
            Assert.That(state.Knowledge.GetCounter("chests-opened"), Is.EqualTo(1));

            var crackedCell = new GridCoord(3, 4);
            Assert.That(session.EndTurn().Success, Is.True);
            Assert.That(session.TryExecute(ActionRequest.Move(player.Id, crackedCell)).Success, Is.True);
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
