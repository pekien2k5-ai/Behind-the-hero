using System;
using System.Collections.Generic;
using BTH.Content;
using BTH.Content.Dialogues;
using BTH.Content.Stages;
using BTH.Content.Terrain;
using BTH.Content.Units;
using BTH.Core.Actions;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Turn;
using BTH.Core.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BTH.Game
{
    public enum PlayerCommandMode
    {
        Move,
        Attack,
        Skill1,
        Skill2,
        Skill3
    }

    public enum AppScreenState
    {
        MainMenu,
        Cutscene,
        Playing
    }

    /// <summary>
    /// Composition Root & Presentation Controller cho Behind the Hero.
    /// Quản lý vòng đời màn hình (Main Menu, Lore, Tutorial, Credits, Cutscene hoạt ảnh, Bàn cờ Phase 1, Duel Phase 2, Pause, Cài đặt, Chiêu đã học).
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const float UiW = 1280f;
        private const float UiH = 720f;
        private const float TransitionDuration = 1.35f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            if (FindAnyObjectByType<GameBootstrap>() == null)
            {
                var go = new GameObject("BTH_GameBootstrap");
                go.AddComponent<GameBootstrap>();
            }
        }

        private GameSession _session;
        private IActionValidator _validator;
        private VfxPopupManager _vfxManager;
        private readonly CutsceneController _cutscene = new CutsceneController();

        private Transform _boardRoot;
        private Transform _unitRoot;
        private readonly Dictionary<GridCoord, SpriteRenderer> _tileRenderers = new Dictionary<GridCoord, SpriteRenderer>();
        private readonly Dictionary<GridCoord, SpriteRenderer> _overlayRenderers = new Dictionary<GridCoord, SpriteRenderer>();
        private readonly Dictionary<UnitId, UnitSpriteAnimator> _unitAnimators = new Dictionary<UnitId, UnitSpriteAnimator>();
        private readonly List<IDisposable> _eventSubs = new List<IDisposable>();

        private HlinhBgmPlayer _bgm;
        private AudioSource _sfxSource;

        private AppScreenState _screenState = AppScreenState.MainMenu;
        private bool _hasActiveRun;
        private bool _isPaused;
        private bool _showStageSelectModal;
        private bool _showLearnedSkillsModal;
        private bool _showSettingsModal;
        private bool _showLoreModal;
        private bool _showTutorialModal;
        private bool _showCreditsModal;
        private bool _showDuelHint;
        private Vector2 _codexScroll;
        private Vector2 _loreScroll;
        private Vector2 _tutorialScroll;
        private float _menuAnimTime;

        private string _currentStageId = "stage-1";
        private PlayerCommandMode _cmdMode = PlayerCommandMode.Move;
        private GridCoord _hoverCoord = GridCoord.None;
        private readonly HashSet<GridCoord> _validTargetCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _previewPathCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _aoePreviewCells = new HashSet<GridCoord>();
        private readonly List<string> _combatLog = new List<string>();
        private readonly GameOptionsModel _options = new GameOptionsModel();

        private DialogueRunner _dialogueRunner;
        private bool _outroShownForStage;
        private string _combatBannerText = "Lượt của Kẻ Lang Thang — Chọn ô để Di chuyển hoặc Tấn công.";
        private float _transitionTimer;
        private float _enemyBeatTimer;
        private string _lastDuelRoundNotice = string.Empty;
        private float _duelVfxTimer;
        private string _activeDuelVfxId = "swift-slash";
        private bool _duelVfxOnBoss = true;
        private int _duelFinisherHitsShown;

        private Camera _mainCam;
        private Vector3 _camBasePos = new Vector3(0f, 0f, -10f);
        private float _shakeTimer;
        private float _shakeStrength;

        // Cached GUIStyles (0 allocation trong OnGUI)
        private GUIStyle _panelStyle;
        private GUIStyle _subPanelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _mutedLabelStyle;
        private GUIStyle _boldLabelStyle;
        private GUIStyle _centerBoldStyle;
        private GUIStyle _dialogueTextStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _phase2BannerStyle;
        private GUIStyle _popupStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _activeBtnStyle;
        private GUIStyle _centerBtnStyle;
        private GUIStyle _centerActiveBtnStyle;
        private GUIStyle _badgeWinStyle;
        private GUIStyle _badgeDrawStyle;
        private GUIStyle _badgeLoseStyle;
        private GUIStyle _badgeInfoStyle;
        private GUIStyle _smallBadgeStyle;
        private bool _stylesInitialized;

        private static readonly string[] PlayerActiveSkills = { "swift-slash", "necrotic-burst", "temporal-step" };
        private static readonly string[] StageIds = { "stage-1", "stage-2", "stage-3", "stage-4", "stage-5" };

        public GameSession Session => _session;
        public string CurrentStageId => _currentStageId;
        public DialogueRunner ActiveDialogue => _dialogueRunner;
        public CutsceneController Cutscene => _cutscene;
        public AppScreenState ScreenState => _screenState;
        public bool HasActiveRun => _hasActiveRun;
        public bool IsPaused => _isPaused;
        public bool ShowDuelHint => _showDuelHint;
        public bool ShowLearnedSkillsModal => _showLearnedSkillsModal;
        public bool ShowSettingsModal => _showSettingsModal;
        public bool ShowStageSelectModal => _showStageSelectModal;
        public bool ShowLoreModal => _showLoreModal;
        public bool ShowTutorialModal => _showTutorialModal;
        public bool ShowCreditsModal => _showCreditsModal;
        public IReadOnlyList<string> CombatLog => _combatLog;
        public IReadOnlyCollection<GridCoord> AoePreviewCells => _aoePreviewCells;

        private void Awake()
        {
            _options.LoadFromPrefs();
            _options.ApplyDisplaySettings();
            SetupCameraAndAudio();
            ApplyAudioOptions();
            _vfxManager = gameObject.AddComponent<VfxPopupManager>();
            _session = ContentCatalog.CreateSession();
            EnterMainMenu();
        }

        private void ApplyAudioOptions()
        {
            Time.timeScale = _isPaused ? 0f : Mathf.Clamp(_options.AnimationSpeed, 0.5f, 2.5f);
            _bgm?.SetVolume(_options.MasterVolume * _options.BgmVolume);
            if (_sfxSource != null) _sfxSource.volume = _options.MasterVolume * _options.SfxVolume;
        }

        private void SetupCameraAndAudio()
        {
            _mainCam = Camera.main;
            if (_mainCam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                _mainCam = camGo.AddComponent<Camera>();
            }
            _mainCam.orthographic = true;
            _mainCam.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);

            _bgm = new HlinhBgmPlayer(gameObject);

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.volume = 0.65f;
        }

        private void PlayBgm(string clipName)
        {
            float pitch = clipName == AudioCatalog.StoryTheme ? 1f : AudioCatalog.DuelPitch;
            _bgm.Play(clipName, pitch);
        }

        private void PlaySfx(string clipName)
        {
            var clip = Resources.Load<AudioClip>("Audio/SFX/" + clipName);
            if (clip != null && _sfxSource != null)
            {
                _sfxSource.PlayOneShot(clip);
            }
        }

        private void PushCombatLog(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry)) return;
            _combatLog.Add(entry);
            if (_combatLog.Count > 3)
            {
                _combatLog.RemoveAt(0);
            }
        }

        private void CloseAllModals()
        {
            _showSettingsModal = false;
            _showLearnedSkillsModal = false;
            _showStageSelectModal = false;
            _showLoreModal = false;
            _showTutorialModal = false;
            _showCreditsModal = false;
        }

        public void EnterMainMenu()
        {
            _isPaused = false;
            CloseAllModals();
            _screenState = AppScreenState.MainMenu;
            ApplyAudioOptions();
            SetBoardVisibility(false);
            PlayBgm(AudioCatalog.StoryTheme);
        }

        public void StartNewGame()
        {
            PlaySfx("ui_confirm");
            _isPaused = false;
            CloseAllModals();
            _showDuelHint = false;

            UnsubscribeAll();
            _session = ContentCatalog.CreateSession();
            LoadStage("stage-1", playIntro: true);
        }

        public void ResetRun()
        {
            PlaySfx("transform_dark");
            _isPaused = false;
            CloseAllModals();
            _showDuelHint = false;

            if (_session.State != null)
            {
                _session.Restart();
            }
            LoadStage("stage-1", playIntro: true);
        }

        public void ResumeActiveRunFromMenu()
        {
            if (!_hasActiveRun || _session?.State == null)
            {
                StartNewGame();
                return;
            }
            PlaySfx("ui_confirm");
            _isPaused = false;
            CloseAllModals();
            _screenState = (_dialogueRunner != null && !_dialogueRunner.IsFinished)
                ? AppScreenState.Cutscene
                : AppScreenState.Playing;
            ApplyAudioOptions();
            UpdateBoardVisibility();
            if (_screenState == AppScreenState.Playing && _session.State.Duel.IsActive)
            {
                PlayBgm(_session.State.Stage.Duel?.MusicId ?? AudioCatalog.DuelTheme);
            }
        }

        public void SelectStageFromMenu(string stageId)
        {
            PlaySfx("ui_confirm");
            _isPaused = false;
            CloseAllModals();
            LoadStage(stageId, playIntro: true);
        }

        public void TogglePause()
        {
            if (_screenState == AppScreenState.MainMenu) return;
            _isPaused = !_isPaused;
            if (!_isPaused)
            {
                CloseAllModals();
            }
            ApplyAudioOptions();
            PlaySfx("ui_select");
        }

        public void SetPaused(bool paused)
        {
            if (_screenState == AppScreenState.MainMenu) return;
            _isPaused = paused;
            if (!_isPaused)
            {
                CloseAllModals();
            }
            ApplyAudioOptions();
        }

        public void ToggleDuelHint()
        {
            _showDuelHint = !_showDuelHint;
            PlaySfx("ui_select");
        }

        public void ToggleLearnedSkillsModal()
        {
            bool next = !_showLearnedSkillsModal;
            CloseAllModals();
            _showLearnedSkillsModal = next;
            _codexScroll = Vector2.zero;
            PlaySfx("ui_select");
        }

        public void ToggleLoreModal()
        {
            bool next = !_showLoreModal;
            CloseAllModals();
            _showLoreModal = next;
            _loreScroll = Vector2.zero;
            PlaySfx("ui_select");
        }

        public void ToggleTutorialModal()
        {
            bool next = !_showTutorialModal;
            CloseAllModals();
            _showTutorialModal = next;
            _tutorialScroll = Vector2.zero;
            PlaySfx("ui_select");
        }

        public void ToggleCreditsModal()
        {
            bool next = !_showCreditsModal;
            CloseAllModals();
            _showCreditsModal = next;
            PlaySfx("ui_select");
        }

        public void LoadStage(string stageId, bool playIntro = true)
        {
            _currentStageId = stageId;
            _hasActiveRun = true;
            _isPaused = false;
            _showDuelHint = false;
            CloseAllModals();
            _outroShownForStage = false;
            _transitionTimer = 0f;
            _enemyBeatTimer = 0f;
            _duelFinisherHitsShown = 0;
            _lastDuelRoundNotice = string.Empty;
            _cmdMode = PlayerCommandMode.Move;
            _combatLog.Clear();

            UnsubscribeAll();
            _session.StartStage(stageId);
            _validator = new ActionValidator(_session.State.Pathfinder);

            SubscribeEvents();
            RebuildBoardAndUnits();
            RefreshValidTargets();
            UpdateTileOverlays();
            ApplyAudioOptions();

            var introGraph = playIntro ? DialogueCatalog.GetIntro(stageId) : null;
            if (introGraph != null)
            {
                StartCutscene(introGraph, isOutro: false);
            }
            else
            {
                _dialogueRunner = null;
                _screenState = AppScreenState.Playing;
                UpdateBoardVisibility();
            }

            PlayBgm(AudioCatalog.StoryTheme);
            _combatBannerText = "Bắt đầu " + _session.State.Stage.DisplayName + " — Quan sát ô đỏ Telegraph của Boss!";
            PushCombatLog("► Khởi đầu " + _session.State.Stage.DisplayName);
        }

        private void StartCutscene(DialogueGraph graph, bool isOutro)
        {
            _dialogueRunner = new DialogueRunner(graph, _session.Knowledge);
            _dialogueRunner.Start();
            _cutscene.Begin(_currentStageId, isOutro, _dialogueRunner.Current, PlaySfx, TriggerCameraShake);
            _screenState = AppScreenState.Cutscene;
            SetBoardVisibility(false);
        }

        public void AdvanceOrCompleteCutscene()
        {
            if (_dialogueRunner == null || _dialogueRunner.IsFinished) return;
            if (_dialogueRunner.Current.Choices.Count > 0) return;

            if (!_cutscene.IsLineComplete)
            {
                _cutscene.CompleteCurrentLine(_dialogueRunner.Current);
                PlaySfx("ui_select");
                return;
            }

            _dialogueRunner.Advance();
            PlaySfx("ui_confirm");
            CheckDialogueFinished();
        }

        public void ChooseCutsceneOption(int choiceIndex)
        {
            if (_dialogueRunner == null || _dialogueRunner.IsFinished) return;
            if (choiceIndex < 0 || choiceIndex >= _dialogueRunner.Current.Choices.Count) return;

            _dialogueRunner.Choose(choiceIndex);
            PlaySfx("ui_confirm");
            CheckDialogueFinished();
        }

        public void SkipCutscene()
        {
            if (_dialogueRunner == null || _dialogueRunner.IsFinished) return;
            int safety = 0;
            while (_dialogueRunner != null && !_dialogueRunner.IsFinished && safety++ < 40)
            {
                if (_dialogueRunner.Current.Choices.Count > 0)
                {
                    _cutscene.CompleteCurrentLine(_dialogueRunner.Current);
                    break;
                }
                _dialogueRunner.Advance();
            }
            PlaySfx("ui_confirm");
            CheckDialogueFinished();
        }

        public void DismissAllDialogue()
        {
            SkipCutscene();
        }

        private void SetBoardVisibility(bool visible)
        {
            if (_boardRoot != null) _boardRoot.gameObject.SetActive(visible);
            if (_unitRoot != null) _unitRoot.gameObject.SetActive(visible);
        }

        private void UpdateBoardVisibility()
        {
            bool showBoard = _screenState == AppScreenState.Playing
                             && _session?.State != null
                             && !_session.State.Duel.IsActive;
            SetBoardVisibility(showBoard);
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < _eventSubs.Count; i++)
            {
                _eventSubs[i]?.Dispose();
            }
            _eventSubs.Clear();
        }

        private void SubscribeEvents()
        {
            var ev = _session.Events;
            if (ev == null) return;

            _eventSubs.Add(ev.Subscribe<UnitMovedEvent>((in UnitMovedEvent e) =>
            {
                if (_unitAnimators.TryGetValue(e.Unit, out var anim) && _session.State.Units.TryGet(e.Unit, out var u))
                {
                    anim.MoveToWorld(CoordToWorldForUnit(u));
                    PlaySfx("dash");
                    if (u.Faction == UnitFaction.Player)
                    {
                        PushCombatLog($"• Kẻ Lang Thang tiến tới ({e.To.X},{e.To.Y}).");
                    }
                }
            }));

            _eventSubs.Add(ev.Subscribe<ChestOpenedEvent>((in ChestOpenedEvent e) =>
            {
                Vector3 position = CoordToWorld(e.Coord);
                _vfxManager.SpawnSkillVfx("divine-barrier", position, 1.4f);
                string text = e.HpRestored > 0 ? $"RƯƠNG: +{e.HpRestored} HP!" : "MỞ RƯƠNG!";
                _vfxManager.SpawnPopup(position, text, new Color(0.35f, 1f, 0.55f), 1.5f);
                PlaySfx("checkpoint");
                PushCombatLog($"★ Mở rương tại ({e.Coord.X},{e.Coord.Y}): hồi +{e.HpRestored} HP.");
            }));

            _eventSubs.Add(ev.Subscribe<UnitActedEvent>((in UnitActedEvent e) =>
            {
                if (_unitAnimators.TryGetValue(e.Unit, out var anim))
                {
                    Vector3 targetWorld = e.Target.IsValid ? CoordToWorld(e.Target) : anim.transform.position;
                    anim.TriggerAttack(targetWorld);
                    if (e.Kind == ActionKind.Attack)
                    {
                        _vfxManager.SpawnSkillVfx("swift-slash", targetWorld, 1.25f);
                        PlaySfx("attack1");
                    }
                    else if (e.Kind == ActionKind.Guard)
                    {
                        _vfxManager.SpawnSkillVfx("blade-guard", anim.transform.position, 1.3f);
                        _vfxManager.SpawnPopup(anim.transform.position, "GUARD (-80% DMG)", new Color(0.4f, 0.85f, 1f));
                        PlaySfx("transform_light");
                        PushCombatLog("• Kẻ Lang Thang dựng Guard (-80% sát thương 1 lượt).");
                    }
                }
            }));

            _eventSubs.Add(ev.Subscribe<SkillUsedEvent>((in SkillUsedEvent e) =>
            {
                Vector3 pos = e.Target.IsValid ? CoordToWorld(e.Target) : Vector3.zero;
                if (_unitAnimators.TryGetValue(e.Unit, out var anim))
                {
                    if (!e.Target.IsValid) pos = anim.transform.position;
                    anim.TriggerAttack(pos);
                }
                _vfxManager.SpawnSkillVfx(e.SkillId, pos, 1.55f);
                string actorName = _session.State.Units.TryGet(e.Unit, out var u) ? u.Definition.DisplayName : "Unit";
                if (_session.Skills.TryGet(e.SkillId, out var sk))
                {
                    _combatBannerText = $"{actorName} thi triển [{sk.DisplayName}]!";
                    PushCombatLog($"• {actorName}: [{sk.DisplayName}].");
                }
                PlaySfx(e.Unit == _session.State.Units.Player?.Id ? "attack2" : "boss_attack");
            }));

            _eventSubs.Add(ev.Subscribe<DamageDealtEvent>((in DamageDealtEvent e) =>
            {
                Vector3 pos = e.At.IsValid ? CoordToWorld(e.At) : Vector3.zero;
                if (_unitAnimators.TryGetValue(e.Target, out var targetAnim))
                {
                    pos = targetAnim.transform.position;
                    targetAnim.TriggerHitFlash();
                }
                string tag = e.WasStaggered ? " (STAGGER ×2!)" : (e.WasFlanked ? " (FLANK ×1.5!)" : (e.WasGuarded ? " (GUARD)" : ""));
                bool isPlayerTarget = e.Target == _session.State.Units.Player?.Id;
                Color col = isPlayerTarget ? new Color(1f, 0.32f, 0.32f) : new Color(1f, 0.86f, 0.25f);
                _vfxManager.SpawnPopup(pos, $"-{e.Amount} HP{tag}", col);
                TriggerCameraShake(0.14f, 0.18f);
                if (e.Amount > 0 && !e.Killed)
                {
                    PlaySfx(isPlayerTarget ? "player_hurt" : "enemy_hurt");
                }
                string tName = _session.State.Units.TryGet(e.Target, out var tu) ? tu.Definition.DisplayName : "Mục tiêu";
                PushCombatLog($"  ↳ {tName} nhận -{e.Amount} HP{tag}.");
            }));

            _eventSubs.Add(ev.Subscribe<StatusAppliedEvent>((in StatusAppliedEvent e) =>
            {
                if (_unitAnimators.TryGetValue(e.Unit, out var anim))
                {
                    _vfxManager.SpawnPopup(anim.transform.position + Vector3.up * 0.35f,
                        $"+{e.Status.Value.ToUpperInvariant()} ({e.Duration}T)", new Color(0.75f, 0.52f, 1f));
                }
            }));

            _eventSubs.Add(ev.Subscribe<UnitDiedEvent>((in UnitDiedEvent e) =>
            {
                if (_unitAnimators.TryGetValue(e.Unit, out var anim))
                {
                    anim.TriggerDeath();
                    _vfxManager.SpawnPopup(anim.transform.position, "GỤC NGÃ!", new Color(1f, 0.2f, 0.2f), 1.6f);
                }
                PlaySfx(e.Unit == _session.State.Units.Player?.Id ? "player_death" : "enemy_death");
            }));

            _eventSubs.Add(ev.Subscribe<CellChangedEvent>((in CellChangedEvent e) =>
            {
                RefreshTileVisual(e.Coord);
            }));

            _eventSubs.Add(ev.Subscribe<CellCollapsedEvent>((in CellCollapsedEvent e) =>
            {
                RefreshTileVisual(e.Coord);
                Vector3 w = CoordToWorld(e.Coord);
                _vfxManager.SpawnSkillVfx("earth-crack", w, 1.3f);
                _vfxManager.SpawnPopup(w, "SẬP SÀN -> VỰC THẲM!", new Color(1f, 0.55f, 0.2f));
                TriggerCameraShake(0.2f, 0.25f);
                PushCombatLog($"! Ô ({e.Coord.X},{e.Coord.Y}) sập thành Vực thẳm.");
            }));

            _eventSubs.Add(ev.Subscribe<HazardTriggeredEvent>((in HazardTriggeredEvent e) =>
            {
                Vector3 w = CoordToWorld(e.Coord);
                _vfxManager.SpawnPopup(w, $"BẪY ĐỊA HÌNH -{e.Damage} HP", new Color(0.45f, 1f, 0.45f));
                PushCombatLog($"! Bẫy địa hình tại ({e.Coord.X},{e.Coord.Y}) gây -{e.Damage} HP.");
            }));

            _eventSubs.Add(ev.Subscribe<BossPhaseChangedEvent>((in BossPhaseChangedEvent e) =>
            {
                if (e.Phase == 2)
                {
                    _showDuelHint = false;
                    _enemyBeatTimer = 0f;
                    _duelFinisherHitsShown = 0;
                    _transitionTimer = TransitionDuration;
                    TriggerCameraShake(0.38f, 0.65f);
                    PlaySfx("boss_roar");
                    PlaySfx("transform_dark");
                    PlayBgm(_session.State.Stage.Duel?.MusicId ?? AudioCatalog.DuelTheme);
                    _combatBannerText = "BOSS DƯỚI 70% HP — CHUYỂN CẢNH PHASE 2: DUEL MODE!";
                    PushCombatLog("★ Boss xuống dưới 70% HP → Chuyển Phase 2: Duel Mode!");
                }
            }));

            _eventSubs.Add(ev.Subscribe<ClashResolvedEvent>((in ClashResolvedEvent e) =>
            {
                _activeDuelVfxId = e.PlayerSkillId;
                _duelVfxOnBoss = e.Result != ClashResult.Lose;
                _duelVfxTimer = 0.70f;
                _duelFinisherHitsShown = e.Result == ClashResult.Win
                    ? Math.Max(1, _session.State.Duel?.Definition?.FinisherCount ?? 1)
                    : 0;

                string outcomeVi = e.Result == ClashResult.Win
                    ? $"THẮNG CLASH ({e.PlayerPower} vs {e.BossPower}) -> {_duelFinisherHitsShown} ĐÒN KẾT LIỄU! (Đã lưu Sổ tay)"
                    : (e.Result == ClashResult.Draw
                        ? $"HÒA CLASH ({e.PlayerPower} vs {e.BossPower}) -> MỖI BÊN MẤT 10% UY LỰC!"
                        : $"THUA CLASH ({e.PlayerPower} vs {e.BossPower}) -> NHẬN ĐÒN TUYỆT KỸ BOSS!");
                _lastDuelRoundNotice = outcomeVi;
                _combatBannerText = outcomeVi;
                PlaySfx(e.Result == ClashResult.Win ? "attack3" : "boss_attack");
            }));

            _eventSubs.Add(ev.Subscribe<RewindPerformedEvent>((in RewindPerformedEvent e) =>
            {
                _transitionTimer = 0f;
                _enemyBeatTimer = 0f;
                _duelVfxTimer = 0f;
                _duelFinisherHitsShown = 0;
                _lastDuelRoundNotice = string.Empty;
                _showDuelHint = false;
                RebuildBoardAndUnits();
                RefreshValidTargets();
                UpdateBoardVisibility();

                var player = _session.State.Units.Player;
                if (player != null)
                {
                    _vfxManager.SpawnSkillVfx("rewind", CoordToWorldForUnit(player), 1.8f);
                }

                bool inDuel = _session.State.Turn.Phase == TurnPhase.Duel;
                PlayBgm(inDuel
                    ? (_session.State.Stage.Duel?.MusicId ?? AudioCatalog.DuelTheme)
                    : AudioCatalog.StoryTheme);
                PlaySfx("transform_dark");
                string checkpoint = inDuel ? "ĐẦU DUEL" : $"LƯỢT {e.TurnIndex}";
                _combatBannerText = $"ĐÃ TUA NGƯỢC THỜI GIAN VỀ {checkpoint}! Ký ức chiêu Boss vẫn được giữ.";
                PushCombatLog($"↺ Rewind về {checkpoint} (Ký ức chiêu được bảo toàn).");
            }));

            _eventSubs.Add(ev.Subscribe<CameraShakeHintEvent>((in CameraShakeHintEvent e) =>
            {
                TriggerCameraShake(e.Strength * 0.4f, e.Duration);
            }));

            _eventSubs.Add(ev.Subscribe<StageEndedEvent>((in StageEndedEvent e) =>
            {
                if (e.Result == StageResult.Victory && !_outroShownForStage)
                {
                    _outroShownForStage = true;
                    var outro = DialogueCatalog.GetOutro(_currentStageId);
                    if (outro != null)
                    {
                        StartCutscene(outro, isOutro: true);
                        PlayBgm(AudioCatalog.StoryTheme);
                    }
                }
            }));
        }

        private void TriggerCameraShake(float strength, float duration)
        {
            _shakeStrength = Mathf.Max(_shakeStrength, strength);
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
        }

        private void RebuildBoardAndUnits()
        {
            if (_boardRoot != null) Destroy(_boardRoot.gameObject);
            if (_unitRoot != null) Destroy(_unitRoot.gameObject);

            _tileRenderers.Clear();
            _overlayRenderers.Clear();
            _unitAnimators.Clear();

            var state = _session.State;
            if (state == null) return;

            _boardRoot = new GameObject("BoardRoot").transform;
            _unitRoot = new GameObject("UnitRoot").transform;

            int w = state.Grid.Width;
            int h = state.Grid.Height;

            float boardCenterX = (w - 1) * 0.5f;
            float boardCenterY = (h - 1) * 0.5f;
            _camBasePos = new Vector3(boardCenterX - 1.35f, boardCenterY + 0.15f, -10f);
            _mainCam.transform.position = _camBasePos;
            _mainCam.orthographicSize = Mathf.Max(5.2f, h * 0.60f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var coord = new GridCoord(x, y);
                    var tileGo = new GameObject($"Tile_{x}_{y}");
                    tileGo.transform.SetParent(_boardRoot, false);
                    tileGo.transform.position = CoordToWorld(coord);

                    var sr = tileGo.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 0;
                    _tileRenderers[coord] = sr;
                    RefreshTileVisual(coord);

                    var ovGo = new GameObject("Overlay");
                    ovGo.transform.SetParent(tileGo.transform, false);
                    ovGo.transform.localPosition = new Vector3(0f, 0f, -0.05f);
                    ovGo.transform.localScale = Vector3.one * 0.96f;
                    var ovSr = ovGo.AddComponent<SpriteRenderer>();
                    ovSr.sprite = ArtCatalog.BorderedTileSprite;
                    ovSr.sortingOrder = 5;
                    ovSr.color = new Color(0.22f, 0.45f, 0.78f, 0.22f);
                    _overlayRenderers[coord] = ovSr;
                }
            }

            foreach (var u in state.Units.All)
            {
                SpawnUnitView(u);
            }
        }

        private void SpawnUnitView(UnitState unit)
        {
            var go = new GameObject($"Unit_{unit.Definition.Id}_{unit.Id.Value}");
            go.transform.SetParent(_unitRoot, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20 + (12 - unit.Position.Y);

            var anim = go.AddComponent<UnitSpriteAnimator>();
            float scaleMult = unit.SizeX > 1 ? 1.85f : 1.05f;
            bool flipX = unit.Faction == UnitFaction.Enemy;
            anim.Initialize(unit.Definition.Id, sr, CoordToWorldForUnit(unit), Vector3.one * scaleMult, flipX);
            if (!unit.IsAlive)
            {
                anim.TriggerDeath();
            }
            _unitAnimators[unit.Id] = anim;
        }

        private void RefreshTileVisual(GridCoord coord)
        {
            if (!_tileRenderers.TryGetValue(coord, out var sr) || sr == null) return;
            var cell = _session.State.Grid.Get(coord);
            sr.sprite = ArtCatalog.GetTerrainSprite(cell.Terrain, _currentStageId, coord.X, coord.Y);
            sr.color = cell.IsLocked ? new Color(0.5f, 0.25f, 0.25f, 1f) : Color.white;
        }

        private static Vector3 CoordToWorld(GridCoord c)
        {
            return new Vector3(c.X, c.Y, 0f);
        }

        private static Vector3 CoordToWorldForUnit(UnitState u)
        {
            float cx = u.Position.X + (u.SizeX - 1) * 0.5f;
            float cy = u.Position.Y + (u.SizeY - 1) * 0.5f;
            return new Vector3(cx, cy - 0.15f, -0.2f);
        }

        private void RefreshValidTargets()
        {
            _validTargetCells.Clear();
            _previewPathCells.Clear();
            _aoePreviewCells.Clear();
            var state = _session?.State;
            var player = state?.Units?.Player;
            if (state == null || player == null || !player.IsAlive || state.Turn.Phase != TurnPhase.Player)
            {
                return;
            }

            ActionRequest req;
            switch (_cmdMode)
            {
                case PlayerCommandMode.Move:
                    req = ActionRequest.Move(player.Id, player.Position);
                    break;
                case PlayerCommandMode.Attack:
                    req = ActionRequest.Attack(player.Id, player.Position);
                    break;
                case PlayerCommandMode.Skill1:
                    req = ActionRequest.Skill(player.Id, PlayerActiveSkills[0], player.Position);
                    break;
                case PlayerCommandMode.Skill2:
                    req = ActionRequest.Skill(player.Id, PlayerActiveSkills[1], player.Position);
                    break;
                case PlayerCommandMode.Skill3:
                    req = ActionRequest.Skill(player.Id, PlayerActiveSkills[2], player.Position);
                    break;
                default:
                    req = ActionRequest.Move(player.Id, player.Position);
                    break;
            }

            var list = _validator.GetValidTargets(req, state);
            for (int i = 0; i < list.Count; i++)
            {
                _validTargetCells.Add(list[i]);
            }
        }

        private void RefreshHoverPreviews()
        {
            _previewPathCells.Clear();
            _aoePreviewCells.Clear();
            var state = _session?.State;
            if (state == null || !_hoverCoord.IsValid || !state.Grid.InBounds(_hoverCoord))
            {
                return;
            }

            if (_cmdMode == PlayerCommandMode.Move && _validTargetCells.Contains(_hoverCoord))
            {
                var path = TacticalNavigator.PreviewPlayerPath(state, _hoverCoord);
                for (int i = 0; i < path.Count; i++)
                {
                    _previewPathCells.Add(path[i]);
                }
            }
            else if (_cmdMode == PlayerCommandMode.Skill2 && _validTargetCells.Contains(_hoverCoord))
            {
                var cross = GridShapes.Cross(_hoverCoord, 1);
                for (int i = 0; i < cross.Count; i++)
                {
                    if (state.Grid.InBounds(cross[i]))
                    {
                        _aoePreviewCells.Add(cross[i]);
                    }
                }
            }
        }

        private void Update()
        {
            float udt = Time.unscaledDeltaTime;
            _bgm?.Tick(udt);
            _menuAnimTime += udt;

            HandleGlobalEscapeShortcut();

            if (_screenState == AppScreenState.MainMenu)
            {
                SetBoardVisibility(false);
                return;
            }

            if (_session?.State == null) return;

            if (_shakeTimer > 0f && !_isPaused)
            {
                _shakeTimer -= udt;
                Vector2 offset = UnityEngine.Random.insideUnitCircle * _shakeStrength;
                _mainCam.transform.position = _camBasePos + new Vector3(offset.x, offset.y, 0f);
                if (_shakeTimer <= 0f)
                {
                    _mainCam.transform.position = _camBasePos;
                    _shakeStrength = 0f;
                }
            }

            if (_screenState == AppScreenState.Cutscene)
            {
                SetBoardVisibility(false);
                if (!_isPaused && _dialogueRunner != null && !_dialogueRunner.IsFinished)
                {
                    _cutscene.Tick(udt, _dialogueRunner.Current, PlaySfx, TriggerCameraShake);
                    HandleCutsceneShortcuts();
                }
                return;
            }

            if (_isPaused)
            {
                return;
            }

            if (_transitionTimer > 0f)
            {
                _transitionTimer = Mathf.Max(0f, _transitionTimer - udt);
            }

            if (_enemyBeatTimer > 0f)
            {
                _enemyBeatTimer = Mathf.Max(0f, _enemyBeatTimer - udt);
            }

            if (_duelVfxTimer > 0f)
            {
                _duelVfxTimer = Mathf.Max(0f, _duelVfxTimer - Time.deltaTime);
            }

            UpdateBoardVisibility();

            if (_transitionTimer > 0f) return;

            bool modalOpen = _showSettingsModal || _showLearnedSkillsModal || _showLoreModal ||
                             _showTutorialModal || _showCreditsModal || _session.State.IsOver;
            HandleGameplayShortcuts(modalOpen);

            bool inDuel = _session.State.Duel.IsActive;
            if (!inDuel)
            {
                if (!modalOpen)
                {
                    UpdateBoardHoverAndClick();
                }
                else
                {
                    _hoverCoord = GridCoord.None;
                    _previewPathCells.Clear();
                    _aoePreviewCells.Clear();
                }
                UpdateTileOverlays();
            }
        }

        private void HandleGlobalEscapeShortcut()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

            if (_showSettingsModal)
            {
                _options.SaveToPrefs();
                _showSettingsModal = false;
                PlaySfx("ui_select");
                return;
            }

            if (_showLearnedSkillsModal || _showStageSelectModal || _showLoreModal || _showTutorialModal || _showCreditsModal)
            {
                CloseAllModals();
                PlaySfx("ui_select");
                return;
            }

            if (_screenState != AppScreenState.MainMenu)
            {
                TogglePause();
            }
        }

        private void HandleCutsceneShortcuts()
        {
            var kb = Keyboard.current;
            if (kb == null || _showSettingsModal || _showLearnedSkillsModal) return;

            if (_dialogueRunner != null && !_dialogueRunner.IsFinished)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                {
                    AdvanceOrCompleteCutscene();
                }
            }
        }

        private void HandleGameplayShortcuts(bool modalOpen)
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.kKey.wasPressedThisFrame && !_showSettingsModal)
            {
                ToggleLearnedSkillsModal();
                return;
            }

            if (modalOpen) return;

            if (_session.State.Turn.Phase == TurnPhase.Duel)
            {
                if (kb.hKey.wasPressedThisFrame) ToggleDuelHint();
                if (kb.rKey.wasPressedThisFrame) ExecuteRewind();
                if (kb.digit1Key.wasPressedThisFrame) TryExecuteDuelOptionByIndex(0);
                if (kb.digit2Key.wasPressedThisFrame) TryExecuteDuelOptionByIndex(1);
                if (kb.digit3Key.wasPressedThisFrame) TryExecuteDuelOptionByIndex(2);
                return;
            }

            if (kb.gKey.wasPressedThisFrame) SetCommandMode(PlayerCommandMode.Move);
            if (kb.sKey.wasPressedThisFrame) SetCommandMode(PlayerCommandMode.Attack);
            if (kb.eKey.wasPressedThisFrame) SetCommandMode(PlayerCommandMode.Skill3);
            if (kb.digit1Key.wasPressedThisFrame) SetCommandMode(PlayerCommandMode.Skill1);
            if (kb.digit2Key.wasPressedThisFrame) SetCommandMode(PlayerCommandMode.Skill2);
            if (kb.digit3Key.wasPressedThisFrame) SetCommandMode(PlayerCommandMode.Skill3);
            if (kb.dKey.wasPressedThisFrame) ExecuteGuard();
            if (kb.rKey.wasPressedThisFrame) ExecuteRewind();
            if (kb.xKey.wasPressedThisFrame) LoadStage(_currentStageId, playIntro: false);
            if (kb.spaceKey.wasPressedThisFrame) ExecuteEndTurn();
        }

        private void TryExecuteDuelOptionByIndex(int index)
        {
            var duel = _session?.State?.Duel;
            var player = _session?.State?.Units?.Player;
            if (duel == null || !duel.IsActive || player == null) return;

            var options = duel.GetOptions();
            if (index >= 0 && index < options.Count)
            {
                _session.TryExecute(ActionRequest.ClashSkill(player.Id, options[index].SkillId));
            }
        }

        private void SetCommandMode(PlayerCommandMode mode)
        {
            _cmdMode = mode;
            RefreshValidTargets();
            RefreshHoverPreviews();
            PlaySfx("ui_select");
        }

        private void UpdateBoardHoverAndClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || _mainCam == null) return;

            Vector2 mpos = mouse.position.ReadValue();
            float scaleX = Screen.width / UiW;
            float scaleY = Screen.height / UiH;
            if (mpos.x < 285f * scaleX || mpos.y > Screen.height - 95f * scaleY)
            {
                _hoverCoord = GridCoord.None;
                _previewPathCells.Clear();
                _aoePreviewCells.Clear();
                return;
            }

            Vector3 world = _mainCam.ScreenToWorldPoint(new Vector3(mpos.x, mpos.y, 10f));
            int gx = Mathf.RoundToInt(world.x);
            int gy = Mathf.RoundToInt(world.y);
            var coord = new GridCoord(gx, gy);

            if (_session.State.Grid.InBounds(coord))
            {
                if (_hoverCoord != coord)
                {
                    _hoverCoord = coord;
                    RefreshHoverPreviews();
                }
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    OnCellClicked(coord);
                }
            }
            else
            {
                _hoverCoord = GridCoord.None;
                _previewPathCells.Clear();
                _aoePreviewCells.Clear();
            }
        }

        private void OnCellClicked(GridCoord coord)
        {
            var state = _session.State;
            var player = state.Units.Player;
            if (player == null || !player.IsAlive || state.Turn.Phase != TurnPhase.Player) return;

            var unitAtCell = state.Units.At(coord);
            if (_cmdMode == PlayerCommandMode.Move && unitAtCell != null && unitAtCell.Faction == UnitFaction.Enemy)
            {
                var atkReq = ActionRequest.Attack(player.Id, coord);
                if (_validator.Validate(atkReq, state).IsValid)
                {
                    _cmdMode = PlayerCommandMode.Attack;
                }
            }

            ActionRequest req;
            switch (_cmdMode)
            {
                case PlayerCommandMode.Move:
                    req = ActionRequest.Move(player.Id, coord);
                    break;
                case PlayerCommandMode.Attack:
                    req = ActionRequest.Attack(player.Id, coord);
                    break;
                case PlayerCommandMode.Skill1:
                    req = ActionRequest.Skill(player.Id, PlayerActiveSkills[0], coord);
                    break;
                case PlayerCommandMode.Skill2:
                    req = ActionRequest.Skill(player.Id, PlayerActiveSkills[1], coord);
                    break;
                case PlayerCommandMode.Skill3:
                    req = ActionRequest.Skill(player.Id, PlayerActiveSkills[2], coord);
                    break;
                default:
                    req = ActionRequest.Move(player.Id, coord);
                    break;
            }

            var val = _validator.Validate(req, state);
            if (!val.IsValid)
            {
                _combatBannerText = $"Không thể thực hiện tại ({coord.X},{coord.Y}): {val.Message} ({val.Error})";
                PlaySfx("ui_select");
                return;
            }

            var res = _session.TryExecute(req);
            if (res.Success)
            {
                if (_cmdMode == PlayerCommandMode.Move && player.HasMovedThisTurn && !player.HasActedThisTurn)
                {
                    _cmdMode = PlayerCommandMode.Skill1;
                }
                RefreshValidTargets();
                RefreshHoverPreviews();
            }
            else
            {
                _combatBannerText = $"Lệnh thất bại: {res.Message}";
            }
        }

        private void ExecuteGuard()
        {
            var player = _session.State.Units.Player;
            if (player == null || _session.State.Turn.Phase != TurnPhase.Player) return;
            var res = _session.TryExecute(ActionRequest.Guard(player.Id));
            if (res.Success)
            {
                _combatBannerText = "Kẻ Lang Thang dựng Hộ Kiếm (Guard: giảm 80% sát thương nhận vào trong 1 lượt)!";
                RefreshValidTargets();
                RefreshHoverPreviews();
            }
            else
            {
                _combatBannerText = $"Không thể Guard: {res.Message}";
            }
        }

        private void ExecuteRewind()
        {
            if (_session?.State?.Snapshots != null && _session.State.Snapshots.CanRewind)
            {
                _session.State.Snapshots.Rewind();
            }
            else
            {
                _combatBannerText = "Chưa có checkpoint để tua ngược!";
            }
        }

        private void ExecuteEndTurn()
        {
            if (_session.State.Turn.Phase != TurnPhase.Player) return;
            PlaySfx("ui_confirm");
            _enemyBeatTimer = 0.55f;
            _session.EndTurn();
            _cmdMode = PlayerCommandMode.Move;
            RefreshValidTargets();
            RefreshHoverPreviews();
        }

        private void UpdateTileOverlays()
        {
            var state = _session.State;
            bool shiftHeld = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            bool showDanger = _options.ShowDangerZoneWarning || shiftHeld;
            var dangerSet = showDanger
                ? new HashSet<GridCoord>(state.Telegraph.DangerCells)
                : new HashSet<GridCoord>();
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 5.5f);

            foreach (var kv in _overlayRenderers)
            {
                var coord = kv.Key;
                var sr = kv.Value;
                if (sr == null) continue;

                bool isHover = coord == _hoverCoord;
                bool isAoe = _aoePreviewCells.Contains(coord);
                bool isPath = _previewPathCells.Contains(coord);
                bool isValid = _validTargetCells.Contains(coord);
                bool isDanger = dangerSet.Contains(coord);

                if (isAoe)
                {
                    sr.color = isHover
                        ? new Color(1f, 0.88f, 0.35f, 0.95f)
                        : new Color(0.86f, 0.26f, 1.0f, 0.88f * pulse);
                }
                else if (isHover)
                {
                    sr.color = new Color(1f, 0.88f, 0.32f, 0.92f);
                }
                else if (isPath && _cmdMode == PlayerCommandMode.Move)
                {
                    sr.color = new Color(0.32f, 1f, 0.78f, 0.88f);
                }
                else if (isValid && _cmdMode == PlayerCommandMode.Move)
                {
                    sr.color = isDanger
                        ? new Color(1f, 0.55f, 0.22f, 0.82f)
                        : new Color(0.25f, 0.75f, 1f, 0.78f);
                }
                else if (isValid)
                {
                    sr.color = new Color(0.78f, 0.38f, 1f, 0.82f);
                }
                else if (isDanger)
                {
                    sr.color = new Color(1f, 0.18f, 0.20f, 0.75f * pulse);
                }
                else
                {
                    sr.color = _options.ShowGridOverlay
                        ? new Color(0.22f, 0.45f, 0.78f, 0.22f)
                        : new Color(0f, 0f, 0f, 0f);
                }
            }
        }

        private void CheckDialogueFinished()
        {
            if (_dialogueRunner != null && _dialogueRunner.IsFinished)
            {
                if (_dialogueRunner.Ending != StageResult.None)
                {
                    _session.State.Turn.FinishStage(_dialogueRunner.Ending);
                }
                _dialogueRunner = null;
                _screenState = AppScreenState.Playing;
                UpdateBoardVisibility();
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized && _panelStyle?.normal.background != null) return;
            _stylesInitialized = true;

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = MakeStyleTex(new Color(0.08f, 0.09f, 0.13f, 0.95f));
            _panelStyle.normal.textColor = new Color(0.92f, 0.89f, 0.82f);
            _panelStyle.padding = new RectOffset(10, 10, 10, 10);

            _subPanelStyle = new GUIStyle(GUI.skin.box);
            _subPanelStyle.normal.background = MakeStyleTex(new Color(0.12f, 0.14f, 0.21f, 0.96f));
            _subPanelStyle.normal.textColor = new Color(0.92f, 0.89f, 0.82f);
            _subPanelStyle.padding = new RectOffset(8, 8, 8, 8);

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _headerStyle.normal.textColor = new Color(0.90f, 0.76f, 0.35f);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };
            _labelStyle.normal.textColor = new Color(0.86f, 0.88f, 0.92f);

            _mutedLabelStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic
            };
            _mutedLabelStyle.normal.textColor = new Color(0.68f, 0.72f, 0.80f);

            _boldLabelStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            _centerBoldStyle = new GUIStyle(_boldLabelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            _dialogueTextStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 15,
                wordWrap = true
            };
            _dialogueTextStyle.normal.textColor = new Color(0.95f, 0.94f, 0.90f);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 46,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = new Color(0.94f, 0.80f, 0.38f);

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter
            };
            _subtitleStyle.normal.textColor = new Color(0.82f, 0.85f, 0.94f);

            _phase2BannerStyle = new GUIStyle(_headerStyle)
            {
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter
            };
            _phase2BannerStyle.normal.textColor = new Color(1f, 0.88f, 0.42f, 1f);

            _popupStyle = new GUIStyle(_boldLabelStyle)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 6, 6)
            };
            _btnStyle.normal.background = MakeStyleTex(new Color(0.16f, 0.21f, 0.30f, 0.96f));
            _btnStyle.hover.background = MakeStyleTex(new Color(0.24f, 0.32f, 0.46f, 0.98f));
            _btnStyle.normal.textColor = Color.white;
            _btnStyle.hover.textColor = new Color(0.90f, 0.76f, 0.35f);

            _activeBtnStyle = new GUIStyle(_btnStyle);
            _activeBtnStyle.normal.background = MakeStyleTex(new Color(0.42f, 0.23f, 0.65f, 0.98f));
            _activeBtnStyle.hover.background = MakeStyleTex(new Color(0.50f, 0.29f, 0.75f, 0.98f));
            _activeBtnStyle.normal.textColor = new Color(1f, 0.92f, 0.55f);

            _centerBtnStyle = new GUIStyle(_btnStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
            _centerActiveBtnStyle = new GUIStyle(_activeBtnStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };

            _badgeWinStyle = MakeBadgeStyle(new Color(0.14f, 0.56f, 0.26f, 0.96f));
            _badgeDrawStyle = MakeBadgeStyle(new Color(0.68f, 0.52f, 0.12f, 0.96f));
            _badgeLoseStyle = MakeBadgeStyle(new Color(0.65f, 0.15f, 0.15f, 0.96f));
            _badgeInfoStyle = MakeBadgeStyle(new Color(0.18f, 0.28f, 0.48f, 0.96f));
            _smallBadgeStyle = new GUIStyle(_badgeDrawStyle) { fontSize = 11 };
        }

        private static GUIStyle MakeBadgeStyle(Color bg)
        {
            var st = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            st.normal.background = MakeStyleTex(bg);
            st.normal.textColor = Color.white;
            return st;
        }

        private static Texture2D MakeStyleTex(Color col)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            t.SetPixels(new[] { col, col, col, col });
            t.Apply();
            return t;
        }

        private static void DrawRect(Rect rect, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(rect, ArtCatalog.WhiteTex);
            GUI.color = prev;
        }

        private void OnGUI()
        {
            InitStyles();

            var prevMatrix = GUI.matrix;
            bool wasEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / UiW, Screen.height / UiH, 1f));

            if (_screenState == AppScreenState.MainMenu)
            {
                bool menuSubModal = _showStageSelectModal || _showLearnedSkillsModal || _showSettingsModal ||
                                    _showLoreModal || _showTutorialModal || _showCreditsModal;
                GUI.enabled = wasEnabled && !menuSubModal;
                DrawMainMenuScreen();
                GUI.enabled = wasEnabled;

                DrawActiveUtilityModals();

                GUI.matrix = prevMatrix;
                return;
            }

            if (_session?.State == null)
            {
                GUI.matrix = prevMatrix;
                return;
            }

            bool isTransitioning = _transitionTimer > 0f;
            bool hasOverlayModal = _isPaused || _showSettingsModal || _showLearnedSkillsModal ||
                                   _showLoreModal || _showTutorialModal || _showCreditsModal;
            GUI.enabled = wasEnabled && !isTransitioning && !hasOverlayModal;

            if (_screenState == AppScreenState.Cutscene && _dialogueRunner != null && !_dialogueRunner.IsFinished)
            {
                _cutscene.Draw(
                    _dialogueRunner,
                    _panelStyle,
                    _headerStyle,
                    _dialogueTextStyle,
                    _mutedLabelStyle,
                    _centerBtnStyle,
                    _centerActiveBtnStyle,
                    _centerBoldStyle,
                    DrawRect,
                    DrawSpriteOnGui,
                    AdvanceOrCompleteCutscene,
                    ChooseCutsceneOption,
                    SkipCutscene,
                    TogglePause);
            }
            else
            {
                if (_session.State.Duel.IsActive)
                {
                    DrawDuelScreen();
                }
                else
                {
                    DrawBoardWorldLabels();
                    DrawLeftHudPanel();
                    DrawCombatLogStrip();
                }

                DrawTopBar();

                if (_session.State.IsOver)
                {
                    DrawStageResultModal();
                }
            }

            GUI.enabled = wasEnabled;
            if (_isPaused && !(_showSettingsModal || _showLearnedSkillsModal || _showLoreModal || _showTutorialModal || _showCreditsModal))
            {
                DrawPauseModal();
            }

            DrawActiveUtilityModals();

            if (isTransitioning)
            {
                DrawPhase2TransitionOverlay();
            }

            GUI.matrix = prevMatrix;
        }

        private void DrawActiveUtilityModals()
        {
            if (_showStageSelectModal) DrawStageSelectModal();
            if (_showLearnedSkillsModal) DrawLearnedSkillsModal();
            if (_showLoreModal) DrawLoreModal();
            if (_showTutorialModal) DrawTutorialModal();
            if (_showCreditsModal) DrawCreditsModal();
            if (_showSettingsModal) DrawSettingsModal();
        }

        private void DrawMainMenuScreen()
        {
            var bgTex = ArtCatalog.GetBackground("bg_mausoleum");
            float drift = Mathf.Sin(_menuAnimTime * 0.25f) * 12f;
            GUI.DrawTexture(new Rect(-16f + drift, -8f, UiW + 32f, UiH + 16f), bgTex, ScaleMode.StretchToFill);
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0.03f, 0.03f, 0.07f, 0.62f));

            for (int i = 0; i < 16; i++)
            {
                float px = ((i * 89f + _menuAnimTime * (15f + (i % 4) * 6f)) % (UiW + 30f)) - 15f;
                float py = UiH - ((i * 53f + _menuAnimTime * (20f + (i % 3) * 9f)) % UiH);
                Color c = (i % 2 == 0)
                    ? new Color(0.90f, 0.76f, 0.35f, 0.35f)
                    : new Color(0.55f, 0.35f, 0.85f, 0.38f);
                DrawRect(new Rect(px, py, 4, 4), c);
            }

            int animFrame = Mathf.FloorToInt(_menuAnimTime * 5.5f) % 4;
            var wFrames = ArtCatalog.GetUnitAnimFrames(UnitCatalog.WandererId, UnitAnimState.Idle);
            var eFrames = ArtCatalog.GetUnitAnimFrames(UnitCatalog.EosphorosId, UnitAnimState.Idle);
            float bobL = Mathf.Sin(_menuAnimTime * 2.2f) * 4f;
            float bobR = Mathf.Sin(_menuAnimTime * 2.2f + 1.2f) * 4f;

            DrawSpriteOnGui(new Rect(85, 290 + bobL, 260, 260), wFrames[animFrame % wFrames.Length], flipX: false);
            DrawSpriteOnGui(new Rect(UiW - 345, 290 + bobR, 260, 260), eFrames[animFrame % eFrames.Length], flipX: true);

            DrawRect(new Rect(280, 40, 720, 4), new Color(0.90f, 0.76f, 0.35f, 0.85f));
            GUI.Label(new Rect(180, 52, 920, 62), "BEHIND THE HERO", _titleStyle);
            GUI.Label(new Rect(220, 114, 840, 28),
                "Kẻ Lang Thang & Lời Nguyền Thời Gian — Chiến Thuật Giải Đố 1v1", _subtitleStyle);
            DrawRect(new Rect(380, 148, 520, 2), new Color(0.55f, 0.36f, 0.84f, 0.75f));

            float cardH = _hasActiveRun ? 458f : 408f;
            Rect menuBox = new Rect(UiW * 0.5f - 220f, 174f, 440f, cardH);
            GUI.Box(menuBox, GUIContent.none, _panelStyle);
            DrawRect(new Rect(menuBox.x, menuBox.y, menuBox.width, 3), new Color(0.90f, 0.76f, 0.35f, 0.9f));

            float y = menuBox.y + 18f;
            float bw = menuBox.width - 44f;
            float bx = menuBox.x + 22f;

            if (_hasActiveRun)
            {
                if (GUI.Button(new Rect(bx, y, bw, 42), "▶  TIẾP TỤC HÀNH TRÌNH (CONTINUE)", _centerActiveBtnStyle))
                {
                    ResumeActiveRunFromMenu();
                }
                y += 50f;
            }

            if (GUI.Button(new Rect(bx, y, bw, 42),
                _hasActiveRun ? "⚔  BẮT ĐẦU MỚI TỪ ĐẦU (NEW GAME)" : "▶  BẮT ĐẦU GAME (NEW GAME)",
                _hasActiveRun ? _centerBtnStyle : _centerActiveBtnStyle))
            {
                StartNewGame();
            }
            y += 50f;

            if (GUI.Button(new Rect(bx, y, bw, 38), "🗺  CHỌN ẢI CHIẾN ĐẤU (ẢI 1 – 5)", _centerBtnStyle))
            {
                CloseAllModals();
                _showStageSelectModal = true;
                PlaySfx("ui_select");
            }
            y += 46f;

            int knownCount = _session?.Knowledge?.KnownBossSkillCount ?? 0;
            if (GUI.Button(new Rect(bx, y, bw, 38), $"📖  CHIÊU ĐÃ HỌC ({knownCount})", _centerBtnStyle))
            {
                ToggleLearnedSkillsModal();
            }
            y += 46f;

            float halfW = (bw - 10f) * 0.5f;
            if (GUI.Button(new Rect(bx, y, halfW, 38), "📜 CỐT TRUYỆN 3 HỒI", _centerBtnStyle))
            {
                ToggleLoreModal();
            }
            if (GUI.Button(new Rect(bx + halfW + 10f, y, halfW, 38), "❓ HƯỚNG DẪN CHƠI", _centerBtnStyle))
            {
                ToggleTutorialModal();
            }
            y += 46f;

            if (GUI.Button(new Rect(bx, y, bw, 38), "⚙  CÀI ĐẶT (SETTINGS)", _centerBtnStyle))
            {
                CloseAllModals();
                _showSettingsModal = true;
                PlaySfx("ui_select");
            }
            y += 46f;

            if (GUI.Button(new Rect(bx, y, halfW, 36), "★ CREDITS", _centerBtnStyle))
            {
                ToggleCreditsModal();
            }
            if (GUI.Button(new Rect(bx + halfW + 10f, y, halfW, 36), "✕ THOÁT GAME", _centerBtnStyle))
            {
                QuitGame();
            }

            GUI.Label(new Rect(0, UiH - 36, UiW, 24),
                "Phase 1: Giải đố ô lưới & Né Telegraph  ·  Phase 2: Đấu Kỹ Clash 1v1  ·  [ESC] Tạm dừng",
                _subtitleStyle);
        }

        private void DrawStageSelectModal()
        {
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0f, 0f, 0f, 0.65f));
            Rect modal = new Rect(UiW * 0.5f - 360f, 80f, 720f, 540f);
            GUI.Box(modal, GUIContent.none, _panelStyle);
            DrawRect(new Rect(modal.x, modal.y, modal.width, 3), new Color(0.90f, 0.76f, 0.35f, 0.9f));

            GUI.Label(new Rect(modal.x + 20, modal.y + 14, 520, 28), "🗺 CHỌN ẢI CHIẾN ĐẤU (5 ẢI)", _headerStyle);
            if (GUI.Button(new Rect(modal.xMax - 92, modal.y + 12, 74, 28), "Đóng", _centerActiveBtnStyle))
            {
                _showStageSelectModal = false;
                PlaySfx("ui_select");
            }

            var stages = StageCatalog.All();
            float y = modal.y + 56f;
            for (int i = 0; i < stages.Count; i++)
            {
                var st = stages[i];
                Rect row = new Rect(modal.x + 18, y, modal.width - 36, 86);
                GUI.Box(row, GUIContent.none, _subPanelStyle);

                string bossPortraitId = st.Spawns.Count > 0 ? st.Spawns[0].Definition.PortraitId : "boss-ashen";
                GUI.DrawTexture(new Rect(row.x + 10, row.y + 11, 64, 64),
                    ArtCatalog.GetPortrait(bossPortraitId), ScaleMode.ScaleToFit);

                GUI.Label(new Rect(row.x + 86, row.y + 8, 430, 22),
                    $"ẢI {i + 1}: {st.DisplayName}  ({st.Width}×{st.Height} · {st.TurnLimit} lượt)", _headerStyle);
                GUI.Label(new Rect(row.x + 86, row.y + 32, 440, 46),
                    StageCatalog.GetPuzzleDescription(st.Id), _labelStyle);

                if (GUI.Button(new Rect(row.xMax - 144, row.y + 23, 130, 38), "▶ VÀO ẢI", _centerActiveBtnStyle))
                {
                    SelectStageFromMenu(st.Id);
                    return;
                }

                y += 94f;
            }
        }

        private void DrawLoreModal()
        {
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0.01f, 0.01f, 0.03f, 0.72f));
            Rect modal = new Rect(UiW * 0.5f - 390f, 64f, 780f, 592f);
            GUI.Box(modal, GUIContent.none, _panelStyle);
            DrawRect(new Rect(modal.x, modal.y, modal.width, 3), new Color(0.90f, 0.76f, 0.35f, 0.95f));

            GUI.Label(new Rect(modal.x + 20, modal.y + 14, 560, 28),
                "📜 CỐT TRUYỆN 3 HỒI — BIÊN NIÊN SỬ OAKHAVEN", _headerStyle);
            if (GUI.Button(new Rect(modal.xMax - 92, modal.y + 12, 74, 28), "Đóng", _centerActiveBtnStyle))
            {
                _showLoreModal = false;
                PlaySfx("ui_select");
            }

            var acts = DialogueCatalog.GetThreeActsLore();
            Rect viewRect = new Rect(modal.x + 16, modal.y + 52, modal.width - 32, modal.height - 108);
            Rect contentRect = new Rect(0, 0, viewRect.width - 22, acts.Count * 210f + 20f);
            _loreScroll = GUI.BeginScrollView(viewRect, _loreScroll, contentRect);

            float y = 6f;
            for (int i = 0; i < acts.Count; i++)
            {
                Rect box = new Rect(4, y, contentRect.width - 8, 198f);
                GUI.Box(box, GUIContent.none, _subPanelStyle);
                GUI.Label(new Rect(box.x + 12, box.y + 8, box.width - 24, 24), $"✦ {acts[i].Title}", _headerStyle);
                GUI.Label(new Rect(box.x + 12, box.y + 36, box.width - 24, 152), acts[i].Content, _labelStyle);
                y += 208f;
            }

            GUI.EndScrollView();

            if (GUI.Button(new Rect(modal.x + 20, modal.yMax - 44, modal.width - 40, 32), "Đóng [ESC]", _centerActiveBtnStyle))
            {
                _showLoreModal = false;
                PlaySfx("ui_select");
            }
        }

        private void DrawTutorialModal()
        {
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0.01f, 0.01f, 0.03f, 0.72f));
            Rect modal = new Rect(UiW * 0.5f - 390f, 64f, 780f, 592f);
            GUI.Box(modal, GUIContent.none, _panelStyle);
            DrawRect(new Rect(modal.x, modal.y, modal.width, 3), new Color(0.35f, 0.65f, 0.95f, 0.95f));

            GUI.Label(new Rect(modal.x + 20, modal.y + 14, 560, 28),
                "❓ HƯỚNG DẪN CHƠI & PHÍM TẮT", _headerStyle);
            if (GUI.Button(new Rect(modal.xMax - 92, modal.y + 12, 74, 28), "Đóng", _centerActiveBtnStyle))
            {
                _showTutorialModal = false;
                PlaySfx("ui_select");
            }

            Rect viewRect = new Rect(modal.x + 16, modal.y + 52, modal.width - 32, modal.height - 108);
            Rect contentRect = new Rect(0, 0, viewRect.width - 22, 620f);
            _tutorialScroll = GUI.BeginScrollView(viewRect, _tutorialScroll, contentRect);

            float y = 6f;
            y = DrawTutorialCard(contentRect.width - 8, y, "1. PHASE 1 — GIẢI ĐỐ BÀN CỜ (DƯỚI 70% HP BOSS)",
                "• Mỗi lượt Kẻ Lang Thang có 2 AP (1 Di chuyển tối đa 2 ô + 1 Đánh/Kỹ năng/Guard).\n" +
                "• Ô ĐỎ NHẤP NHÁY là vùng nguy hiểm (Telegraph) Boss sẽ tấn công vào cuối lượt. Hãy di chuyển né ra ngoài hoặc nấp sau Trụ Đá (chặn LOS)!\n" +
                "• ĐỊA HÌNH: Sàn Nứt (sập thành Vực thẳm), Đầm Độc (tốn 2 bước, -3 HP), Dung Nham (-4 HP), Rương Kho Báu (+4 HP tức thời).", 116f);

            y = DrawTutorialCard(contentRect.width - 8, y, "2. PHASE 2 — DUEL MODE (ĐẤU CHIÊU ĐỐI KHÁNG 1V1)",
                "• Khi HP Boss tụt xuống dưới 70%, bàn cờ chuyển sang sàn đấu Duel 1v1.\n" +
                "• Mỗi vòng Boss ra 1 tuyệt kỹ (theo chu kỳ 2 chiêu). Bạn chọn 1 trong 3 thẻ chiêu Clash để đối đầu.\n" +
                "• Cảnh báo Thắng/Hòa/Thua mặc định được ẨN để giữ thử thách: hãy quan sát Hệ & Uy lực hoặc bấm nút [💡 GỢI Ý (H)] để hiện dự đoán và khắc chế đã học!\n" +
                "• THẮNG: Gắn hiệu ứng (Stagger ×2 / Poison / Flanked ×1.5) + tự động tung đòn kết liễu! HÒA: Mỗi bên mất 10% uy lực. THUA: Nhận nguyên Uy lực Boss.", 148f);

            y = DrawTutorialCard(contentRect.width - 8, y, "3. LỜI NGUYỀN TUA NGƯỢC (REWIND) & SỔ TAY CHIÊU [K]",
                "• Khi gục ngã hoặc bước nhầm, bấm nút [UNDO / Rewind (R)] để quay về Checkpoint đầu lượt hoặc đầu Duel.\n" +
                "• Mọi chiêu Boss từng thấy và đòn khắc chế bạn đã thắng Clash được LƯU TRONG SỔ TAY [K] xuyên suốt cả lượt chạy (kể cả qua Rewind và chuyển ải).", 104f);

            DrawTutorialCard(contentRect.width - 8, y, "4. BẢNG PHÍM TẮT NHANH",
                "• [G] Di chuyển (Move)     ·  [S] Chém thường (Slash)  ·  [D] Hộ Kiếm (Guard -80% DMG)\n" +
                "• [1] Trảm Tốc (5 DMG)     ·  [2] Bùng Nổ Hoại Tử (Chữ thập)  ·  [E / 3] Bước Nhảy Thời Gian\n" +
                "• [Space] Kết thúc lượt / Qua thoại  ·  [R] Rewind  ·  [H] Bật/Tắt Gợi ý Duel  ·  [K] Chiêu đã học  ·  [ESC] Tạm dừng", 104f);

            GUI.EndScrollView();

            if (GUI.Button(new Rect(modal.x + 20, modal.yMax - 44, modal.width - 40, 32), "Đã hiểu [ESC]", _centerActiveBtnStyle))
            {
                _showTutorialModal = false;
                PlaySfx("ui_select");
            }
        }

        private float DrawTutorialCard(float w, float y, string title, string body, float height)
        {
            Rect box = new Rect(4, y, w, height);
            GUI.Box(box, GUIContent.none, _subPanelStyle);
            GUI.Label(new Rect(box.x + 12, box.y + 8, box.width - 24, 24), title, _headerStyle);
            GUI.Label(new Rect(box.x + 12, box.y + 34, box.width - 24, height - 42), body, _labelStyle);
            return y + height + 10f;
        }

        private void DrawCreditsModal()
        {
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0.01f, 0.01f, 0.03f, 0.72f));
            Rect modal = new Rect(UiW * 0.5f - 290f, UiH * 0.5f - 175f, 580f, 350f);
            GUI.Box(modal, GUIContent.none, _panelStyle);
            DrawRect(new Rect(modal.x, modal.y, modal.width, 3), new Color(0.90f, 0.76f, 0.35f, 0.95f));

            GUI.Label(new Rect(modal.x + 20, modal.y + 14, modal.width - 40, 28), "★ THÔNG TIN DỰ ÁN & CREDITS", _headerStyle);
            GUI.Label(new Rect(modal.x + 24, modal.y + 54, modal.width - 48, 220),
                "BEHIND THE HERO — Đồ án Phát triển Game (Unity 6000.6)\n" +
                "Nhóm 10 · Lớp 65CNTT · Trường Đại học Thủy Lợi\n\n" +
                "• Ngô Quý Kiên (2351060457) — Kiến trúc Core, Ma trận cân bằng & Duel Loop\n" +
                "• Nguyễn Tuấn Kiệt (2351060458) — A* Pathfinding, AI Telegraph & RunKnowledge Rewind\n" +
                "• Nguyễn Xuân Lăng (2351060459) — Spritesheet, Tileset, VFX, 5 Ải giải đố & Giao diện UI/Cutscene\n" +
                "• Phạm Thị Huyền Linh (2351060460) — Kịch bản 3 Hồi, 2 Kết thúc & Hệ thống Âm thanh\n\n" +
                "• Tài nguyên tham khảo & công cụ: Kenney Tiny Dungeon / Pixel UI, 0x72 Dungeon Tileset.",
                _labelStyle);

            if (GUI.Button(new Rect(modal.x + 20, modal.yMax - 46, modal.width - 40, 34), "Đóng [ESC]", _centerActiveBtnStyle))
            {
                _showCreditsModal = false;
                PlaySfx("ui_select");
            }
        }

        private void DrawTopBar()
        {
            var state = _session.State;
            var boss = state.Units.Boss;
            bool inDuel = state.Duel.IsActive;

            float barX = inDuel ? 8f : 280f;
            float barW = UiW - barX - 8f;
            GUI.Box(new Rect(barX, 8, barW, 84), GUIContent.none, _panelStyle);

            if (_options.ShowDevStageBar)
            {
                GUI.Label(new Rect(barX + 12, 14, 90, 22), "MÀN CHƠI:", _headerStyle);
                for (int i = 0; i < StageIds.Length; i++)
                {
                    string sid = StageIds[i];
                    var st = _currentStageId == sid ? _centerActiveBtnStyle : _centerBtnStyle;
                    if (GUI.Button(new Rect(barX + 102 + i * 62, 12, 58, 26), $"Ải {i + 1}", st))
                    {
                        LoadStage(sid, playIntro: true);
                    }
                }
            }
            else
            {
                GUI.Label(new Rect(barX + 12, 14, 410, 24), $"⚔ {state.Stage.DisplayName}", _headerStyle);
            }

            float rightEdge = barX + barW - 10f;
            if (GUI.Button(new Rect(rightEdge - 96, 12, 96, 26), "⏸ PAUSE", _isPaused ? _centerActiveBtnStyle : _centerBtnStyle))
            {
                TogglePause();
            }
            if (GUI.Button(new Rect(rightEdge - 196, 12, 94, 26), "⚙ CÀI ĐẶT", _showSettingsModal ? _centerActiveBtnStyle : _centerBtnStyle))
            {
                CloseAllModals();
                _showSettingsModal = true;
                PlaySfx("ui_select");
            }
            if (GUI.Button(new Rect(rightEdge - 306, 12, 104, 26), "↻ RESET[X]", _centerBtnStyle))
            {
                LoadStage(_currentStageId, playIntro: false);
            }
            if (GUI.Button(new Rect(rightEdge - 390, 12, 78, 26), "↺ UNDO", _centerBtnStyle))
            {
                ExecuteRewind();
            }
            int knownCount = state.Knowledge?.KnownBossSkillCount ?? 0;
            if (GUI.Button(new Rect(rightEdge - 516, 12, 120, 26), $"📖 CHIÊU ({knownCount})",
                _showLearnedSkillsModal ? _centerActiveBtnStyle : _centerBtnStyle))
            {
                ToggleLearnedSkillsModal();
            }

            if (boss != null)
            {
                var bossPortrait = ArtCatalog.GetPortrait(boss.Definition.PortraitId);
                GUI.DrawTexture(new Rect(barX + 12, 42, 42, 42), bossPortrait, ScaleMode.ScaleToFit);

                string phaseTag = inDuel ? " [DUEL]" : " [BÀN CỜ]";
                string armTag = boss.Definition.Armor > 0 ? $" · Giáp {boss.Definition.Armor}" : string.Empty;
                string statusTag = FormatUnitStatusTags(boss);
                GUI.Label(new Rect(barX + 62, 41, 355, 20),
                    $"{boss.Definition.DisplayName}{phaseTag}{armTag}{statusTag}", _headerStyle);

                Rect barBg = new Rect(barX + 62, 63, 330, 18);
                DrawRect(barBg, new Color(0.18f, 0.08f, 0.08f, 1f));
                float ratio = Mathf.Clamp01(boss.HpRatio);
                DrawRect(new Rect(barBg.x, barBg.y, barBg.width * ratio, barBg.height),
                    new Color(0.75f, 0.16f, 0.18f, 1f));
                float mark70X = barBg.x + barBg.width * state.Rules.Phase2Threshold;
                DrawRect(new Rect(mark70X, barBg.y - 2, 3, barBg.height + 4),
                    new Color(0.95f, 0.82f, 0.30f, 1f));
                GUI.Label(new Rect(barBg.x + 8, barBg.y - 1, barBg.width, 20),
                    $"HP BOSS: {boss.Hp} / {boss.MaxHp}  (Phase 2 < 70%)", _boldLabelStyle);
            }

            float descX = barX + 412f;
            string bannerLine = _enemyBeatTimer > 0f
                ? "⚔ LƯỢT CỦA BOSS — ĐÃ THỰC THI Ý ĐỒ!"
                : _combatBannerText;
            string puzzleDesc = StageCatalog.GetPuzzleDescription(_currentStageId);
            GUI.Label(new Rect(descX, 40, barX + barW - descX - 10f, 48),
                $"★ {puzzleDesc}\n► {bannerLine}", _labelStyle);
        }

        private static string FormatUnitStatusTags(UnitState unit)
        {
            if (unit == null || unit.Statuses.Count == 0) return string.Empty;
            var list = new List<string>();
            foreach (var st in unit.Statuses.Effects)
            {
                list.Add($"{st.Id.Value.ToUpperInvariant()}({unit.Statuses.GetRemaining(st.Id)}T)");
            }
            return " · " + string.Join(", ", list);
        }

        private void DrawLeftHudPanel()
        {
            var state = _session.State;
            var player = state.Units.Player;

            Rect panel = new Rect(8, 8, 264, UiH - 16);
            GUI.Box(panel, GUIContent.none, _panelStyle);

            GUI.Label(new Rect(20, 16, 240, 24), "HOODED WANDERER", _headerStyle);

            Rect portBox = new Rect(20, 44, 78, 78);
            DrawRect(portBox, new Color(0.28f, 0.24f, 0.18f, 1f));
            GUI.DrawTexture(new Rect(portBox.x + 3, portBox.y + 3, portBox.width - 6, portBox.height - 6),
                ArtCatalog.GetPortrait("wanderer"), ScaleMode.ScaleToFit);

            if (player != null)
            {
                GUI.Label(new Rect(106, 42, 155, 20), $"LV 12  |  ATK {player.Definition.Attack}", _boldLabelStyle);

                Rect hpBg = new Rect(106, 65, 152, 16);
                DrawRect(hpBg, new Color(0.22f, 0.08f, 0.10f, 1f));
                DrawRect(new Rect(hpBg.x, hpBg.y, hpBg.width * Mathf.Clamp01(player.HpRatio), hpBg.height),
                    new Color(0.82f, 0.18f, 0.22f, 1f));
                GUI.Label(new Rect(hpBg.x + 6, hpBg.y - 2, hpBg.width, 18), $"HP: {player.Hp} / {player.MaxHp} ♦", _boldLabelStyle);

                Rect mpBg = new Rect(106, 86, 152, 15);
                DrawRect(mpBg, new Color(0.08f, 0.14f, 0.26f, 1f));
                DrawRect(new Rect(mpBg.x, mpBg.y, mpBg.width, mpBg.height),
                    new Color(0.22f, 0.52f, 0.88f, 1f));
                GUI.Label(new Rect(mpBg.x + 6, mpBg.y - 2, mpBg.width, 18), "MP: 3 / 3 ❖", _boldLabelStyle);

                string apDiamonds = player.ActionPoints >= 2 ? "◆ ◆" : (player.ActionPoints == 1 ? "◆ ◇" : "◇ ◇");
                GUI.Label(new Rect(106, 105, 155, 20), $"AP: {player.ActionPoints} / {state.Rules.ApPerTurn}  {apDiamonds}", _headerStyle);
            }

            GUI.Label(new Rect(20, 132, 240, 20), "STATUS:", _headerStyle);
            string phaseText = $"Turn: {state.Turn.TurnIndex} / {state.Stage.TurnLimit}  ({state.Turn.Phase})";
            GUI.Label(new Rect(20, 152, 240, 20), phaseText, _boldLabelStyle);

            string statusLine = "Hiệu ứng: Không";
            if (player != null && player.Statuses.Count > 0)
            {
                var names = new List<string>();
                foreach (var st in player.Statuses.Effects)
                {
                    names.Add($"{st.Id.Value}({player.Statuses.GetRemaining(st.Id)}T)");
                }
                statusLine = "Hiệu ứng: " + string.Join(", ", names);
            }
            GUI.Label(new Rect(20, 172, 240, 20), statusLine, _labelStyle);
            DrawRunKnowledge(new Rect(20, 192, 240, 20), state.Knowledge);

            GUI.Label(new Rect(20, 218, 240, 22), "ACTIONS:", _headerStyle);
            float by = 244f;
            float bh = 31f;
            float gap = 35f;

            bool prevEnabled = GUI.enabled;
            bool isPlayerTurn = state.Turn.Phase == TurnPhase.Player && !state.IsOver && player != null && !player.IsStaggered;
            bool canMove = isPlayerTurn && !player.HasMovedThisTurn && !player.IsRooted && state.Turn.CanSpend(player.Id, 1);
            bool canAct = isPlayerTurn && !player.HasActedThisTurn && state.Turn.CanSpend(player.Id, 1);
            int cdBurst = player?.GetCooldown("necrotic-burst") ?? 0;
            int cdStep = player?.GetCooldown("temporal-step") ?? 0;

            GUI.enabled = prevEnabled && canMove;
            string moveLabel = (player != null && player.HasMovedThisTurn) ? "➡  Move [ĐÃ ĐI]              [G]" : "➡  Move                        [G]";
            if (GUI.Button(new Rect(20, by, 240, bh), moveLabel,
                _cmdMode == PlayerCommandMode.Move ? _activeBtnStyle : _btnStyle))
            {
                SetCommandMode(PlayerCommandMode.Move);
            }
            by += gap;

            GUI.enabled = prevEnabled && canAct;
            if (GUI.Button(new Rect(20, by, 240, bh), "⚔  Slash                       [S]",
                _cmdMode == PlayerCommandMode.Attack ? _activeBtnStyle : _btnStyle))
            {
                SetCommandMode(PlayerCommandMode.Attack);
            }
            by += gap;

            if (GUI.Button(new Rect(20, by, 240, bh), "✦  Swift Slash                 [1]",
                _cmdMode == PlayerCommandMode.Skill1 ? _activeBtnStyle : _btnStyle))
            {
                SetCommandMode(PlayerCommandMode.Skill1);
            }
            by += gap;

            GUI.enabled = prevEnabled && canAct && cdBurst == 0;
            string burstLabel = cdBurst > 0 ? $"☣  Necrotic Burst [CD:{cdBurst}T]      [2]" : "☣  Necrotic Burst              [2]";
            if (GUI.Button(new Rect(20, by, 240, bh), burstLabel,
                _cmdMode == PlayerCommandMode.Skill2 ? _activeBtnStyle : _btnStyle))
            {
                SetCommandMode(PlayerCommandMode.Skill2);
            }
            by += gap;

            GUI.enabled = prevEnabled && canAct && cdStep == 0;
            string stepLabel = cdStep > 0 ? $"⌛  Temporal Step [CD:{cdStep}T]       [E]" : "⌛  Temporal Step               [E]";
            if (GUI.Button(new Rect(20, by, 240, bh), stepLabel,
                _cmdMode == PlayerCommandMode.Skill3 ? _activeBtnStyle : _btnStyle))
            {
                SetCommandMode(PlayerCommandMode.Skill3);
            }
            by += gap;

            GUI.enabled = prevEnabled && canAct;
            if (GUI.Button(new Rect(20, by, 240, bh), "🛡  Guard                       [D]", _btnStyle))
            {
                ExecuteGuard();
            }
            by += gap;

            GUI.enabled = prevEnabled && state.Snapshots.CanRewind;
            if (GUI.Button(new Rect(20, by, 240, bh), "↺  Rewind                      [R]", _btnStyle))
            {
                ExecuteRewind();
            }
            by += gap;

            GUI.enabled = prevEnabled && state.Turn.Phase == TurnPhase.Player && !state.IsOver;
            if (GUI.Button(new Rect(20, by, 240, bh), "⏭  End Turn                [Space]", _btnStyle))
            {
                ExecuteEndTurn();
            }
            GUI.enabled = prevEnabled;
            by += gap + 4f;

            GUI.Label(new Rect(20, by, 240, 20), "THÔNG TIN & GỢI Ý:", _headerStyle);
            by += 20f;
            if (_hoverCoord.IsValid && state.Grid.InBounds(_hoverCoord))
            {
                var cState = state.Grid.Get(_hoverCoord);
                var tSpec = TerrainCatalog.Get(cState.Terrain);
                string collapseInfo = cState.TurnsUntilCollapse > 0 ? $" [Sập sau {cState.TurnsUntilCollapse} lượt]" : "";
                GUI.Label(new Rect(20, by, 240, 82),
                    $"Ô ({_hoverCoord.X},{_hoverCoord.Y}): {tSpec.DisplayName}{collapseInfo}\n• {tSpec.TacticalDescription}",
                    _labelStyle);
            }
            else
            {
                GUI.Label(new Rect(20, by, 240, 82), GetSelectedCommandTip(), _labelStyle);
            }

            var boss = state.Units.Boss;
            if (!state.IsOver && boss != null && state.Telegraph.TryGet(boss.Id, out var intent) &&
                _session.Skills.TryGet(intent.SkillId, out var skill))
            {
                string familiarity = state.Knowledge.HasSeenBossSkill(skill.Id) ? "Đã gặp" : "Chiêu mới";
                GUI.Label(new Rect(20, 652, 240, 44), $"Boss chuẩn bị · {familiarity}\n{skill.DisplayName}", _labelStyle);
            }
        }

        private string GetSelectedCommandTip()
        {
            switch (_cmdMode)
            {
                case PlayerCommandMode.Move:
                    return "Di chuyển [G]: Đi tối đa 2 ô theo đường A* (Ô xanh lam). Né ô đỏ Telegraph!";
                case PlayerCommandMode.Attack:
                    return "Slash [S]: Đánh thường 4 DMG (tầm 1 ô quanh footprint Boss).";
                case PlayerCommandMode.Skill1:
                    return "Swift Slash [1]: Chém 5 DMG (Physical), tầm 1 ô, không hồi chiêu.";
                case PlayerCommandMode.Skill2:
                    return "Necrotic Burst [2]: Nổ 5 DMG hình chữ thập 5 ô (tầm 1–2, CD 1 lượt). Rê chuột xem vùng tím!";
                case PlayerCommandMode.Skill3:
                    return "Temporal Step [E]: Dịch chuyển 1–3 ô vượt Vực thẳm + nhận Guard (CD 2 lượt).";
                default:
                    return string.Empty;
            }
        }

        private void DrawCombatLogStrip()
        {
            if (_combatLog.Count == 0) return;
            Rect strip = new Rect(UiW - 376, UiH - 96, 368, 88);
            GUI.Box(strip, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(strip.x + 10, strip.y + 4, 240, 18), "NHẬT KÝ CHIẾN ĐẤU:", _headerStyle);
            string body = string.Join("\n", _combatLog);
            GUI.Label(new Rect(strip.x + 10, strip.y + 24, strip.width - 20, 60), body, _labelStyle);
        }

        private void DrawBoardWorldLabels()
        {
            if (_mainCam == null) return;
            var state = _session.State;
            float sx = UiW / Mathf.Max(1f, Screen.width);
            float sy = UiH / Mathf.Max(1f, Screen.height);

            for (int y = 0; y < state.Grid.Height; y++)
            {
                for (int x = 0; x < state.Grid.Width; x++)
                {
                    var c = new GridCoord(x, y);
                    var cell = state.Grid.Get(c);
                    Vector3 sp = _mainCam.WorldToScreenPoint(CoordToWorld(c));
                    if (sp.z < 0) continue;
                    float guiX = sp.x * sx;
                    float guiY = (Screen.height - sp.y) * sy;

                    if (cell.TurnsUntilCollapse > 0)
                    {
                        GUI.Box(new Rect(guiX - 28, guiY + 12, 56, 17), $"NỨT:{cell.TurnsUntilCollapse}T", _smallBadgeStyle);
                    }
                    else if (cell.Terrain == TerrainId.Chest)
                    {
                        GUI.Box(new Rect(guiX - 28, guiY + 12, 56, 17), "RƯƠNG", _badgeWinStyle);
                    }
                    else if (cell.IsHazard)
                    {
                        GUI.Box(new Rect(guiX - 26, guiY + 12, 52, 16), $"-{cell.HazardDamagePerTurn}HP", _badgeLoseStyle);
                    }
                }
            }

            foreach (var u in state.Units.All)
            {
                if (!_unitAnimators.TryGetValue(u.Id, out var anim) || anim == null) continue;
                Vector3 topWorld = anim.transform.position + new Vector3(0f, u.SizeY > 1 ? 1.32f : 0.72f, 0f);
                Vector3 sp = _mainCam.WorldToScreenPoint(topWorld);
                if (sp.z < 0) continue;
                float guiX = sp.x * sx;
                float guiY = (Screen.height - sp.y) * sy;

                Rect bar = new Rect(guiX - 30, guiY - 10, 60, 9);
                DrawRect(bar, new Color(0.1f, 0.1f, 0.12f, 0.9f));
                Color hpCol = u.Faction == UnitFaction.Player
                    ? new Color(0.25f, 0.85f, 0.38f, 1f)
                    : new Color(0.92f, 0.24f, 0.24f, 1f);
                DrawRect(new Rect(bar.x + 1, bar.y + 1, (bar.width - 2) * Mathf.Clamp01(u.HpRatio), bar.height - 2),
                    hpCol);
                GUI.Label(new Rect(guiX - 32, guiY - 26, 64, 16), $"{u.Hp}/{u.MaxHp}", _smallBadgeStyle);
            }

            var popups = _vfxManager.ActivePopups;
            Color prevColor = GUI.contentColor;
            for (int i = 0; i < popups.Count; i++)
            {
                var p = popups[i];
                Vector3 sp = _mainCam.WorldToScreenPoint(p.WorldPos);
                if (sp.z < 0) continue;
                float guiX = sp.x * sx;
                float guiY = (Screen.height - sp.y) * sy;
                GUI.contentColor = p.Color;
                GUI.Label(new Rect(guiX - 95, guiY - 14, 190, 26), p.Text, _popupStyle);
            }
            GUI.contentColor = prevColor;
        }

        private void DrawDuelScreen()
        {
            var state = _session.State;
            var duel = state.Duel;
            var player = state.Units.Player;
            var boss = state.Units.Boss;
            if (duel == null || player == null || boss == null) return;

            Rect arena = new Rect(8, 96, UiW - 16, UiH - 104);
            var bgTex = ArtCatalog.GetBackground(duel.Definition.BackgroundId);
            GUI.DrawTexture(arena, bgTex, ScaleMode.StretchToFill);
            DrawRect(arena, new Color(0.03f, 0.03f, 0.06f, 0.22f));

            Rect bannerRect = new Rect(arena.x + 16, arena.y + 10, arena.width - 32, 76);
            GUI.Box(bannerRect, GUIContent.none, _panelStyle);

            string bossSkillName = duel.State.BossSkillId;
            if (_session.Skills.TryGet(duel.State.BossSkillId, out var bSkill))
            {
                bossSkillName = bSkill.DisplayName;
            }
            string familiarityTag = state.Knowledge.HasSeenBossSkill(duel.State.BossSkillId) ? "ĐÃ GẶP" : "CHIÊU MỚI";

            GUI.Label(new Rect(bannerRect.x + 14, bannerRect.y + 8, bannerRect.width - 380, 26),
                $"⚔ PHASE 2: DUEL MODE — VÒNG {duel.State.Round}  |  BOSS: [{bossSkillName.ToUpperInvariant()}] (UY LỰC {duel.State.BossPower} | HỆ {duel.State.BossElement.Value} · {familiarityTag})",
                _headerStyle);
            GUI.Label(new Rect(bannerRect.x + 14, bannerRect.y + 38, bannerRect.width - 380, 28),
                string.IsNullOrEmpty(_lastDuelRoundNotice)
                    ? "Quan sát Hệ & Uy lực của Boss để chọn 1 trong 3 thẻ Clash — bấm [💡 Gợi ý] nếu cần cảnh báo Thắng/Thua!"
                    : _lastDuelRoundNotice,
                _boldLabelStyle);

            string hintBtnText = _showDuelHint ? "💡 GỢI Ý: BẬT [H]" : "💡 GỢI Ý: TẮT [H]";
            if (GUI.Button(new Rect(bannerRect.xMax - 352, bannerRect.y + 18, 168, 38),
                hintBtnText, _showDuelHint ? _centerActiveBtnStyle : _centerBtnStyle))
            {
                ToggleDuelHint();
            }

            int knownCount = state.Knowledge?.KnownBossSkillCount ?? 0;
            if (GUI.Button(new Rect(bannerRect.xMax - 176, bannerRect.y + 18, 164, 38),
                $"📖 CHIÊU ĐÃ HỌC ({knownCount})", _showLearnedSkillsModal ? _centerActiveBtnStyle : _centerBtnStyle))
            {
                ToggleLearnedSkillsModal();
            }

            int frameIdx = Mathf.FloorToInt(Time.unscaledTime * 6f) % 4;
            UnitAnimState pAnimState = (_duelVfxTimer > 0f && _duelVfxOnBoss) ? UnitAnimState.Attack : UnitAnimState.Idle;
            UnitAnimState bAnimState = (_duelVfxTimer > 0f && !_duelVfxOnBoss) ? UnitAnimState.Attack : UnitAnimState.Idle;

            var pFrames = ArtCatalog.GetUnitAnimFrames(player.Definition.Id, player.IsAlive ? pAnimState : UnitAnimState.Death);
            var bFrames = ArtCatalog.GetUnitAnimFrames(boss.Definition.Id, boss.IsAlive ? bAnimState : UnitAnimState.Death);

            Rect pRect = new Rect(arena.x + 95, arena.y + 110, 185, 185);
            float bossSize = boss.SizeX > 1 ? 235f : 210f;
            Rect bRect = new Rect(arena.xMax - 95 - bossSize, arena.y + 92, bossSize, bossSize);

            DrawSpriteOnGui(pRect, pFrames[frameIdx % pFrames.Length], flipX: false);
            DrawSpriteOnGui(bRect, bFrames[frameIdx % bFrames.Length], flipX: true);
            DrawDuelHealth(new Rect(pRect.x, pRect.yMax, pRect.width, 22), player);
            DrawDuelHealth(new Rect(bRect.x, bRect.yMax, bRect.width, 22), boss);
            DrawRunKnowledge(new Rect(pRect.x, pRect.yMax + 24, 290, 20), state.Knowledge);

            string pStatus = FormatUnitStatusTags(player);
            if (!string.IsNullOrEmpty(pStatus))
            {
                GUI.Box(new Rect(pRect.x, pRect.yMax + 44, pRect.width, 20), pStatus.Trim(' ', '·'), _badgeInfoStyle);
            }
            string bStatus = FormatUnitStatusTags(boss);
            if (!string.IsNullOrEmpty(bStatus))
            {
                GUI.Box(new Rect(bRect.x, bRect.yMax + 26, bRect.width, 20), bStatus.Trim(' ', '·'), _badgeDrawStyle);
            }

            if (_duelVfxTimer > 0f)
            {
                var vfxFrames = ArtCatalog.GetVfxFrames(_activeDuelVfxId);
                float normalized = 1f - (_duelVfxTimer / 0.70f);
                int hitLoop = _duelFinisherHitsShown > 1
                    ? Mathf.FloorToInt(normalized * _duelFinisherHitsShown * vfxFrames.Length) % vfxFrames.Length
                    : Mathf.Clamp(Mathf.FloorToInt(normalized * vfxFrames.Length), 0, vfxFrames.Length - 1);
                Rect vRect = _duelVfxOnBoss ? bRect : pRect;
                DrawSpriteOnGui(vRect, vfxFrames[hitLoop], flipX: false);

                if (_duelVfxOnBoss && _duelFinisherHitsShown > 1)
                {
                    int currentHit = Mathf.Clamp(Mathf.FloorToInt(normalized * _duelFinisherHitsShown) + 1, 1, _duelFinisherHitsShown);
                    string pips = new string('◆', currentHit) + new string('◇', _duelFinisherHitsShown - currentHit);
                    GUI.Box(new Rect(UiW * 0.5f - 150, arena.y + 115, 300, 30),
                        $"⚔ KẾT LIỄU LIÊN HOÀN: {pips} ({currentHit}/{_duelFinisherHitsShown})", _badgeWinStyle);
                }
            }

            if (_showDuelHint)
            {
                Rect hintStrip = new Rect(arena.x + 16, arena.yMax - 208f, arena.width - 32, 30f);
                GUI.Box(hintStrip, GUIContent.none, _subPanelStyle);
                var learnedCounters = state.Knowledge.GetWonClashPlayerSkills(duel.State.BossSkillId);
                string counterSummary;
                if (learnedCounters.Count > 0)
                {
                    var displayNames = new List<string>();
                    for (int i = 0; i < learnedCounters.Count; i++)
                    {
                        displayNames.Add(_session.Skills.TryGet(learnedCounters[i], out var sk) ? sk.DisplayName : learnedCounters[i]);
                    }
                    counterSummary = $"★ GỢI Ý & KÝ ỨC ĐÃ HỌC: Đã từng khắc chế [{bossSkillName}] bằng: {string.Join(", ", displayNames)}";
                }
                else
                {
                    counterSummary = $"💡 GỢI Ý ĐANG BẬT: Cảnh báo Thắng/Hòa/Thua đã mở · Chưa ghi nhớ khắc chế cho [{bossSkillName}] (Thắng 1 lần để lưu vào Sổ tay!)";
                }
                GUI.Label(new Rect(hintStrip.x + 12, hintStrip.y + 4, hintStrip.width - 24, 22), counterSummary, _boldLabelStyle);
            }

            var options = duel.GetOptions();
            float cardW = (arena.width - 72f) / Mathf.Max(1, options.Count);
            float cardY = arena.yMax - 172f;

            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                Rect card = new Rect(arena.x + 16 + i * (cardW + 20f), cardY, cardW, 158f);
                GUI.Box(card, GUIContent.none, _panelStyle);

                GUI.Label(new Rect(card.x + 12, card.y + 8, card.width - 24, 24),
                    $"[{i + 1}] {opt.DisplayName}", _headerStyle);

                int basePower = opt.Prediction.PlayerPower - opt.Prediction.Bonus;
                string skillElement = _session.Skills.TryGet(opt.SkillId, out var pSkill)
                    ? pSkill.Element.Value
                    : "Physical";

                if (_showDuelHint)
                {
                    string bonusSign = opt.Prediction.Bonus >= 0 ? $"+{opt.Prediction.Bonus}" : opt.Prediction.Bonus.ToString();
                    GUI.Label(new Rect(card.x + 12, card.y + 34, card.width - 24, 42),
                        $"Hệ: {skillElement}  |  Uy lực gốc: {basePower}  ({bonusSign} khắc chế)\n⇒ TỔNG UY LỰC: {opt.Prediction.PlayerPower}  (so với Boss {opt.Prediction.BossPower})",
                        _labelStyle);

                    GUIStyle badgeSt = opt.Prediction.Result == ClashResult.Win
                        ? _badgeWinStyle
                        : (opt.Prediction.Result == ClashResult.Draw ? _badgeDrawStyle : _badgeLoseStyle);
                    bool learnedWin = state.Knowledge.HasWonClash(duel.State.BossSkillId, opt.SkillId);
                    string winLabel = learnedWin ? "★ ĐÃ HỌC KHẮC CHẾ" : "DỰ BÁO: THẮNG";
                    string badgeText = opt.Prediction.Result == ClashResult.Win
                        ? $"{winLabel} (+{duel.Definition.FinisherCount} ĐÒN KẾT LIỄU)"
                        : (opt.Prediction.Result == ClashResult.Draw
                            ? "DỰ BÁO: HÒA (MỖI BÊN -10%)"
                            : "DỰ BÁO: THUA (ĂN ĐÒN BOSS)");
                    GUI.Box(new Rect(card.x + 12, card.y + 80, card.width - 24, 28), badgeText, badgeSt);
                }
                else
                {
                    GUI.Label(new Rect(card.x + 12, card.y + 36, card.width - 24, 38),
                        $"Hệ: {skillElement}  |  Uy lực gốc: {basePower}\n(Tự phán đoán khắc chế so với chiêu Boss {duel.State.BossPower})",
                        _labelStyle);

                    GUI.Box(new Rect(card.x + 12, card.y + 80, card.width - 24, 28),
                        "🔒 ẨN CẢNH BÁO — Bấm [💡 GỢI Ý] để xem", _badgeInfoStyle);
                }

                if (GUI.Button(new Rect(card.x + 12, card.y + 114, card.width - 24, 32),
                    $"⚡ TUNG CHIÊU ĐỐI KHÁNG [{i + 1}]", _centerActiveBtnStyle))
                {
                    _session.TryExecute(ActionRequest.ClashSkill(player.Id, opt.SkillId));
                }
            }
        }

        private void DrawPauseModal()
        {
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0.01f, 0.01f, 0.03f, 0.72f));
            Rect modal = new Rect(UiW * 0.5f - 220f, UiH * 0.5f - 250f, 440f, 500f);
            GUI.Box(modal, GUIContent.none, _panelStyle);
            DrawRect(new Rect(modal.x, modal.y, modal.width, 3), new Color(0.90f, 0.76f, 0.35f, 0.95f));

            GUI.Label(new Rect(modal.x + 20, modal.y + 14, modal.width - 40, 28), "⏸ TẠM DỪNG TRÒ CHƠI", _headerStyle);

            string stageName = _session?.State?.Stage?.DisplayName ?? _currentStageId;
            int deaths = _session?.Knowledge?.Deaths ?? 0;
            int knownSkills = _session?.Knowledge?.KnownBossSkillCount ?? 0;
            GUI.Label(new Rect(modal.x + 20, modal.y + 44, modal.width - 40, 24),
                $"{stageName}  ·  Vòng lặp: {deaths}  |  Chiêu đã biết: {knownSkills}", _mutedLabelStyle);

            float bx = modal.x + 28f;
            float bw = modal.width - 56f;
            float y = modal.y + 80f;

            if (GUI.Button(new Rect(bx, y, bw, 40), "▶  TIẾP TỤC CHƠI [ESC]", _centerActiveBtnStyle))
            {
                TogglePause();
            }
            y += 48f;

            if (GUI.Button(new Rect(bx, y, bw, 36), $"📖  SỔ TAY CHIÊU ĐÃ HỌC ({knownSkills}) [K]", _centerBtnStyle))
            {
                ToggleLearnedSkillsModal();
            }
            y += 44f;

            if (GUI.Button(new Rect(bx, y, bw, 36), "❓  HƯỚNG DẪN CHƠI & PHÍM TẮT", _centerBtnStyle))
            {
                ToggleTutorialModal();
            }
            y += 44f;

            bool canRewind = _screenState == AppScreenState.Playing && _session?.State?.Snapshots?.CanRewind == true;
            bool prevEnabled = GUI.enabled;
            GUI.enabled = prevEnabled && canRewind;
            if (GUI.Button(new Rect(bx, y, bw, 36), "↺  TUA NGƯỢC CHECKPOINT [R]", _centerBtnStyle))
            {
                SetPaused(false);
                ExecuteRewind();
            }
            GUI.enabled = prevEnabled;
            y += 44f;

            if (GUI.Button(new Rect(bx, y, bw, 36), "↻  CHƠI LẠI ẢI NÀY (GIỮ KÝ ỨC CHIÊU)", _centerBtnStyle))
            {
                LoadStage(_currentStageId, playIntro: false);
            }
            y += 44f;

            if (GUI.Button(new Rect(bx, y, bw, 36), "↻  CHƠI LẠI TỪ ĐẦU (RESET KÝ ỨC)", _centerBtnStyle))
            {
                ResetRun();
            }
            y += 44f;

            if (GUI.Button(new Rect(bx, y, bw, 36), "⚙  CÀI ĐẶT (SETTINGS)", _centerBtnStyle))
            {
                CloseAllModals();
                _showSettingsModal = true;
                PlaySfx("ui_select");
            }
            y += 44f;

            if (GUI.Button(new Rect(bx, y, bw, 36), "🏠  VỀ MENU CHÍNH", _centerBtnStyle))
            {
                EnterMainMenu();
            }
        }

        private void DrawLearnedSkillsModal()
        {
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0.01f, 0.01f, 0.03f, 0.72f));
            Rect modal = new Rect(UiW * 0.5f - 370f, 64f, 740f, 592f);
            GUI.Box(modal, GUIContent.none, _panelStyle);
            DrawRect(new Rect(modal.x, modal.y, modal.width, 3), new Color(0.55f, 0.36f, 0.85f, 0.95f));

            GUI.Label(new Rect(modal.x + 20, modal.y + 14, 560, 28),
                "📖 SỔ TAY KÝ ỨC VÒNG LẶP — CHIÊU ĐÃ HỌC", _headerStyle);
            if (GUI.Button(new Rect(modal.xMax - 92, modal.y + 12, 74, 28), "Đóng", _centerActiveBtnStyle))
            {
                _showLearnedSkillsModal = false;
                PlaySfx("ui_select");
            }

            var knowledge = _session?.Knowledge;
            int deaths = knowledge?.Deaths ?? 0;
            var seenBossSkills = knowledge != null ? knowledge.GetSeenBossSkillIds() : Array.Empty<string>();
            GUI.Label(new Rect(modal.x + 20, modal.y + 44, modal.width - 40, 24),
                $"Số vòng lặp tử vong: {deaths}   |   Tuyệt kỹ Boss đã chứng kiến: {seenBossSkills.Count}   (Ký ức được giữ khi Rewind & chuyển ải)",
                _boldLabelStyle);

            var wandererSkills = UnitCatalog.CreateWanderer().SkillIds;
            float bossSectionH = Mathf.Max(66f, seenBossSkills.Count * 66f);
            float contentH = 44f + bossSectionH + 46f + wandererSkills.Count * 54f + 24f;

            Rect viewRect = new Rect(modal.x + 16, modal.y + 76, modal.width - 32, modal.height - 134);
            Rect contentRect = new Rect(0, 0, viewRect.width - 20, contentH);
            _codexScroll = GUI.BeginScrollView(viewRect, _codexScroll, contentRect);

            float y = 6f;
            GUI.Label(new Rect(4, y, contentRect.width - 8, 24), "1. TUYỆT KỸ BOSS ĐÃ GẶP & KHẮC CHẾ ĐÃ HỌC:", _headerStyle);
            y += 28f;

            if (seenBossSkills.Count == 0)
            {
                GUI.Box(new Rect(4, y, contentRect.width - 8, 56), GUIContent.none, _subPanelStyle);
                GUI.Label(new Rect(16, y + 10, contentRect.width - 32, 40),
                    "Chưa ghi nhận chiêu thức nào của Boss trong lượt chơi này.\nHãy giao chiến trên bàn cờ hoặc bước vào Phase 2 Duel — mọi chiêu Boss tung ra và chiêu thắng Clash sẽ tự động lưu tại đây!",
                    _mutedLabelStyle);
                y += 66f;
            }
            else
            {
                for (int i = 0; i < seenBossSkills.Count; i++)
                {
                    string bId = seenBossSkills[i];
                    _session.Skills.TryGet(bId, out var bSk);
                    string bName = bSk != null ? bSk.DisplayName : bId;
                    string bElem = bSk != null ? bSk.Element.Value : "?";
                    int bClash = bSk != null ? bSk.Power : 0;
                    int bBoard = bSk != null ? bSk.BoardPower : 0;

                    var wonCounters = knowledge.GetWonClashPlayerSkills(bId);
                    string counterText;
                    if (wonCounters.Count > 0)
                    {
                        var names = new List<string>();
                        for (int j = 0; j < wonCounters.Count; j++)
                        {
                            names.Add(_session.Skills.TryGet(wonCounters[j], out var pSk) ? pSk.DisplayName : wonCounters[j]);
                        }
                        counterText = "★ Khắc chế đã thắng Clash: " + string.Join(", ", names);
                    }
                    else
                    {
                        counterText = "• Chưa tìm ra chiêu khắc chế trong Duel (Thắng Clash 1 lần để ghi nhớ).";
                    }

                    Rect row = new Rect(4, y, contentRect.width - 8, 58);
                    GUI.Box(row, GUIContent.none, _subPanelStyle);
                    GUI.Label(new Rect(row.x + 12, row.y + 6, row.width - 24, 22),
                        $"• {bName}   [Hệ: {bElem} | Uy lực Duel: {bClash} | Sát thương Bàn cờ: {bBoard}]", _boldLabelStyle);
                    GUI.Label(new Rect(row.x + 12, row.y + 30, row.width - 24, 22), counterText,
                        wonCounters.Count > 0 ? _headerStyle : _mutedLabelStyle);
                    y += 66f;
                }
            }

            y += 10f;
            GUI.Label(new Rect(4, y, contentRect.width - 8, 24), "2. BỘ KỸ NĂNG CỦA KẺ LANG THANG (PHASE 1 & PHASE 2 DUEL):", _headerStyle);
            y += 28f;

            for (int i = 0; i < wandererSkills.Count; i++)
            {
                if (!_session.Skills.TryGet(wandererSkills[i], out var sk)) continue;
                Rect row = new Rect(4, y, contentRect.width - 8, 48);
                GUI.Box(row, GUIContent.none, _subPanelStyle);
                string clashTag = sk.IsClashSkill ? $"Uy lực Clash: {sk.Power}" : "Chỉ bàn cờ";
                GUI.Label(new Rect(row.x + 12, row.y + 6, row.width - 24, 20),
                    $"✦ {sk.DisplayName}   [Hệ: {sk.Element.Value} | {clashTag}]", _boldLabelStyle);
                GUI.Label(new Rect(row.x + 12, row.y + 25, row.width - 24, 18),
                    $"Tầm đánh: {sk.RangeMin}–{sk.RangeMax} ô  ·  Sát thương bàn cờ: {sk.BoardPower}  ·  Hồi chiêu: {sk.CooldownTurns} lượt",
                    _mutedLabelStyle);
                y += 54f;
            }

            GUI.EndScrollView();

            if (GUI.Button(new Rect(modal.x + 20, modal.yMax - 44, modal.width - 40, 32),
                "Đóng Sổ Tay [ESC / K]", _centerActiveBtnStyle))
            {
                _showLearnedSkillsModal = false;
                PlaySfx("ui_select");
            }
        }

        private void DrawRunKnowledge(Rect rect, IRunKnowledge knowledge)
        {
            if (knowledge == null) return;
            GUI.Label(rect, $"Vòng lặp: {knowledge.Deaths}  |  Chiêu đã biết: {knowledge.KnownBossSkillCount}", _labelStyle);
        }

        private void DrawDuelHealth(Rect rect, UnitState unit)
        {
            DrawRect(rect, new Color(0.18f, 0.08f, 0.08f, 1f));
            DrawRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(unit.HpRatio), rect.height),
                new Color(0.75f, 0.16f, 0.18f, 1f));
            GUI.Label(new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height), $"HP {unit.Hp} / {unit.MaxHp}", _boldLabelStyle);
        }

        private static void DrawSpriteOnGui(Rect dst, Sprite sp, bool flipX)
        {
            if (sp == null || sp.texture == null) return;
            Texture2D tex = sp.texture;
            Rect tr = sp.textureRect;
            Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            if (flipX)
            {
                uv = new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height);
            }
            GUI.DrawTextureWithTexCoords(dst, tex, uv);
        }

        private void DrawPhase2TransitionOverlay()
        {
            float elapsed = TransitionDuration - _transitionTimer;
            Color overlayCol;
            string caption;
            if (elapsed < 0.45f)
            {
                float flash = 0.65f * (elapsed / 0.45f);
                overlayCol = new Color(0.60f, 0.08f, 0.12f, flash);
                caption = "⚔ BOSS DƯỚI 70% HP! ⚔";
            }
            else if (elapsed < 0.90f)
            {
                float fadeOut = Mathf.Clamp01((elapsed - 0.45f) / 0.35f);
                overlayCol = new Color(0.02f, 0.02f, 0.04f, Mathf.Lerp(0.55f, 0.96f, fadeOut));
                caption = "⚔ PHASE 2: DUEL MODE! ⚔";
            }
            else
            {
                float fadeIn = 1f - Mathf.Clamp01((elapsed - 0.90f) / 0.45f);
                overlayCol = new Color(0.02f, 0.02f, 0.04f, fadeIn * 0.96f);
                caption = "⚔ ĐẤU KỸ CLASH 1V1! ⚔";
            }

            DrawRect(new Rect(0, 0, UiW, UiH), overlayCol);
            GUI.Label(new Rect(0, UiH * 0.4f, UiW, 80), caption, _phase2BannerStyle);
        }

        private void DrawStageResultModal()
        {
            var res = _session.State.Result;
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0.01f, 0.01f, 0.03f, 0.58f));
            Rect modal = new Rect(UiW * 0.5f - 270f, UiH * 0.5f - 125f, 540f, 250f);
            GUI.Box(modal, GUIContent.none, _panelStyle);
            DrawRect(new Rect(modal.x, modal.y, modal.width, 3), new Color(0.90f, 0.76f, 0.35f, 0.95f));

            string title;
            string desc;
            bool isEnding = res == StageResult.EndingA || res == StageResult.EndingB;
            if (res == StageResult.EndingA)
            {
                title = "★ KẾT CỤC A: LỜI NÓI DỐI VĨNH HẰNG ★";
                desc = "Kẻ Lang Thang buông gươm và gục ngã dưới nhát kiếm của Eosphoros. Lời nguyền thời gian tan biến; thế gian tiếp tục tôn thờ một lời nói dối hoàn hảo.";
            }
            else if (res == StageResult.EndingB)
            {
                title = "★ KẾT CỤC B: TỘI ĐỒ ẨN CƯ ★";
                desc = "Kẻ Lang Thang hạ sát Eosphoros trong hầm sâu để dập tắt lời nói dối. Vòng lặp thời gian khép lại, hắn mang danh phản đồ và lặng lẽ bước vào kiếp ẩn cư.";
            }
            else if (res == StageResult.Victory)
            {
                title = "★ CHIẾN THẮNG MÀN CHƠI (VICTORY) ★";
                desc = "Bạn đã vượt qua thử thách giải đố bàn cờ và hạ gục Boss trong màn Duel Phase 2! Ký ức vòng lặp sẽ tiếp tục theo bạn sang ải tiếp theo.";
            }
            else
            {
                title = "☠ THẤT BẠI — LỜI NGUYỀN THỜI GIAN KÍCH HOẠT ☠";
                desc = "Kẻ Lang Thang đã gục ngã, nhưng ký ức về tuyệt kỹ Boss và khắc chế vẫn còn nguyên. Hãy Tua Ngược Thời Gian (Rewind) hoặc thử lại!";
            }

            GUI.Label(new Rect(modal.x + 20, modal.y + 18, modal.width - 40, 28), title, _headerStyle);
            GUI.Label(new Rect(modal.x + 20, modal.y + 52, modal.width - 40, 82), desc, _boldLabelStyle);

            if (isEnding)
            {
                if (GUI.Button(new Rect(modal.x + 20, modal.y + 150, 240, 40), "🏠 VỀ MENU CHÍNH", _centerActiveBtnStyle))
                {
                    EnterMainMenu();
                }
                if (GUI.Button(new Rect(modal.x + 280, modal.y + 150, 240, 40), "↻ CHƠI LẠI TỪ ẢI 1", _centerBtnStyle))
                {
                    ResetRun();
                }
            }
            else
            {
                if (GUI.Button(new Rect(modal.x + 20, modal.y + 146, 160, 38), "↺ TUA NGƯỢC", _centerActiveBtnStyle))
                {
                    ExecuteRewind();
                }

                string nextStage = GetNextStageId(_currentStageId);
                if (GUI.Button(new Rect(modal.x + 192, modal.y + 146, 174, 38),
                    res == StageResult.Victory ? $"Sang {nextStage.ToUpperInvariant()} ▶" : "↻ Chơi lại màn này",
                    res == StageResult.Victory ? _centerActiveBtnStyle : _centerBtnStyle))
                {
                    LoadStage(res == StageResult.Victory ? nextStage : _currentStageId, playIntro: res == StageResult.Victory);
                }

                if (GUI.Button(new Rect(modal.x + 378, modal.y + 146, 142, 38), "🏠 Menu Chính", _centerBtnStyle))
                {
                    EnterMainMenu();
                }
            }

            GUI.Label(new Rect(modal.x + 20, modal.y + 202, modal.width - 40, 24),
                $"Ký ức vòng lặp hiện tại · Vòng lặp: {_session.Knowledge.Deaths}  |  Chiêu Boss đã biết: {_session.Knowledge.KnownBossSkillCount}",
                _mutedLabelStyle);
        }

        private static string GetNextStageId(string current)
        {
            switch (current)
            {
                case "stage-1": return "stage-2";
                case "stage-2": return "stage-3";
                case "stage-3": return "stage-4";
                case "stage-4": return "stage-5";
                default: return "stage-1";
            }
        }

        private void DrawSettingsModal()
        {
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0.01f, 0.01f, 0.03f, 0.62f));
            Rect modal = new Rect(UiW * 0.5f - 230f, UiH * 0.5f - 210f, 460f, 420f);
            GUI.Box(modal, GUIContent.none, _panelStyle);
            DrawRect(new Rect(modal.x, modal.y, modal.width, 3), new Color(0.90f, 0.76f, 0.35f, 0.9f));

            GUI.Label(new Rect(modal.x + 16, modal.y + 12, 340, 24), "⚙ CÀI ĐẶT (SETTINGS)", _headerStyle);
            if (GUI.Button(new Rect(modal.xMax - 80, modal.y + 10, 66, 24), "Đóng", _centerActiveBtnStyle))
            {
                _options.SaveToPrefs();
                _options.ApplyDisplaySettings();
                _showSettingsModal = false;
            }

            float y = modal.y + 48;
            GUI.Label(new Rect(modal.x + 18, y, 220, 20), $"Âm lượng tổng (Master): {Mathf.RoundToInt(_options.MasterVolume * 100)}%", _boldLabelStyle);
            _options.MasterVolume = GUI.HorizontalSlider(new Rect(modal.x + 245, y + 4, 190, 18), _options.MasterVolume, 0f, 1f);
            y += 30;

            GUI.Label(new Rect(modal.x + 18, y, 220, 20), $"Nhạc nền (BGM): {Mathf.RoundToInt(_options.BgmVolume * 100)}%", _boldLabelStyle);
            _options.BgmVolume = GUI.HorizontalSlider(new Rect(modal.x + 245, y + 4, 190, 18), _options.BgmVolume, 0f, 1f);
            y += 30;

            GUI.Label(new Rect(modal.x + 18, y, 220, 20), $"Hiệu ứng (SFX): {Mathf.RoundToInt(_options.SfxVolume * 100)}%", _boldLabelStyle);
            _options.SfxVolume = GUI.HorizontalSlider(new Rect(modal.x + 245, y + 4, 190, 18), _options.SfxVolume, 0f, 1f);
            y += 34;

            GUI.Label(new Rect(modal.x + 18, y, 180, 22), "Tốc độ hoạt ảnh:", _boldLabelStyle);
            float[] speeds = { 1.0f, 1.5f, 2.0f };
            for (int i = 0; i < speeds.Length; i++)
            {
                float sp = speeds[i];
                if (GUI.Button(new Rect(modal.x + 205 + i * 76, y - 2, 70, 24), $"x{sp:0.#}",
                    Mathf.Approximately(_options.AnimationSpeed, sp) ? _centerActiveBtnStyle : _centerBtnStyle))
                {
                    _options.AnimationSpeed = sp;
                }
            }
            y += 36;

            GUI.Label(new Rect(modal.x + 18, y, 160, 22), "Độ phân giải:", _boldLabelStyle);
            for (int i = 0; i < GameOptionsModel.SupportedResolutions.Length; i++)
            {
                var (rw, rh) = GameOptionsModel.SupportedResolutions[i];
                if (GUI.Button(new Rect(modal.x + 165 + i * 94, y - 2, 88, 24), $"{rw}×{rh}",
                    _options.ResolutionIndex == i ? _centerActiveBtnStyle : _centerBtnStyle))
                {
                    _options.ResolutionIndex = i;
                }
            }
            y += 34;

            _options.Fullscreen = GUI.Toggle(new Rect(modal.x + 18, y, 400, 22),
                _options.Fullscreen, "  Toàn màn hình (Fullscreen — áp dụng trong bản Build)");
            y += 28;

            _options.ShowGridOverlay = GUI.Toggle(new Rect(modal.x + 18, y, 400, 22),
                _options.ShowGridOverlay, "  Hiển thị lưới ô cờ chiến thuật (Grid Overlay)");
            y += 28;

            _options.ShowDangerZoneWarning = GUI.Toggle(new Rect(modal.x + 18, y, 400, 22),
                _options.ShowDangerZoneWarning, "  Hiển thị cảnh báo vùng nguy hiểm đỏ (Danger Tiles Warning)");
            y += 28;

            _options.ShowDevStageBar = GUI.Toggle(new Rect(modal.x + 18, y, 400, 22),
                _options.ShowDevStageBar, "  Hiện thanh nhảy nhanh Ải 1–5 trên Top Bar (Dev)");
            y += 36;

            ApplyAudioOptions();

            if (GUI.Button(new Rect(modal.x + 18, y, modal.width - 36, 34), "Lưu Cài Đặt & Áp Dụng", _centerActiveBtnStyle))
            {
                _options.SaveToPrefs();
                _options.ApplyDisplaySettings();
                _showSettingsModal = false;
                PlaySfx("ui_confirm");
            }
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            UnsubscribeAll();
        }
    }
}
