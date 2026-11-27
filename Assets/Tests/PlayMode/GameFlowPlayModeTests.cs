using System.Collections;
using System.Linq;
using BTH.Core.Actions;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Turn;
using BTH.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BTH.PlayMode.Tests
{
    [TestFixture]
    public sealed class GameFlowPlayModeTests
    {
        private GameBootstrap _boot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var existing = Object.FindAnyObjectByType<GameBootstrap>();
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
                yield return null;
            }

            var go = new GameObject("BTH_PlayModeTestBootstrap");
            _boot = go.AddComponent<GameBootstrap>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (_boot != null)
            {
                Object.Destroy(_boot.gameObject);
                _boot = null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullFlow_MainMenu_Modals_Cutscene_Board_Pause_DuelHint_Codex_Reset()
        {
            Assert.That(_boot.ScreenState, Is.EqualTo(AppScreenState.MainMenu));
            Assert.That(_boot.HasActiveRun, Is.False);

            _boot.ToggleLoreModal();
            Assert.That(_boot.ShowLoreModal, Is.True);
            _boot.ToggleTutorialModal();
            Assert.That(_boot.ShowLoreModal, Is.False);
            Assert.That(_boot.ShowTutorialModal, Is.True);
            _boot.ToggleTutorialModal();
            Assert.That(_boot.ShowTutorialModal, Is.False);

            _boot.StartNewGame();
            yield return null;
            Assert.That(_boot.ScreenState, Is.EqualTo(AppScreenState.Cutscene));
            Assert.That(_boot.HasActiveRun, Is.True);
            Assert.That(_boot.ActiveDialogue, Is.Not.Null);

            _boot.SkipCutscene();
            yield return null;
            Assert.That(_boot.ScreenState, Is.EqualTo(AppScreenState.Playing));
            Assert.That(_boot.Session.State.Turn.Phase, Is.EqualTo(TurnPhase.Player));

            // EndTurn để Boss dùng axe-cleave và ghi vào RunKnowledge
            Assert.That(_boot.Session.EndTurn().Success, Is.True);
            Assert.That(_boot.Session.Knowledge.HasSeenBossSkill("axe-cleave"), Is.True);

            // Pause đóng băng timeScale
            _boot.TogglePause();
            Assert.That(_boot.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            _boot.TogglePause();
            Assert.That(_boot.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.GreaterThan(0f));

            // Vào Phase 2 Duel: mặc định Hint OFF và chiêu Boss đầu tiên chưa được giải mã (???)
            _boot.Session.EnterDuel();
            yield return null;
            Assert.That(_boot.Session.State.Duel.IsActive, Is.True);
            Assert.That(_boot.ShowDuelHint, Is.False);
            string bossSkill = _boot.Session.State.Duel.State.BossSkillId;
            Assert.That(_boot.Session.Knowledge.HasSeenBossSkill(bossSkill), Is.False);

            var opts = _boot.Session.State.Duel.GetOptions();
            Assert.That(opts.Any(o => o.SkillId == "protect"), Is.False);
            Assert.That(opts.Any(o => o.SkillId == "blade-guard"), Is.True);

            _boot.ToggleDuelHint();
            Assert.That(_boot.ShowDuelHint, Is.True);

            // Thắng Clash Round 1 -> giải mã chiêu Boss & lưu khắc chế
            var player = _boot.Session.State.Units.Player;
            Assert.That(_boot.Session.TryExecute(ActionRequest.ClashSkill(player.Id, "blade-guard")).Success, Is.True);
            Assert.That(_boot.Session.Knowledge.HasSeenBossSkill(bossSkill), Is.True);
            Assert.That(_boot.Session.Knowledge.HasWonClash(bossSkill, "blade-guard"), Is.True);

            // Mở Sổ tay Chiêu đã học
            _boot.ToggleLearnedSkillsModal();
            Assert.That(_boot.ShowLearnedSkillsModal, Is.True);
            _boot.ToggleLearnedSkillsModal();

            // Sang ải 5 vẫn giữ ký ức
            int knownBefore = _boot.Session.Knowledge.KnownBossSkillCount;
            _boot.LoadStage("stage-5", playIntro: false);
            Assert.That(_boot.Session.Knowledge.KnownBossSkillCount, Is.EqualTo(knownBefore));

            // ResetRun xóa ký ức sạch sẽ
            _boot.ResetRun();
            yield return null;
            Assert.That(_boot.Session.Knowledge.Deaths, Is.Zero);
            Assert.That(_boot.Session.Knowledge.KnownBossSkillCount, Is.Zero);
        }
    }
}
