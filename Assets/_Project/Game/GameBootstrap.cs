using System;
using System.Collections.Generic;
using BTH.Content;
using BTH.Content.Dialogues;
using BTH.Content.Stages;
using BTH.Content.Terrain;
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

    /// <summary>
    /// Composition Root & Presentation Controller cho Behind the Hero.
    /// Kết nối GameSession với bàn cờ Tileset, hoạt ảnh Spritesheet, hiệu ứng VFX và giao diện HUD / Duel.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const float UiW = 1280f;
        private const float UiH = 720f;

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

        private Transform _boardRoot;
        private Transform _unitRoot;
        private readonly Dictionary<GridCoord, SpriteRenderer> _tileRenderers = new Dictionary<GridCoord, SpriteRenderer>();
        private readonly Dictionary<GridCoord, SpriteRenderer> _overlayRenderers = new Dictionary<GridCoord, SpriteRenderer>();
        private readonly Dictionary<UnitId, UnitSpriteAnimator> _unitAnimators = new Dictionary<UnitId, UnitSpriteAnimator>();
        private readonly List<IDisposable> _eventSubs = new List<IDisposable>();

        private HlinhBgmPlayer _bgm;
        private AudioSource _sfxSource;

        private string _currentStageId = "stage-1";
        private PlayerCommandMode _cmdMode = PlayerCommandMode.Move;
        private GridCoord _hoverCoord = GridCoord.None;
        private readonly HashSet<GridCoord> _validTargetCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _previewPathCells = new HashSet<GridCoord>();
        private readonly GameOptionsModel _options = new GameOptionsModel();
        private bool _showSettingsModal;

        private DialogueRunner _dialogueRunner;
        private bool _outroShownForStage;
        private string _combatBannerText = "Lượt của Kẻ Lang Thang — Chọn ô để Di chuyển hoặc Tấn công.";
        private float _transitionTimer;
        private string _lastDuelRoundNotice = string.Empty;
        private float _duelVfxTimer;
        private string _activeDuelVfxId = "swift-slash";
        private bool _duelVfxOnBoss = true;

        private Camera _mainCam;
        private Vector3 _camBasePos = new Vector3(0f, 0f, -10f);
        private float _shakeTimer;
        private float _shakeStrength;

        // Styles cache
        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _boldLabelStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _activeBtnStyle;
        private GUIStyle _badgeWinStyle;
        private GUIStyle _badgeDrawStyle;
        private GUIStyle _badgeLoseStyle;
        private bool _stylesInitialized;

        private static readonly string[] PlayerActiveSkills = { "swift-slash", "necrotic-burst", "temporal-step" };

        public GameSession Session => _session;
        public string CurrentStageId => _currentStageId;
        public DialogueRunner ActiveDialogue => _dialogueRunner;

        private void Awake()
        {
            _options.LoadFromPrefs();
            SetupCameraAndAudio();
            ApplyAudioOptions();
            _vfxManager = gameObject.AddComponent<VfxPopupManager>();
            _session = ContentCatalog.CreateSession();
            LoadStage("stage-1");
        }

        private void ApplyAudioOptions()
        {
            Time.timeScale = Mathf.Clamp(_options.AnimationSpeed, 0.5f, 2.5f);
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

        public void LoadStage(string stageId)
        {
            _currentStageId = stageId;
            _outroShownForStage = false;
            _transitionTimer = 0f;
            _lastDuelRoundNotice = string.Empty;

            UnsubscribeAll();
            _session.StartStage(stageId);
            _validator = new ActionValidator(_session.State.Pathfinder);

            SubscribeEvents();
            RebuildBoardAndUnits();
            RefreshValidTargets();
            UpdateTileOverlays();

            var introGraph = DialogueCatalog.GetIntro(stageId);
            if (introGraph != null)
            {
                _dialogueRunner = new DialogueRunner(introGraph, _session.Knowledge);
                _dialogueRunner.Start();
            }
            else
            {
                _dialogueRunner = null;
            }

            PlayBgm(AudioCatalog.StoryTheme);
            _combatBannerText = "Bắt đầu " + _session.State.Stage.DisplayName + " — Quan sát ô đỏ Telegraph của Boss!";
        }

        public void DismissAllDialogue()
        {
            int safety = 0;
            while (_dialogueRunner != null && !_dialogueRunner.IsFinished && safety++ < 20)
            {
                if (_dialogueRunner.Current.Choices.Count > 0)
                {
                    break;
                }
                _dialogueRunner.Advance();
            }
            CheckDialogueFinished();
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

                    var beforeTerrain = _session.State.Grid.InBounds(e.To) ? _session.State.Grid.Get(e.To).Terrain : TerrainId.Floor;
                    int hpBefore = u.Hp;
                    TerrainCatalog.HandleUnitEnteredCell(u, e.To, _session.State);
                    if (beforeTerrain == TerrainId.Chest && u.Hp >= hpBefore)
                    {
                        Vector3 w = CoordToWorld(e.To);
                        _vfxManager.SpawnSkillVfx("divine-barrier", w, 1.4f);
                        _vfxManager.SpawnPopup(w, $"RƯƠNG KHO BÁU: +{ChestTerrainBehaviour.HealAmount} HP!", new Color(0.35f, 1f, 0.55f), 1.5f);
                        PlaySfx("checkpoint");
                    }
                }
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
                if (_session.Skills.TryGet(e.SkillId, out var sk))
                {
                    _combatBannerText = $"Thi triển [{sk.DisplayName}]!";
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
                Color col = e.Target == _session.State.Units.Player?.Id
                    ? new Color(1f, 0.32f, 0.32f)
                    : new Color(1f, 0.86f, 0.25f);
                _vfxManager.SpawnPopup(pos, $"-{e.Amount} HP{tag}", col);
                TriggerCameraShake(0.14f, 0.18f);
                if (e.Amount > 0 && !e.Killed)
                {
                    PlaySfx(e.Target == _session.State.Units.Player?.Id ? "player_hurt" : "enemy_hurt");
                }
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
            }));

            _eventSubs.Add(ev.Subscribe<HazardTriggeredEvent>((in HazardTriggeredEvent e) =>
            {
                Vector3 w = CoordToWorld(e.Coord);
                _vfxManager.SpawnPopup(w, $"BẪY ĐỊA HÌNH -{e.Damage} HP", new Color(0.45f, 1f, 0.45f));
            }));

            _eventSubs.Add(ev.Subscribe<BossPhaseChangedEvent>((in BossPhaseChangedEvent e) =>
            {
                if (e.Phase == 2)
                {
                    _transitionTimer = 1.25f;
                    TriggerCameraShake(0.35f, 0.6f);
                    PlaySfx("boss_roar");
                    PlaySfx("transform_dark");
                    PlayBgm(_session.State.Stage.Duel?.MusicId ?? AudioCatalog.DuelTheme);
                    _combatBannerText = "BOSS DƯỚI 70% HP — CHUYỂN CẢNH PHASE 2: DUEL MODE!";
                }
            }));

            _eventSubs.Add(ev.Subscribe<ClashResolvedEvent>((in ClashResolvedEvent e) =>
            {
                _activeDuelVfxId = e.PlayerSkillId;
                _duelVfxOnBoss = e.Result != ClashResult.Lose;
                _duelVfxTimer = 0.65f;

                string outcomeVi = e.Result == ClashResult.Win
                    ? $"THẮNG CLASH ({e.PlayerPower} vs {e.BossPower}) -> KẾT LIỄU TỰ ĐỘNG!"
                    : (e.Result == ClashResult.Draw
                        ? $"HÒA CLASH ({e.PlayerPower} vs {e.BossPower}) -> MỖI BÊN MẤT 10% UY LỰC!"
                        : $"THUA CLASH ({e.PlayerPower} vs {e.BossPower}) -> NHẬN ĐÒN TUYỆT KỸ BOSS!");
                _lastDuelRoundNotice = outcomeVi;
                _combatBannerText = outcomeVi;
                PlaySfx(e.Result == ClashResult.Win ? "attack3" : "boss_attack");
            }));

            _eventSubs.Add(ev.Subscribe<RewindPerformedEvent>((in RewindPerformedEvent e) =>
            {
                RebuildBoardAndUnits();
                RefreshValidTargets();
                var player = _session.State.Units.Player;
                if (player != null)
                {
                    _vfxManager.SpawnSkillVfx("rewind", CoordToWorldForUnit(player), 1.8f);
                }
                PlaySfx("transform_dark");
                _combatBannerText = $"ĐÃ TUA NGƯỢC THỜI GIAN (REWIND) VỀ LƯỢT {e.TurnIndex}! Ký ức chiêu Boss vẫn được giữ.";
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
                        _dialogueRunner = new DialogueRunner(outro, _session.Knowledge);
                        _dialogueRunner.Start();
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

        private void Update()
        {
            _bgm?.Tick(Time.unscaledDeltaTime);
            if (_session?.State == null) return;

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                Vector2 offset = UnityEngine.Random.insideUnitCircle * _shakeStrength;
                _mainCam.transform.position = _camBasePos + new Vector3(offset.x, offset.y, 0f);
                if (_shakeTimer <= 0f)
                {
                    _mainCam.transform.position = _camBasePos;
                    _shakeStrength = 0f;
                }
            }

            if (_transitionTimer > 0f)
            {
                _transitionTimer -= Time.deltaTime;
            }

            if (_duelVfxTimer > 0f)
            {
                _duelVfxTimer -= Time.deltaTime;
            }

            bool inDuel = _session.State.Turn.Phase == TurnPhase.Duel;
            if (_boardRoot != null) _boardRoot.gameObject.SetActive(!inDuel);
            if (_unitRoot != null) _unitRoot.gameObject.SetActive(!inDuel);

            HandleKeyboardShortcuts();
            if (!inDuel)
            {
                if (_dialogueRunner == null && !_showSettingsModal)
                {
                    UpdateBoardHoverAndClick();
                }
                UpdateTileOverlays();
            }
        }

        private void HandleKeyboardShortcuts()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (_dialogueRunner != null && !_dialogueRunner.IsFinished)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                {
                    if (_dialogueRunner.Current.Choices.Count == 0)
                    {
                        _dialogueRunner.Advance();
                        CheckDialogueFinished();
                        PlaySfx("ui_confirm");
                    }
                }
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
            if (kb.xKey.wasPressedThisFrame) LoadStage(_currentStageId);
            if (kb.spaceKey.wasPressedThisFrame) ExecuteEndTurn();
        }

        private void SetCommandMode(PlayerCommandMode mode)
        {
            _cmdMode = mode;
            RefreshValidTargets();
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
                    _previewPathCells.Clear();
                    if (_cmdMode == PlayerCommandMode.Move && _validTargetCells.Contains(coord))
                    {
                        var path = TacticalNavigator.PreviewPlayerPath(_session.State, coord);
                        for (int i = 0; i < path.Count; i++)
                        {
                            _previewPathCells.Add(path[i]);
                        }
                    }
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
            }
            else
            {
                _combatBannerText = $"Không thể Guard: {res.Message}";
            }
        }

        private void ExecuteRewind()
        {
            if (_session.State.Snapshots.CanRewind)
            {
                _session.State.Snapshots.Rewind();
                var state = _session.State;
                PlayBgm(state.Turn.Phase == TurnPhase.Duel
                    ? (state.Stage.Duel?.MusicId ?? AudioCatalog.DuelTheme)
                    : AudioCatalog.StoryTheme);
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
            _session.EndTurn();
            _cmdMode = PlayerCommandMode.Move;
            RefreshValidTargets();
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
                bool isPath = _previewPathCells.Contains(coord);
                bool isValid = _validTargetCells.Contains(coord);
                bool isDanger = dangerSet.Contains(coord);

                if (isHover)
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
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized && _panelStyle?.normal.background != null) return;
            _stylesInitialized = true;

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = MakeStyleTex(new Color(0.08f, 0.09f, 0.13f, 0.94f));
            _panelStyle.normal.textColor = new Color(0.92f, 0.89f, 0.82f);
            _panelStyle.padding = new RectOffset(10, 10, 10, 10);

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

            _boldLabelStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 6, 6)
            };
            _btnStyle.normal.background = MakeStyleTex(new Color(0.16f, 0.21f, 0.30f, 0.95f));
            _btnStyle.hover.background = MakeStyleTex(new Color(0.24f, 0.32f, 0.46f, 0.98f));
            _btnStyle.normal.textColor = Color.white;
            _btnStyle.hover.textColor = new Color(0.90f, 0.76f, 0.35f);

            _activeBtnStyle = new GUIStyle(_btnStyle);
            _activeBtnStyle.normal.background = MakeStyleTex(new Color(0.42f, 0.23f, 0.65f, 0.98f));
            _activeBtnStyle.normal.textColor = new Color(1f, 0.92f, 0.55f);

            _badgeWinStyle = MakeBadgeStyle(new Color(0.14f, 0.56f, 0.26f, 0.96f));
            _badgeDrawStyle = MakeBadgeStyle(new Color(0.68f, 0.52f, 0.12f, 0.96f));
            _badgeLoseStyle = MakeBadgeStyle(new Color(0.65f, 0.15f, 0.15f, 0.96f));
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
            if (_session?.State == null) return;

            var prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / UiW, Screen.height / UiH, 1f));

            if (_session.State.Turn.Phase == TurnPhase.Duel)
            {
                DrawDuelScreen();
            }
            else
            {
                DrawBoardWorldLabels();
            }

            DrawTopBar();
            DrawLeftHudPanel();

            if (_transitionTimer > 0f)
            {
                DrawPhase2TransitionOverlay();
            }

            if (_dialogueRunner != null && !_dialogueRunner.IsFinished)
            {
                DrawDialogueOverlay();
            }
            else if (_session.State.IsOver)
            {
                DrawStageResultModal();
            }

            if (_showSettingsModal)
            {
                DrawSettingsModal();
            }

            GUI.matrix = prevMatrix;
        }

        private void DrawTopBar()
        {
            var state = _session.State;
            var boss = state.Units.Boss;

            GUI.Box(new Rect(280, 8, UiW - 288, 84), GUIContent.none, _panelStyle);

            GUI.Label(new Rect(292, 14, 100, 22), "MÀN CHƠI:", _headerStyle);
            string[] stages = { "stage-1", "stage-2", "stage-3", "stage-4", "stage-5" };
            for (int i = 0; i < stages.Length; i++)
            {
                string sid = stages[i];
                var st = _currentStageId == sid ? _activeBtnStyle : _btnStyle;
                if (GUI.Button(new Rect(395 + i * 68, 12, 64, 26), $"Ải {i + 1}", st))
                {
                    LoadStage(sid);
                }
            }

            float rightX = UiW - 294;
            if (GUI.Button(new Rect(rightX, 12, 86, 26), "↺ UNDO", _btnStyle))
            {
                ExecuteRewind();
            }
            if (GUI.Button(new Rect(rightX + 90, 12, 92, 26), "⟳ RESET[X]", _btnStyle))
            {
                LoadStage(_currentStageId);
            }
            if (GUI.Button(new Rect(rightX + 186, 12, 96, 26), "⚙ CÀI ĐẶT", _showSettingsModal ? _activeBtnStyle : _btnStyle))
            {
                _showSettingsModal = !_showSettingsModal;
            }

            if (boss != null)
            {
                var bossPortrait = ArtCatalog.GetPortrait(boss.Definition.PortraitId);
                GUI.DrawTexture(new Rect(292, 42, 42, 42), bossPortrait, ScaleMode.ScaleToFit);

                string phaseTag = state.Turn.Phase == TurnPhase.Duel ? " [PHASE 2: DUEL MODE]" : " [PHASE 1: BÀN CỜ]";
                GUI.Label(new Rect(342, 41, 350, 20), $"{boss.Definition.DisplayName}{phaseTag}", _headerStyle);

                Rect barBg = new Rect(342, 63, 330, 18);
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

            string puzzleDesc = StageCatalog.GetPuzzleDescription(_currentStageId);
            GUI.Label(new Rect(682, 40, UiW - 694, 48),
                $"★ {puzzleDesc}\n► {_combatBannerText}", _labelStyle);
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
            string phaseText = $"Active (Turn: {state.Turn.TurnIndex} / {state.Stage.TurnLimit})";
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
            GUI.Label(new Rect(20, 172, 240, 22), statusLine, _labelStyle);
            GUI.Label(new Rect(20, 192, 240, 20),
                $"Vòng lặp: {_session.Knowledge.Deaths}  |  Chiêu đã biết: {_session.Knowledge.SeenKeys.Count}", _labelStyle);

            GUI.Label(new Rect(20, 220, 240, 22), "ACTIONS:", _headerStyle);
            float by = 246f;
            float bh = 32f;
            float gap = 36f;

            if (GUI.Button(new Rect(20, by, 240, bh), "➡  Move                        [G]",
                _cmdMode == PlayerCommandMode.Move ? _activeBtnStyle : _btnStyle))
            {
                SetCommandMode(PlayerCommandMode.Move);
            }
            by += gap;

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

            if (GUI.Button(new Rect(20, by, 240, bh), "☣  Necrotic Burst              [2]",
                _cmdMode == PlayerCommandMode.Skill2 ? _activeBtnStyle : _btnStyle))
            {
                SetCommandMode(PlayerCommandMode.Skill2);
            }
            by += gap;

            if (GUI.Button(new Rect(20, by, 240, bh), "⌛  Temporal Step               [E]",
                _cmdMode == PlayerCommandMode.Skill3 ? _activeBtnStyle : _btnStyle))
            {
                SetCommandMode(PlayerCommandMode.Skill3);
            }
            by += gap;

            if (GUI.Button(new Rect(20, by, 240, bh), "🛡  Guard                       [D]", _btnStyle))
            {
                ExecuteGuard();
            }
            by += gap;

            if (GUI.Button(new Rect(20, by, 240, bh), "↺  Rewind                      [R]", _btnStyle))
            {
                ExecuteRewind();
            }
            by += gap;

            if (GUI.Button(new Rect(20, by, 240, bh), "⏭  End Turn                [Space]", _btnStyle))
            {
                ExecuteEndTurn();
            }
            by += gap + 6f;

            GUI.Label(new Rect(20, by, 240, 20), "THÔNG TIN Ô CỜ:", _headerStyle);
            by += 22f;
            if (_hoverCoord.IsValid && state.Grid.InBounds(_hoverCoord))
            {
                var cState = state.Grid.Get(_hoverCoord);
                var tSpec = TerrainCatalog.Get(cState.Terrain);
                string collapseInfo = cState.TurnsUntilCollapse > 0 ? $" [Sập sau {cState.TurnsUntilCollapse} lượt]" : "";
                GUI.Label(new Rect(20, by, 240, 85),
                    $"Ô ({_hoverCoord.X},{_hoverCoord.Y}): {tSpec.DisplayName}{collapseInfo}\n• {tSpec.TacticalDescription}",
                    _labelStyle);
            }
            else
            {
                GUI.Label(new Rect(20, by, 240, 85),
                    "Current Turn: Wanderer\n• Ô xanh lam: Vùng đi được (A*)\n• Ô đỏ nhấp nháy: Telegraph Boss",
                    _labelStyle);
            }
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
                        GUI.Box(new Rect(guiX - 32, guiY + 8, 64, 19), $"NỨT:{cell.TurnsUntilCollapse}T", _badgeDrawStyle);
                    }
                    else if (cell.Terrain == TerrainId.Chest)
                    {
                        GUI.Box(new Rect(guiX - 32, guiY + 8, 64, 19), "RƯƠNG", _badgeWinStyle);
                    }
                    else if (cell.IsHazard)
                    {
                        GUI.Box(new Rect(guiX - 30, guiY + 10, 60, 18), $"-{cell.HazardDamagePerTurn}HP", _badgeLoseStyle);
                    }
                }
            }

            foreach (var u in state.Units.All)
            {
                if (!_unitAnimators.TryGetValue(u.Id, out var anim) || anim == null) continue;
                Vector3 topWorld = anim.transform.position + new Vector3(0f, u.SizeY > 1 ? 1.35f : 0.75f, 0f);
                Vector3 sp = _mainCam.WorldToScreenPoint(topWorld);
                if (sp.z < 0) continue;
                float guiX = sp.x * sx;
                float guiY = (Screen.height - sp.y) * sy;

                Rect bar = new Rect(guiX - 36, guiY - 12, 72, 11);
                DrawRect(bar, new Color(0.1f, 0.1f, 0.12f, 0.9f));
                Color hpCol = u.Faction == UnitFaction.Player
                    ? new Color(0.25f, 0.85f, 0.38f, 1f)
                    : new Color(0.92f, 0.24f, 0.24f, 1f);
                DrawRect(new Rect(bar.x + 1, bar.y + 1, (bar.width - 2) * Mathf.Clamp01(u.HpRatio), bar.height - 2),
                    hpCol);
                GUI.Label(new Rect(guiX - 45, guiY - 28, 90, 18), $"{u.Hp}/{u.MaxHp}", _badgeDrawStyle);
            }

            var popups = _vfxManager.ActivePopups;
            for (int i = 0; i < popups.Count; i++)
            {
                var p = popups[i];
                Vector3 sp = _mainCam.WorldToScreenPoint(p.WorldPos);
                if (sp.z < 0) continue;
                float guiX = sp.x * sx;
                float guiY = (Screen.height - sp.y) * sy;
                var st = new GUIStyle(_boldLabelStyle) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
                st.normal.textColor = p.Color;
                GUI.Label(new Rect(guiX - 90, guiY - 12, 180, 26), p.Text, st);
            }
        }

        private void DrawDuelScreen()
        {
            var state = _session.State;
            var duel = state.Duel;
            var player = state.Units.Player;
            var boss = state.Units.Boss;
            if (duel == null || player == null || boss == null) return;

            Rect arena = new Rect(280, 96, UiW - 288, UiH - 104);
            var bgTex = ArtCatalog.GetBackground(duel.Definition.BackgroundId);
            GUI.DrawTexture(arena, bgTex, ScaleMode.StretchToFill);

            GUI.Box(new Rect(arena.x + 20, arena.y + 14, arena.width - 40, 74), GUIContent.none, _panelStyle);
            string bossSkillName = duel.State.BossSkillId;
            if (_session.Skills.TryGet(duel.State.BossSkillId, out var bSkill))
            {
                bossSkillName = bSkill.DisplayName;
            }
            GUI.Label(new Rect(arena.x + 36, arena.y + 20, arena.width - 70, 26),
                $"⚔ PHASE 2: DUEL MODE — VÒNG {duel.State.Round}  |  BOSS RA CHIÊU: [{bossSkillName.ToUpperInvariant()}] (UY LỰC: {duel.State.BossPower} | HỆ: {duel.State.BossElement.Value})",
                _headerStyle);
            GUI.Label(new Rect(arena.x + 36, arena.y + 46, arena.width - 70, 22),
                string.IsNullOrEmpty(_lastDuelRoundNotice)
                    ? "Chọn 1 trong 3 thẻ chiêu Clash bên dưới để đối đầu trực diện với tuyệt kỹ của Boss!"
                    : _lastDuelRoundNotice,
                _boldLabelStyle);

            int frameIdx = Mathf.FloorToInt(Time.time * 6f) % 4;
            UnitAnimState pAnimState = (_duelVfxTimer > 0f && _duelVfxOnBoss) ? UnitAnimState.Attack : UnitAnimState.Idle;
            UnitAnimState bAnimState = (_duelVfxTimer > 0f && !_duelVfxOnBoss) ? UnitAnimState.Attack : UnitAnimState.Idle;

            var pFrames = ArtCatalog.GetUnitAnimFrames(player.Definition.Id, player.IsAlive ? pAnimState : UnitAnimState.Death);
            var bFrames = ArtCatalog.GetUnitAnimFrames(boss.Definition.Id, boss.IsAlive ? bAnimState : UnitAnimState.Death);

            Rect pRect = new Rect(arena.x + 80, arena.y + 135, 190, 190);
            Rect bRect = new Rect(arena.xMax - 290, arena.y + 115, 220, 220);

            DrawSpriteOnGui(pRect, pFrames[frameIdx % pFrames.Length], flipX: false);
            DrawSpriteOnGui(bRect, bFrames[frameIdx % bFrames.Length], flipX: true);

            if (_duelVfxTimer > 0f)
            {
                var vfxFrames = ArtCatalog.GetVfxFrames(_activeDuelVfxId);
                int vIdx = Mathf.Clamp(Mathf.FloorToInt((1f - _duelVfxTimer / 0.65f) * vfxFrames.Length), 0, vfxFrames.Length - 1);
                Rect vRect = _duelVfxOnBoss ? bRect : pRect;
                DrawSpriteOnGui(vRect, vfxFrames[vIdx], flipX: false);
            }

            var options = duel.GetOptions();
            float cardW = (arena.width - 80f) / Mathf.Max(1, options.Count);
            float cardY = arena.yMax - 175f;

            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                Rect card = new Rect(arena.x + 20 + i * (cardW + 20f), cardY, cardW, 155f);
                GUI.Box(card, GUIContent.none, _panelStyle);

                GUI.Label(new Rect(card.x + 12, card.y + 10, card.width - 24, 24),
                    $"[{i + 1}] {opt.DisplayName}", _headerStyle);

                int basePower = opt.Prediction.PlayerPower - opt.Prediction.Bonus;
                GUI.Label(new Rect(card.x + 12, card.y + 36, card.width - 24, 40),
                    $"Uy lực gốc: {basePower}  +  Khắc chế: {opt.Prediction.Bonus}\n⇒ TỔNG UY LỰC: {opt.Prediction.PlayerPower}  (so với Boss {opt.Prediction.BossPower})",
                    _labelStyle);

                GUIStyle badgeSt = opt.Prediction.Result == ClashResult.Win
                    ? _badgeWinStyle
                    : (opt.Prediction.Result == ClashResult.Draw ? _badgeDrawStyle : _badgeLoseStyle);
                string badgeText = opt.Prediction.Result == ClashResult.Win
                    ? $"DỰ BÁO: THẮNG (+{duel.Definition.FinisherCount} ĐÒN KẾT LIỄU)"
                    : (opt.Prediction.Result == ClashResult.Draw
                        ? "DỰ BÁO: HÒA (MỖI BÊN -10%)"
                        : "DỰ BÁO: THUA (ĂN ĐÒN BOSS)");
                GUI.Box(new Rect(card.x + 12, card.y + 82, card.width - 24, 26), badgeText, badgeSt);

                if (GUI.Button(new Rect(card.x + 12, card.y + 114, card.width - 24, 30),
                    "⚡ TUNG CHIÊU ĐỐI KHÁNG", _activeBtnStyle))
                {
                    _session.TryExecute(ActionRequest.ClashSkill(player.Id, opt.SkillId));
                }
            }
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
            float alpha = Mathf.Clamp01(_transitionTimer);
            DrawRect(new Rect(0, 0, UiW, UiH), new Color(0.55f, 0.08f, 0.12f, alpha * 0.65f));
            var st = new GUIStyle(_headerStyle)
            {
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter
            };
            st.normal.textColor = new Color(1f, 0.88f, 0.42f, 1f);
            GUI.Label(new Rect(280, UiH * 0.4f, UiW - 280, 80),
                "⚔ BOSS DƯỚI 70% HP — PHASE 2: DUEL MODE! ⚔", st);
        }

        private void DrawDialogueOverlay()
        {
            var node = _dialogueRunner.Current;
            if (node == null) return;

            float boxW = Mathf.Min(820f, UiW - 320f);
            Rect box = new Rect(295, UiH - 220, boxW, 204);
            GUI.Box(box, GUIContent.none, _panelStyle);

            bool isNarration = node.Speaker == DialogueCatalog.Narrator;
            if (!isNarration)
            {
                Rect port = new Rect(box.x + 14, box.y + 14, 136, 136);
                DrawRect(port, new Color(0.90f, 0.76f, 0.35f, 1f));
                GUI.DrawTexture(new Rect(port.x + 3, port.y + 3, port.width - 6, port.height - 6),
                    ArtCatalog.GetPortrait(node.Speaker), ScaleMode.ScaleToFit);
            }

            float textX = box.x + (isNarration ? 18 : 165);
            float textW = box.xMax - textX - 16;
            string title = isNarration ? "DẪN TRUYỆN" : $"ĐỐI THOẠI — {node.Speaker.ToUpperInvariant()}";
            GUI.Label(new Rect(textX, box.y + 14, textW, 26), title, _headerStyle);

            var dlgTextStyle = new GUIStyle(_labelStyle) { fontSize = 14 };
            float textHeight = node.Choices.Count == 0 ? 104 : 74;
            GUI.Label(new Rect(textX, box.y + 44, textW, textHeight), node.Text, dlgTextStyle);

            if (node.Choices.Count > 0)
            {
                for (int i = 0; i < node.Choices.Count; i++)
                {
                    var ch = node.Choices[i];
                    if (GUI.Button(new Rect(box.x + 165, box.y + 120 + i * 36, box.width - 185, 32),
                        $"➤ {ch.Text}", _activeBtnStyle))
                    {
                        _dialogueRunner.Choose(i);
                        CheckDialogueFinished();
                        PlaySfx("ui_confirm");
                    }
                }
            }
            else
            {
                if (GUI.Button(new Rect(box.xMax - 210, box.yMax - 44, 190, 32),
                    "Tiếp tục [Space] ▶", _activeBtnStyle))
                {
                    _dialogueRunner.Advance();
                    CheckDialogueFinished();
                    PlaySfx("ui_confirm");
                }
            }
        }

        private void DrawStageResultModal()
        {
            var res = _session.State.Result;
            Rect modal = new Rect(UiW * 0.5f - 180, UiH * 0.5f - 110, 480, 220);
            GUI.Box(modal, GUIContent.none, _panelStyle);

            string title;
            string desc;
            if (res == StageResult.Victory || res == StageResult.EndingA || res == StageResult.EndingB)
            {
                title = res == StageResult.EndingA
                    ? "★ KẾT CỤC A: LỜI NÓI DỐI VĨNH HẰNG ★"
                    : (res == StageResult.EndingB
                        ? "★ KẾT CỤC B: TỘI ĐỒ ẨN CƯ ★"
                        : "★ CHIẾN THẮNG MÀN CHƠI (VICTORY) ★");
                desc = res == StageResult.EndingA
                    ? "Kẻ Lang Thang buông gươm và chết dưới nhát kiếm của Eosphoros. Lời nguyền thời gian tan biến."
                    : "Bạn đã vượt qua thử thách giải đố bàn cờ và hạ gục Boss trong màn Duel Phase 2!";
            }
            else
            {
                title = "☠ THẤT BẠI — LỜI NGUYỀN THỜI GIAN KÍCH HOẠT ☠";
                desc = "Kẻ Lang Thang đã gục ngã, nhưng ký ức về tuyệt kỹ của Boss vẫn còn nguyên. Hãy Tua Ngược Thời Gian (Rewind)!";
            }

            GUI.Label(new Rect(modal.x + 20, modal.y + 18, modal.width - 40, 28), title, _headerStyle);
            GUI.Label(new Rect(modal.x + 20, modal.y + 54, modal.width - 40, 65), desc, _boldLabelStyle);

            if (GUI.Button(new Rect(modal.x + 20, modal.y + 130, 210, 36), "↺ TUA NGƯỢC (REWIND)", _activeBtnStyle))
            {
                ExecuteRewind();
            }

            string nextStage = GetNextStageId(_currentStageId);
            if (GUI.Button(new Rect(modal.x + 250, modal.y + 130, 210, 36),
                res == StageResult.Victory ? $"Sang {nextStage.ToUpperInvariant()} ▶" : "⟳ Chơi lại màn này", _btnStyle))
            {
                LoadStage(res == StageResult.Victory ? nextStage : _currentStageId);
            }
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
            Rect modal = new Rect(UiW * 0.5f - 180, UiH * 0.5f - 155, 420, 310);
            GUI.Box(modal, GUIContent.none, _panelStyle);

            GUI.Label(new Rect(modal.x + 16, modal.y + 12, 320, 24), "⚙ CÀI ĐẶT (SETTINGS)", _headerStyle);
            if (GUI.Button(new Rect(modal.xMax - 80, modal.y + 10, 66, 24), "Đóng", _activeBtnStyle))
            {
                _options.SaveToPrefs();
                _showSettingsModal = false;
            }

            float y = modal.y + 48;
            GUI.Label(new Rect(modal.x + 18, y, 220, 20), $"Âm lượng tổng (Master): {Mathf.RoundToInt(_options.MasterVolume * 100)}%", _boldLabelStyle);
            _options.MasterVolume = GUI.HorizontalSlider(new Rect(modal.x + 240, y + 4, 150, 18), _options.MasterVolume, 0f, 1f);
            y += 32;

            GUI.Label(new Rect(modal.x + 18, y, 220, 20), $"Nhạc nền (BGM): {Mathf.RoundToInt(_options.BgmVolume * 100)}%", _boldLabelStyle);
            _options.BgmVolume = GUI.HorizontalSlider(new Rect(modal.x + 240, y + 4, 150, 18), _options.BgmVolume, 0f, 1f);
            y += 32;

            GUI.Label(new Rect(modal.x + 18, y, 220, 20), $"Hiệu ứng (SFX): {Mathf.RoundToInt(_options.SfxVolume * 100)}%", _boldLabelStyle);
            _options.SfxVolume = GUI.HorizontalSlider(new Rect(modal.x + 240, y + 4, 150, 18), _options.SfxVolume, 0f, 1f);
            y += 36;

            GUI.Label(new Rect(modal.x + 18, y, 180, 22), "Tốc độ hoạt ảnh (Speed):", _boldLabelStyle);
            float[] speeds = { 1.0f, 1.5f, 2.0f };
            for (int i = 0; i < speeds.Length; i++)
            {
                float sp = speeds[i];
                if (GUI.Button(new Rect(modal.x + 205 + i * 62, y - 2, 58, 24), $"x{sp:0.#}",
                    Mathf.Approximately(_options.AnimationSpeed, sp) ? _activeBtnStyle : _btnStyle))
                {
                    _options.AnimationSpeed = sp;
                }
            }
            y += 36;

            _options.ShowGridOverlay = GUI.Toggle(new Rect(modal.x + 18, y, 360, 22),
                _options.ShowGridOverlay, "  Hiển thị lưới ô cờ chiến thuật (Grid Overlay)");
            y += 28;

            _options.ShowDangerZoneWarning = GUI.Toggle(new Rect(modal.x + 18, y, 360, 22),
                _options.ShowDangerZoneWarning, "  Hiển thị cảnh báo vùng nguy hiểm đỏ (Danger Tiles Warning)");
            y += 36;

            ApplyAudioOptions();

            if (GUI.Button(new Rect(modal.x + 18, y, modal.width - 36, 30), "Lưu Cài Đặt & Áp Dụng", _activeBtnStyle))
            {
                _options.SaveToPrefs();
                _showSettingsModal = false;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }
    }
}
