using System.Collections.Generic;
using BTH.Core.Actions;
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
    public sealed class DuelFlowTests
    {
        [Test]
        public void EnterDuel_TriggersWhenBossUnderThreshold_AndClearsBoardStatuses()
        {
            var session = CreateDuelSession(Duel());
            var boss = session.State.Units.Boss;

            ApplyDamage(session, boss.Id, 8); // 20 -> 12 (60%)
            ActionResult guard = session.TryExecute(ActionRequest.Guard(session.State.Units.Player.Id));

            Assert.That(guard.Success, Is.True);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.Duel));
            Assert.That(session.State.Duel.IsActive, Is.True);
            Assert.That(session.State.Duel.State.Round, Is.EqualTo(1));
            Assert.That(session.State.Duel.State.BossSkillId, Is.EqualTo("boss-strike"));
            Assert.That(session.State.Duel.State.BossPower, Is.EqualTo(15));
            Assert.That(session.State.Units.Player.IsGuarding, Is.False, "Guard phải bị clear khi vào Duel.");
            Assert.That(session.EndTurn().Error, Is.EqualTo(ActionError.WrongPhase));
        }

        [Test]
        public void Duel_GetOptions_ReturnsThreeSkillsWithBadges()
        {
            var session = CreateDuelSession(Duel());
            EnterDuel(session);

            IReadOnlyList<ClashOption> options = session.State.Duel.GetOptions();

            Assert.That(options.Count, Is.EqualTo(3));
            Assert.That(options[0].SkillId, Is.EqualTo("sword"));
            Assert.That(options[0].Power, Is.EqualTo(17));
            Assert.That(options[0].Prediction.Result, Is.EqualTo(ClashResult.Win));
            Assert.That(options[1].SkillId, Is.EqualTo("burst"));
            Assert.That(options[1].Prediction.Result, Is.EqualTo(ClashResult.Draw));
            Assert.That(options[2].SkillId, Is.EqualTo("heavy"));
            Assert.That(options[2].Prediction.Result, Is.EqualTo(ClashResult.Lose));
        }

        [Test]
        public void ClashWin_DoublesFinisherDamageAndClearsStaggerBeforeNextRound()
        {
            var duel = Duel();
            duel.BossPower = 15;
            var session = CreateDuelSession(duel);
            EnterDuel(session);
            var hero = session.State.Units.Player;
            var boss = session.State.Units.Boss;

            ActionResult result = session.TryExecute(ActionRequest.ClashSkill(hero.Id, "sword"));

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(boss.IsStaggered, Is.False, "Stagger phải hết ngay sau đòn kết liễu.");
            Assert.That(session.State.Duel.State.LastResult, Is.EqualTo(ClashResult.Win));
            Assert.That(boss.Hp, Is.EqualTo(4), "4 ATK × 2 (Stagger) = 8, tự đánh ngay.");
            Assert.That(session.State.Duel.State.Round, Is.EqualTo(2), "Tự sang vòng sau.");

            Assert.That(session.TryExecute(ActionRequest.ClashSkill(hero.Id, "burst")).Success, Is.True);
            Assert.That(session.State.Duel.State.LastResult, Is.EqualTo(ClashResult.Draw));
            Assert.That(boss.Hp, Is.EqualTo(2), "Hoà ở vòng kế chỉ gây 2 HP, không nhân Stagger.");
        }

        [Test]
        public void Attack_InDuel_IsRejected_BecauseFinisherIsAutomatic()
        {
            var session = CreateDuelSession(Duel());
            EnterDuel(session);
            var hero = session.State.Units.Player;
            var boss = session.State.Units.Boss;

            ActionResult result = session.TryExecute(ActionRequest.Attack(hero.Id, boss.Position));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ActionError.WrongPhase));
            Assert.That(boss.Hp, Is.EqualTo(12), "Không có đòn đánh tay trong Duel.");
        }

        [Test]
        public void Draw_ChipsTenPercent_AndAdvancesRound()
        {
            var session = CreateDuelSession(Duel());
            EnterDuel(session);
            var hero = session.State.Units.Player;
            var boss = session.State.Units.Boss;

            ActionResult result = session.TryExecute(ActionRequest.ClashSkill(hero.Id, "burst"));

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(hero.Hp, Is.EqualTo(10), "12 - round(15 × 0.10) = 10.");
            Assert.That(boss.Hp, Is.EqualTo(10), "Boss vào Duel ở 12 HP, hoà mất thêm 2.");
            Assert.That(session.State.Duel.State.LastResult, Is.EqualTo(ClashResult.Draw));
            Assert.That(session.State.Duel.State.Round, Is.EqualTo(2));
            Assert.That(session.State.Duel.State.BossSkillId, Is.EqualTo("boss-strike-2"));
        }

        [Test]
        public void Lose_TakesBossPower_AndEndsDuelWhenPlayerDies()
        {
            var session = CreateDuelSession(Duel());
            EnterDuel(session);
            var hero = session.State.Units.Player;
            var boss = session.State.Units.Boss;

            ActionResult result = session.TryExecute(ActionRequest.ClashSkill(hero.Id, "heavy"));

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(hero.IsAlive, Is.False);
            Assert.That(session.State.Duel.State.IsOver, Is.True);
            Assert.That(session.State.Duel.State.Winner, Is.EqualTo(boss.Id));
            Assert.That(session.State.Result, Is.EqualTo(StageResult.DefeatPlayerDead));
        }

        [Test]
        public void DuelDefeat_CanRetryFromRoundOneWhileKeepingDeathsAndKnowledge()
        {
            var duel = Duel();
            duel.BossPower = 15;
            var session = CreateDuelSession(duel);
            var state = session.State;
            SnapshotTrigger lastRewind = SnapshotTrigger.StageStart;
            using var subscription = session.Events.Subscribe<RewindPerformedEvent>((in RewindPerformedEvent e) =>
            {
                lastRewind = e.Trigger;
            });
            EnterDuel(session);

            Assert.That(session.TryExecute(ActionRequest.ClashSkill(state.Units.Player.Id, "burst")).Success, Is.True);
            state.Snapshots.Rewind();
            Assert.That(lastRewind, Is.EqualTo(SnapshotTrigger.ClashStart));
            Assert.That(state.Duel.State.Round, Is.EqualTo(1));

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                var heroId = state.Units.Player.Id;
                Assert.That(session.TryExecute(ActionRequest.ClashSkill(heroId, "burst")).Success, Is.True);
                Assert.That(session.TryExecute(ActionRequest.ClashSkill(heroId, "heavy")).Success, Is.True);
                Assert.That(state.Result, Is.EqualTo(StageResult.DefeatPlayerDead));
                state.Units.Kill(heroId);
                Assert.That(state.Knowledge.Deaths, Is.EqualTo(attempt));

                state.Snapshots.Rewind();
                Assert.That(lastRewind, Is.EqualTo(SnapshotTrigger.ClashStart));
                Assert.That(state.Result, Is.EqualTo(StageResult.None));
                Assert.That(state.Turn.Phase, Is.EqualTo(TurnPhase.Duel));
                Assert.That(state.BossPhase.Phase, Is.EqualTo(2));
                Assert.That(state.Duel.State.Round, Is.EqualTo(1));
                Assert.That(state.Units.Player.Hp, Is.EqualTo(12));
                Assert.That(state.Units.Boss.Hp, Is.EqualTo(12));
                Assert.That(state.Knowledge.Deaths, Is.EqualTo(attempt));
                Assert.That(state.Knowledge.HasSeen(RunKnowledge.BossSkillSeenPrefix + "boss-strike-2"), Is.True);
            }
        }

        [Test]
        public void RewindToBoard_RestoresBossPhaseAndNotifiesOnReentry()
        {
            var session = CreateDuelSession(Duel());
            var state = session.State;
            int transitions = 0;
            using var subscription = session.Events.Subscribe<BossPhaseChangedEvent>((in BossPhaseChangedEvent e) =>
            {
                transitions++;
            });
            EnterDuel(session);
            Assert.That(state.Snapshots.Capture().BossPhase, Is.EqualTo(2));

            state.Snapshots.Rewind();
            state.Snapshots.Rewind();

            Assert.That(state.Turn.Phase, Is.EqualTo(TurnPhase.Player));
            Assert.That(state.Duel.IsActive, Is.False);
            Assert.That(state.BossPhase.Phase, Is.EqualTo(1));
            Assert.That(transitions, Is.EqualTo(1));

            EnterDuel(session);
            Assert.That(transitions, Is.EqualTo(2));
        }

        [Test]
        public void DuelKill_EndsStageWithVictory()
        {
            var session = CreateDuelSession(Duel(), bossHp: 10);
            var boss = session.State.Units.Boss;
            ApplyDamage(session, boss.Id, 4); // 10 -> 6 (60%)
            EnterDuel(session);

            var hero = session.State.Units.Player;
            ActionResult clash = session.TryExecute(ActionRequest.ClashSkill(hero.Id, "sword"));

            Assert.That(clash.Success, Is.True, clash.Message);
            Assert.That(boss.IsAlive, Is.False, "Finisher tự động phải giết boss.");
            Assert.That(session.Knowledge.Deaths, Is.Zero);
            Assert.That(session.State.Duel.State.Winner, Is.EqualTo(hero.Id));
            Assert.That(session.State.Result, Is.EqualTo(StageResult.Victory));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.StageEnded));
        }

        [Test]
        public void ClashSkill_InDuel_UsesDuelPath_AndRecordsSeenSkill()
        {
            var clash = new ClashDefinition { BossSkillId = "boss-strike", BossPower = 99 };
            var session = CreateDuelSession(Duel(), clash: clash);
            EnterDuel(session);
            var hero = session.State.Units.Player;

            session.TryExecute(ActionRequest.ClashSkill(hero.Id, "sword"));

            Assert.That(session.State.Duel.State.BossPower, Is.EqualTo(10),
                "Sang vòng 2 lấy uy lực chiêu boss thứ hai, không dùng Stage.Clash.");
            Assert.That(session.State.Knowledge.HasSeen(RunKnowledge.BossSkillSeenPrefix + "boss-strike"),
                Is.True);
        }

        [Test]
        public void Snapshot_Restore_KeepsDuelState()
        {
            var session = CreateDuelSession(Duel());
            EnterDuel(session);
            var hero = session.State.Units.Player;
            var boss = session.State.Units.Boss;
            session.TryExecute(ActionRequest.ClashSkill(hero.Id, "sword"));
            Assert.That(boss.IsAlive, Is.True);

            GameSnapshot snapshot = session.State.Snapshots.Capture();
            Assert.That(snapshot.Duel, Is.Not.Null);
            int capturedBossHp = boss.Hp;
            int capturedRound = session.State.Duel.State.Round;

            ApplyDamage(session, boss.Id, 1);
            Assert.That(boss.Hp, Is.EqualTo(capturedBossHp - 1));

            session.State.Snapshots.Restore(snapshot);

            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.Duel));
            Assert.That(session.State.Units.Boss.Hp, Is.EqualTo(capturedBossHp));
            Assert.That(session.State.Duel.State.Round, Is.EqualTo(capturedRound));
        }

        [Test]
        public void PoisonWinEffect_TicksAtEndOfNextRounds_LikeEndTurn()
        {
            var session = CreateDuelSession(PoisonDuel(), bossHp: 25);
            EnterDuel(session);
            var hero = session.State.Units.Player;
            var boss = session.State.Units.Boss;

            session.TryExecute(ActionRequest.ClashSkill(hero.Id, "sword")); // 17 vs 13 → Win
            Assert.That(boss.Hp, Is.EqualTo(13), "17 - finisher 4; Poison chưa tick ở vòng gắn.");
            Assert.That(boss.Statuses.Has(StatusIds.Poison), Is.True);
            Assert.That(boss.Statuses.GetRemaining(StatusIds.Poison), Is.EqualTo(2));

            session.TryExecute(ActionRequest.ClashSkill(hero.Id, "heavy")); // 13 vs 13 → Draw
            Assert.That(boss.Hp, Is.EqualTo(9), "13 - chip 1 - Poison 3 = 9 ở cuối vòng 2.");
            Assert.That(boss.Statuses.GetRemaining(StatusIds.Poison), Is.EqualTo(1));

            session.TryExecute(ActionRequest.ClashSkill(hero.Id, "heavy")); // Draw
            Assert.That(boss.Hp, Is.EqualTo(5), "9 - chip 1 - Poison 3 = 5 ở cuối vòng 3.");
            Assert.That(boss.Statuses.Has(StatusIds.Poison), Is.False, "Poison 2 lượt đã hết.");
            Assert.That(session.State.Duel.State.Round, Is.EqualTo(4));
        }

        [Test]
        public void ClashRuleTable_PrefersSkillPair_ThenElement_ThenDefault()
        {
            var table = new ClashRuleTable();
            table.Add(new ClashRule("sword", "boss-strike", 5));
            table.AddElement(ElementId.Holy, ElementId.Necrotic, 4);

            Assert.That(table.GetBonus("sword", "boss-strike", ElementId.Physical, ElementId.Physical),
                Is.EqualTo(5));
            Assert.That(table.GetBonus("holy-blade", "boss-strike", ElementId.Holy, ElementId.Necrotic),
                Is.EqualTo(4));
            Assert.That(table.GetBonus("other", "boss-strike", ElementId.Fire, ElementId.Fire),
                Is.EqualTo(3), "Không khớp cặp chiêu/hệ nào thì cộng mặc định 3.");
        }

        [Test]
        public void Snapshot_SeenBossSkills_DoesNotMutateCapturedCheckpoint()
        {
            var session = CreateDuelSession(Duel());
            EnterDuel(session);
            var hero = session.State.Units.Player;
            var duel = session.State.Duel;

            var captured = duel.Capture();
            Assert.That(captured.SeenBossSkills.Count, Is.EqualTo(1));

            session.TryExecute(ActionRequest.ClashSkill(hero.Id, "burst")); // Draw → sang vòng 2

            Assert.That(duel.State.SeenBossSkills.Count, Is.EqualTo(2));
            Assert.That(captured.SeenBossSkills.Count, Is.EqualTo(1),
                "Checkpoint cũ không được bị cộng thêm chiêu mới.");
        }

        [Test]
        public void RoundLimit_Exceeded_EndsStageAsDefeat()
        {
            var duel = Duel();
            duel.RoundLimit = 1;
            duel.BossPower = 13; // heavy 13 vs 13 → Draw, rồi vượt round limit
            var session = CreateDuelSession(duel);
            EnterDuel(session);
            var hero = session.State.Units.Player;

            ActionResult result = session.TryExecute(ActionRequest.ClashSkill(hero.Id, "heavy"));

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(session.State.Duel.State.IsOver, Is.True);
            Assert.That(session.State.Result, Is.EqualTo(StageResult.DefeatTurnLimit));
            Assert.That(session.Knowledge.Deaths, Is.Zero);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.StageEnded),
                "Không được kẹt IsOver mà stage chưa kết thúc.");
        }

        private static void EnterDuel(GameSession session)
        {
            var boss = session.State.Units.Boss;
            if (boss.IsAlive && boss.HpRatio >= session.State.Rules.Phase2Threshold)
            {
                int amount = 8;
                if (amount >= boss.Hp)
                {
                    amount = boss.Hp - 1;
                }

                if (amount > 0)
                {
                    ApplyDamage(session, boss.Id, amount);
                }
            }

            session.TryExecute(ActionRequest.Guard(session.State.Units.Player.Id));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.Duel), "Duel phải bắt đầu.");
        }

        private static void ApplyDamage(GameSession session, UnitId target, int amount)
        {
            var unit = session.State.Units.Get(target);
            session.State.Damage.Apply(new DamageRequest(UnitId.None, target, amount,
                ElementId.None, false, true, unit.Position), session.State);
        }

        private static DuelDefinition Duel()
        {
            var duel = new DuelDefinition { Id = "duel-boss" };
            duel.BossSkillIds.AddRange(new[] { "boss-strike", "boss-strike-2" });
            duel.PlayerSkillIds.AddRange(new[] { "sword", "burst", "heavy" });
            return duel;
        }

        private static DuelDefinition PoisonDuel()
        {
            var duel = new DuelDefinition
            {
                Id = "duel-poison",
                BossPower = 13,
                WinEffect = () => new PoisonStatus(3, 2)
            };
            duel.BossSkillIds.AddRange(new[] { "boss-strike-2", "boss-strike-2" });
            duel.PlayerSkillIds.AddRange(new[] { "sword", "burst", "heavy" });
            return duel;
        }

        private static GameSession CreateDuelSession(DuelDefinition duel, int bossHp = 20,
            int heroHp = 12, ClashDefinition clash = null)
        {
            var skills = new SkillLibrary();
            skills.Add(new SkillDefinition
            {
                Id = "sword", DisplayName = "Chém", Power = 14, Element = ElementId.Physical,
                IsClashSkill = true
            });
            skills.Add(new SkillDefinition
            {
                Id = "burst", DisplayName = "Bộc phá", Power = 12, Element = ElementId.Physical,
                IsClashSkill = true
            });
            skills.Add(new SkillDefinition
            {
                Id = "heavy", DisplayName = "Trọng kích", Power = 10, Element = ElementId.Physical,
                IsClashSkill = true
            });
            skills.Add(new SkillDefinition
            {
                Id = "boss-strike", DisplayName = "Boss đòn", Power = 15, Element = ElementId.Physical,
                IsClashSkill = true
            });
            skills.Add(new SkillDefinition
            {
                Id = "boss-strike-2", DisplayName = "Boss đòn 2", Power = 10, Element = ElementId.Physical,
                IsClashSkill = true
            });

            var session = new GameSession(skills: skills);
            var stage = TestWorld.Stage();
            stage.Player = TestWorld.Unit("hero", UnitFaction.Player, hp: heroHp, attack: 4,
                skillIds: new[] { "sword", "burst", "heavy" });
            stage.PlayerSpawn = new GridCoord(0, 0);
            stage.Spawns.Add(new UnitSpawnDefinition(
                TestWorld.Boss("boss", hp: bossHp, attack: 5), new GridCoord(1, 0)));
            stage.BossId = "boss";
            stage.Duel = duel;
            stage.Clash = clash;
            session.StartStage(stage);
            return session;
        }
    }
}
