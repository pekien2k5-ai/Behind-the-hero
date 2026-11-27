using System;
using BTH.Content.Dialogues;
using BTH.Content.Units;
using BTH.Core.Run;
using UnityEngine;

namespace BTH.Game
{
    /// <summary>
    /// Điều khiển hoạt ảnh sân khấu cắt cảnh (Cutscene) cho phần dẫn chuyện mở đầu,
    /// chuyển ải và kết thúc game (Ending A / Ending B) với spritesheet 4 trạng thái & VFX.
    /// </summary>
    public sealed class CutsceneController
    {
        private const float UiW = 1280f;
        private const float UiH = 720f;
        private const float TypewriterCharsPerSec = 52f;

        private struct ActorRig
        {
            public string UnitId;
            public string DisplayName;
            public UnitAnimState BaseState;
            public UnitAnimState CurrentState;
            public float TargetX;
            public float CurrentX;
            public float Scale;
            public bool FlipX;
            public bool Visible;
            public float ActionTimer;
            public float ActionDuration;
            public bool HoldDeathFrame;
        }

        private string _stageId = "stage-1";
        private bool _isOutro;
        private string _lastNodeId = string.Empty;
        private string _backgroundId = "bg_oakhaven_ruins";
        private string _chapterBanner = string.Empty;

        private ActorRig _left;
        private ActorRig _right;

        private float _sceneElapsed;
        private float _nodeElapsed;
        private int _revealedChars;
        private bool _lineComplete;

        private string _vfxSkillId = string.Empty;
        private float _vfxTimer;
        private float _vfxDuration = 0.7f;
        private bool _vfxOnLeft;
        private bool _vfxCenter;

        public bool IsLineComplete => _lineComplete;
        public string ActiveNodeId => _lastNodeId;
        public string LeftActorId => _left.Visible ? _left.UnitId : string.Empty;
        public string RightActorId => _right.Visible ? _right.UnitId : string.Empty;
        public UnitAnimState LeftAnimState => _left.CurrentState;
        public UnitAnimState RightAnimState => _right.CurrentState;

        public void Begin(string stageId, bool isOutro, DialogueNode initialNode, Action<string> playSfx, Action<float, float> triggerShake)
        {
            _stageId = string.IsNullOrEmpty(stageId) ? "stage-1" : stageId;
            _isOutro = isOutro;
            _sceneElapsed = 0f;
            _lastNodeId = string.Empty;
            _backgroundId = ResolveBackground(_stageId);
            _chapterBanner = ResolveChapterBanner(_stageId, _isOutro);

            _left = default;
            _right = default;

            if (initialNode != null)
            {
                EnterNode(initialNode, playSfx, triggerShake);
            }
        }

        public void Tick(float unscaledDt, DialogueNode currentNode, Action<string> playSfx, Action<float, float> triggerShake)
        {
            if (currentNode == null) return;

            if (!string.Equals(_lastNodeId, currentNode.Id, StringComparison.Ordinal))
            {
                EnterNode(currentNode, playSfx, triggerShake);
            }

            float dt = Mathf.Min(unscaledDt, 0.05f);
            _sceneElapsed += dt;
            _nodeElapsed += dt;

            string fullText = currentNode.Text ?? string.Empty;
            if (!_lineComplete)
            {
                _revealedChars = Mathf.Min(fullText.Length, Mathf.FloorToInt(_nodeElapsed * TypewriterCharsPerSec) + 1);
                if (_revealedChars >= fullText.Length)
                {
                    _revealedChars = fullText.Length;
                    _lineComplete = true;
                }
            }

            TickActor(ref _left, dt);
            TickActor(ref _right, dt);

            if (_vfxTimer > 0f)
            {
                _vfxTimer = Mathf.Max(0f, _vfxTimer - dt);
            }
        }

        public void CompleteCurrentLine(DialogueNode currentNode)
        {
            if (currentNode == null) return;
            _revealedChars = (currentNode.Text ?? string.Empty).Length;
            _lineComplete = true;
        }

        public string GetVisibleText(DialogueNode currentNode)
        {
            string fullText = currentNode?.Text ?? string.Empty;
            if (_lineComplete || _revealedChars >= fullText.Length)
            {
                return fullText;
            }
            return fullText.Substring(0, Mathf.Clamp(_revealedChars, 0, fullText.Length));
        }

        private void EnterNode(DialogueNode node, Action<string> playSfx, Action<float, float> triggerShake)
        {
            _lastNodeId = node.Id ?? string.Empty;
            _nodeElapsed = 0f;
            _revealedChars = 0;
            _lineComplete = string.IsNullOrEmpty(node.Text);
            _vfxTimer = 0f;

            ConfigureStagingForNode(node, playSfx, triggerShake);
        }

        private void ConfigureStagingForNode(DialogueNode node, Action<string> playSfx, Action<float, float> triggerShake)
        {
            string id = node.Id ?? string.Empty;
            string stageBossId = ResolveStageBossId(_stageId);

            // Mặc định: Kẻ Lang Thang bên trái, Boss màn hiện tại bên phải.
            string desiredLeft = UnitCatalog.WandererId;
            string desiredRight = stageBossId;
            bool leftVisible = true;
            bool rightVisible = true;
            UnitAnimState leftState = UnitAnimState.Idle;
            UnitAnimState rightState = UnitAnimState.Idle;
            float leftTargetX = 250f;
            float rightTargetX = 810f;
            bool leftFlip = false;
            bool rightFlip = true;
            float leftScale = 1f;
            float rightScale = stageBossId == UnitCatalog.DemonKingMalkorId ? 1.28f : 1f;
            bool leftHoldDeath = false;
            bool rightHoldDeath = false;

            // --- Dàn dựng hoạt ảnh chi tiết ĐẦU GAME (Ải 1: Oakhaven & Eosphoros cứu Wanderer) ---
            if (_stageId == "stage-1" && !_isOutro)
            {
                if (id == "s1_story_1" || id == "s1_story_2")
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    rightVisible = false;
                    leftTargetX = 480f;
                    leftState = UnitAnimState.Walk;
                }
                else if (id == "s1_story_3" || id == "s1_story_4")
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    rightVisible = false;
                    leftTargetX = 360f;
                    leftState = UnitAnimState.Death;
                    leftHoldDeath = true;
                    TriggerVfx("rewind", onLeft: true, center: false, 0.9f);
                    playSfx?.Invoke("transform_dark");
                }
                else if (id == "s1_story_5")
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    rightVisible = true;
                    leftTargetX = 300f;
                    rightTargetX = 760f;
                    leftState = UnitAnimState.Death;
                    leftHoldDeath = true;
                    TriggerVfx("divine-barrier", onLeft: false, center: false, 0.85f);
                    playSfx?.Invoke("transform_light");
                }
                else if (id == "s1_story_6")
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    rightVisible = true;
                    leftTargetX = 320f;
                    rightTargetX = 730f;
                    leftState = UnitAnimState.Idle;
                    TriggerVfx("holy-pillar", onLeft: true, center: false, 0.85f);
                    playSfx?.Invoke("checkpoint");
                }
                else if (id == "s1_1")
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    rightVisible = true;
                    leftTargetX = 320f;
                    rightTargetX = 720f;
                    rightState = UnitAnimState.Attack;
                    TriggerVfx("divine-barrier", onLeft: false, center: false, 0.7f);
                    playSfx?.Invoke("transform_light");
                }
                else if (id == "s1_1b")
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    rightVisible = true;
                    leftTargetX = 360f;
                    rightTargetX = 730f;
                    leftState = UnitAnimState.Attack;
                    playSfx?.Invoke("attack1");
                }
                else if (id == "s1_1c")
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    rightVisible = true;
                    leftTargetX = 340f;
                    rightTargetX = 720f;
                }
                else if (id == "s1_2")
                {
                    desiredRight = UnitCatalog.AshenExecutionerId;
                    rightVisible = true;
                    leftTargetX = 270f;
                    rightTargetX = 810f;
                }
                else if (id == "s1_3")
                {
                    desiredRight = UnitCatalog.AshenExecutionerId;
                    rightVisible = true;
                    leftTargetX = 270f;
                    rightTargetX = 760f;
                    rightState = UnitAnimState.Attack;
                    TriggerVfx("earth-crack", onLeft: false, center: true, 0.8f);
                    playSfx?.Invoke("boss_roar");
                    triggerShake?.Invoke(0.25f, 0.35f);
                }
            }
            // --- Ải 2 & Ải 3: Hành trình đồng hành cùng Eosphoros rồi đối diện Boss ---
            else if ((_stageId == "stage-2" || _stageId == "stage-3") && !_isOutro)
            {
                if (id.StartsWith("s2_story", StringComparison.Ordinal) || id.StartsWith("s3_story", StringComparison.Ordinal))
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    rightVisible = true;
                    rightScale = 1f;
                    leftTargetX = 310f;
                    rightTargetX = 740f;
                    if (id == "s3_story_1" || id == "s3_story_2")
                    {
                        TriggerVfx("rewind", onLeft: true, center: false, 0.75f);
                    }
                }
                else if (id == "s2_1")
                {
                    rightState = UnitAnimState.Attack;
                    TriggerVfx("plague-storm", onLeft: false, center: true, 0.8f);
                    playSfx?.Invoke("boss_attack");
                }
                else if (id == "s3_1")
                {
                    rightState = UnitAnimState.Attack;
                    TriggerVfx("void-spear-beam", onLeft: false, center: true, 0.8f);
                    playSfx?.Invoke("boss_attack");
                }
            }
            // --- Ải 4: Quỷ Vương Malkor Intro & Outro ---
            else if (_stageId == "stage-4" && !_isOutro)
            {
                if (id == "s4_story_1")
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    rightScale = 1f;
                    leftTargetX = 320f;
                    rightTargetX = 760f;
                    leftState = UnitAnimState.Walk;
                }
                else if (id == "s4_1")
                {
                    rightState = UnitAnimState.Attack;
                    TriggerVfx("hellfire-greatsword", onLeft: false, center: true, 0.85f);
                    playSfx?.Invoke("boss_roar");
                    triggerShake?.Invoke(0.3f, 0.4f);
                }
            }
            else if (_stageId == "stage-4" && _isOutro)
            {
                desiredRight = UnitCatalog.EosphorosId;
                rightScale = 1f;
                rightState = UnitAnimState.Death;
                rightHoldDeath = true;
                leftTargetX = 360f;
                leftState = UnitAnimState.Attack;
                TriggerVfx("swift-slash", onLeft: false, center: true, 0.8f);
                playSfx?.Invoke("attack3");
            }
            // --- Outro các ải 1-3 (Đối thoại Wanderer <-> Eosphoros) ---
            else if ((_stageId == "stage-1" || _stageId == "stage-2" || _stageId == "stage-3") && _isOutro)
            {
                desiredRight = UnitCatalog.EosphorosId;
                rightVisible = true;
                rightScale = 1f;
                leftTargetX = 310f;
                rightTargetX = 740f;
                if (id == "s1_end_1")
                {
                    rightState = UnitAnimState.Attack;
                    TriggerVfx("divine-barrier", onLeft: false, center: false, 0.7f);
                    playSfx?.Invoke("checkpoint");
                }
                else if (id == "s2_end_3" || id == "s3_end_1")
                {
                    TriggerVfx("rewind", onLeft: true, center: false, 0.75f);
                }
                else if (id == "s3_end_3")
                {
                    rightTargetX = 790f;
                    TriggerVfx("holy-light-slash", onLeft: false, center: false, 0.8f);
                    playSfx?.Invoke("transform_dark");
                }
            }
            // --- Ải 5 Intro: Khúc khải hoàn & cuộc đối chất dưới Mật thất ---
            else if (_stageId == "stage-5" && !_isOutro)
            {
                desiredRight = UnitCatalog.FallenEosphorosId;
                rightScale = 1.05f;
                if (id == "s5_story_1" || id == "s5_story_2")
                {
                    desiredRight = UnitCatalog.EosphorosId;
                    leftTargetX = 280f;
                    rightTargetX = 780f;
                }
                else if (id == "s5_story_3")
                {
                    desiredRight = UnitCatalog.FallenEosphorosId;
                    leftTargetX = 300f;
                    rightTargetX = 760f;
                    TriggerVfx("necrotic-burst", onLeft: false, center: false, 0.8f);
                    playSfx?.Invoke("transform_dark");
                }
                else if (id == "s5_2b" || id == "s5_4")
                {
                    leftTargetX = 300f;
                    rightTargetX = 720f;
                    rightState = UnitAnimState.Attack;
                    TriggerVfx("holy-light-slash", onLeft: false, center: true, 0.85f);
                    playSfx?.Invoke("boss_roar");
                    triggerShake?.Invoke(0.28f, 0.35f);
                }
            }
            // --- CUỐI GAME (Ải 5 Outro: Lựa chọn & hoạt ảnh Ending A / Ending B) ---
            else if (_stageId == "stage-5" && _isOutro)
            {
                desiredRight = UnitCatalog.FallenEosphorosId;
                rightScale = 1.05f;
                if (id == "s5_end_1")
                {
                    leftTargetX = 320f;
                    rightTargetX = 740f;
                }
                else if (id == "ending_a_1")
                {
                    leftTargetX = 380f;
                    rightTargetX = 620f;
                    rightState = UnitAnimState.Attack;
                    TriggerVfx("holy-light-slash", onLeft: true, center: false, 0.9f);
                    playSfx?.Invoke("boss_attack");
                    triggerShake?.Invoke(0.32f, 0.4f);
                }
                else if (id == "ending_a_2")
                {
                    leftTargetX = 350f;
                    rightTargetX = 720f;
                    leftState = UnitAnimState.Death;
                    leftHoldDeath = true;
                    TriggerVfx("rewind", onLeft: true, center: false, 0.95f);
                    playSfx?.Invoke("player_death");
                }
                else if (id == "ending_a_3")
                {
                    leftTargetX = 340f;
                    rightTargetX = 990f;
                    rightFlip = false;
                    leftState = UnitAnimState.Death;
                    leftHoldDeath = true;
                    rightState = UnitAnimState.Walk;
                    TriggerVfx("holy-pillar", onLeft: false, center: false, 0.9f);
                    playSfx?.Invoke("transform_light");
                }
                else if (id == "ending_b_1")
                {
                    leftTargetX = 430f;
                    rightTargetX = 630f;
                    leftState = UnitAnimState.Attack;
                    rightState = UnitAnimState.Attack;
                    TriggerVfx("swift-slash", onLeft: false, center: true, 0.85f);
                    playSfx?.Invoke("attack3");
                    triggerShake?.Invoke(0.35f, 0.45f);
                }
                else if (id == "ending_b_2")
                {
                    leftTargetX = 390f;
                    rightTargetX = 730f;
                    rightState = UnitAnimState.Death;
                    rightHoldDeath = true;
                    TriggerVfx("necrotic-burst", onLeft: false, center: false, 0.9f);
                    playSfx?.Invoke("enemy_death");
                }
                else if (id == "ending_b_3")
                {
                    leftTargetX = 290f;
                    rightTargetX = 760f;
                    leftFlip = true;
                    rightState = UnitAnimState.Death;
                    rightHoldDeath = true;
                }
                else if (id == "ending_b_monologue_1")
                {
                    leftTargetX = 460f;
                    leftFlip = true;
                    rightVisible = false;
                    TriggerVfx("rewind", onLeft: true, center: false, 0.8f);
                }
                else if (id == "ending_b_monologue_2")
                {
                    leftTargetX = 180f;
                    leftFlip = true;
                    leftState = UnitAnimState.Walk;
                    rightVisible = false;
                }
            }
            // --- Outro các ải 1-3 ---
            else if (_isOutro)
            {
                rightState = UnitAnimState.Death;
                rightHoldDeath = true;
                leftTargetX = 360f;
                rightTargetX = 790f;
                playSfx?.Invoke("checkpoint");
            }

            ApplyActorTarget(ref _left, desiredLeft, leftVisible, leftTargetX, spawnX: -90f, leftState, leftScale, leftFlip, leftHoldDeath);
            ApplyActorTarget(ref _right, desiredRight, rightVisible, rightTargetX, spawnX: UiW - 140f, rightState, rightScale, rightFlip, rightHoldDeath);
        }

        private static void ApplyActorTarget(ref ActorRig rig, string unitId, bool visible, float targetX, float spawnX,
            UnitAnimState desiredState, float scale, bool flipX, bool holdDeath)
        {
            bool changedActor = !string.Equals(rig.UnitId, unitId, StringComparison.Ordinal) || (!rig.Visible && visible);
            rig.UnitId = unitId;
            rig.DisplayName = ResolveActorName(unitId);
            rig.Visible = visible;
            rig.Scale = scale;
            rig.FlipX = flipX;
            rig.TargetX = targetX;
            rig.HoldDeathFrame = holdDeath;
            rig.BaseState = desiredState == UnitAnimState.Attack ? UnitAnimState.Idle : desiredState;

            if (changedActor)
            {
                rig.CurrentX = spawnX;
            }

            if (desiredState == UnitAnimState.Attack)
            {
                rig.CurrentState = UnitAnimState.Attack;
                rig.ActionTimer = 0.55f;
                rig.ActionDuration = 0.55f;
            }
            else if (desiredState == UnitAnimState.Death)
            {
                rig.CurrentState = UnitAnimState.Death;
                rig.ActionTimer = 0f;
            }
            else if (Mathf.Abs(rig.CurrentX - targetX) > 12f)
            {
                rig.CurrentState = UnitAnimState.Walk;
                rig.ActionTimer = 0f;
            }
            else
            {
                rig.CurrentState = desiredState;
                rig.ActionTimer = 0f;
            }
        }

        private static void TickActor(ref ActorRig rig, float dt)
        {
            if (!rig.Visible) return;

            if (Mathf.Abs(rig.CurrentX - rig.TargetX) > 2f)
            {
                float speed = 560f;
                rig.CurrentX = Mathf.MoveTowards(rig.CurrentX, rig.TargetX, speed * dt);
                if (rig.ActionTimer <= 0f && rig.BaseState != UnitAnimState.Death)
                {
                    rig.CurrentState = UnitAnimState.Walk;
                }
            }
            else
            {
                rig.CurrentX = rig.TargetX;
                if (rig.ActionTimer <= 0f)
                {
                    rig.CurrentState = rig.BaseState;
                }
            }

            if (rig.ActionTimer > 0f)
            {
                rig.ActionTimer -= dt;
                if (rig.ActionTimer <= 0f)
                {
                    rig.CurrentState = rig.BaseState;
                }
            }
        }

        private void TriggerVfx(string skillId, bool onLeft, bool center, float duration)
        {
            _vfxSkillId = skillId;
            _vfxOnLeft = onLeft;
            _vfxCenter = center;
            _vfxDuration = Mathf.Max(0.25f, duration);
            _vfxTimer = _vfxDuration;
        }

        public void Draw(
            DialogueRunner runner,
            GUIStyle panelStyle,
            GUIStyle headerStyle,
            GUIStyle dialogueTextStyle,
            GUIStyle boldLabelStyle,
            GUIStyle btnStyle,
            GUIStyle activeBtnStyle,
            GUIStyle bannerCenterStyle,
            Action<Rect, Color> drawRect,
            Action<Rect, Sprite, bool> drawSprite,
            Action onAdvance,
            Action<int> onChoose,
            Action onSkip,
            Action onPause)
        {
            if (runner == null || runner.IsFinished || runner.Current == null) return;
            var node = runner.Current;

            // 1. Nền 16:9 của địa điểm + hiệu ứng chuyển động nhẹ
            var bgTex = ArtCatalog.GetBackground(_backgroundId);
            float pan = Mathf.Sin(_sceneElapsed * 0.35f) * 14f;
            GUI.DrawTexture(new Rect(-18f + pan, -8f, UiW + 36f, UiH + 16f), bgTex, ScaleMode.StretchToFill);

            // Phủ tối khí quyển Dark Fantasy + thanh letterbox điện ảnh
            drawRect(new Rect(0, 0, UiW, UiH), new Color(0.03f, 0.03f, 0.06f, 0.42f));
            drawRect(new Rect(0, 0, UiW, 56), new Color(0.02f, 0.02f, 0.04f, 0.92f));
            drawRect(new Rect(0, UiH - 224, UiW, 224), new Color(0.02f, 0.02f, 0.04f, 0.92f));

            // Hạt tro tàn / bụi thời gian trôi nhẹ trên sân khấu
            DrawAmbientParticles(drawRect);

            // Thanh tiêu đề Chương & nút Skip / Pause
            GUI.Label(new Rect(24, 14, 780, 28), _chapterBanner, headerStyle);
            bool hasChoices = node.Choices.Count > 0;
            if (!hasChoices)
            {
                if (GUI.Button(new Rect(UiW - 252, 12, 132, 30), "⏩ Bỏ qua", btnStyle))
                {
                    onSkip?.Invoke();
                    return;
                }
            }
            if (GUI.Button(new Rect(UiW - 112, 12, 96, 30), "⏸ [ESC]", btnStyle))
            {
                onPause?.Invoke();
                return;
            }

            // Sàn đá sân khấu nơi nhân vật đứng đối thoại
            drawRect(new Rect(70, 446, UiW - 140, 4), new Color(0.90f, 0.76f, 0.35f, 0.32f));

            // 2. Vẽ 2 nhân vật chuyển động trên sân khấu
            bool isNarration = string.Equals(node.Speaker, DialogueCatalog.Narrator, StringComparison.Ordinal);
            bool leftSpeaking = !isNarration && _left.Visible && string.Equals(node.Speaker, _left.UnitId, StringComparison.Ordinal);
            bool rightSpeaking = !isNarration && _right.Visible && string.Equals(node.Speaker, _right.UnitId, StringComparison.Ordinal);

            DrawActor(ref _left, leftSpeaking, isNarration, drawRect, drawSprite, bannerCenterStyle);
            DrawActor(ref _right, rightSpeaking, isNarration, drawRect, drawSprite, bannerCenterStyle);

            // 3. Hiệu ứng VFX minh họa hành động trong cắt cảnh
            if (_vfxTimer > 0f && !string.IsNullOrEmpty(_vfxSkillId))
            {
                var vfxFrames = ArtCatalog.GetVfxFrames(_vfxSkillId);
                if (vfxFrames != null && vfxFrames.Length > 0)
                {
                    float progress = 1f - (_vfxTimer / Mathf.Max(0.01f, _vfxDuration));
                    int idx = Mathf.Clamp(Mathf.FloorToInt(progress * vfxFrames.Length), 0, vfxFrames.Length - 1);
                    float vx = _vfxCenter ? (UiW * 0.5f - 115f) : (_vfxOnLeft ? _left.CurrentX : _right.CurrentX);
                    Rect vRect = new Rect(vx, 210f, 230f, 230f);
                    drawSprite(vRect, vfxFrames[idx], false);
                }
            }

            // 4. Hộp hội thoại chân dung phía dưới
            Rect box = new Rect(64, UiH - 212, UiW - 128, 196);
            GUI.Box(box, GUIContent.none, panelStyle);
            drawRect(new Rect(box.x, box.y, box.width, 3), new Color(0.90f, 0.76f, 0.35f, 0.85f));

            if (!isNarration)
            {
                Rect port = new Rect(box.x + 18, box.y + 22, 152, 152);
                drawRect(port, new Color(0.90f, 0.76f, 0.35f, 0.95f));
                drawRect(new Rect(port.x + 3, port.y + 3, port.width - 6, port.height - 6), new Color(0.07f, 0.08f, 0.12f, 1f));
                GUI.DrawTexture(new Rect(port.x + 4, port.y + 4, port.width - 8, port.height - 8),
                    ArtCatalog.GetPortrait(node.Speaker), ScaleMode.ScaleToFit);
            }

            float textX = box.x + (isNarration ? 26f : 188f);
            float textW = box.xMax - textX - 24f;
            string speakerTitle = isNarration
                ? "✦ DẪN TRUYỆN"
                : $"✦ {ResolveActorName(node.Speaker).ToUpperInvariant()}";
            GUI.Label(new Rect(textX, box.y + 14, textW, 28), speakerTitle, headerStyle);

            float textH = hasChoices ? 72f : 106f;
            GUI.Label(new Rect(textX, box.y + 46, textW, textH), GetVisibleText(node), dialogueTextStyle);

            if (hasChoices)
            {
                for (int i = 0; i < node.Choices.Count; i++)
                {
                    var ch = node.Choices[i];
                    if (GUI.Button(new Rect(textX, box.y + 114 + i * 38, textW - 8, 33),
                        $"➤ {ch.Text}", activeBtnStyle))
                    {
                        onChoose?.Invoke(i);
                    }
                }
            }
            else
            {
                string nextLabel = _lineComplete ? "Tiếp tục [Space] ▶" : "Hiện hết lời thoại [Space] ⏭";
                if (GUI.Button(new Rect(box.xMax - 248, box.yMax - 44, 228, 32), nextLabel, activeBtnStyle))
                {
                    onAdvance?.Invoke();
                }

                GUI.Label(new Rect(textX, box.yMax - 36, 460, 22),
                    "Nhấn [Space] / [Enter] hoặc bấm hộp thoại để chuyển lời",
                    boldLabelStyle);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    new Rect(box.x, box.y, box.width - 252, box.height).Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    onAdvance?.Invoke();
                    return;
                }
            }

            if (_sceneElapsed < 0.35f)
            {
                float fadeAlpha = 1f - (_sceneElapsed / 0.35f);
                drawRect(new Rect(0, 0, UiW, UiH), new Color(0.01f, 0.01f, 0.02f, fadeAlpha));
            }
        }

        private void DrawActor(ref ActorRig rig, bool isSpeaking, bool isNarration,
            Action<Rect, Color> drawRect, Action<Rect, Sprite, bool> drawSprite, GUIStyle nameStyle)
        {
            if (!rig.Visible || string.IsNullOrEmpty(rig.UnitId)) return;

            var frames = ArtCatalog.GetUnitAnimFrames(rig.UnitId, rig.CurrentState);
            if (frames == null || frames.Length == 0) return;

            int frameIdx;
            if (rig.CurrentState == UnitAnimState.Death)
            {
                frameIdx = rig.HoldDeathFrame
                    ? frames.Length - 1
                    : Mathf.Clamp(Mathf.FloorToInt(_nodeElapsed * 7f), 0, frames.Length - 1);
            }
            else if (rig.CurrentState == UnitAnimState.Attack)
            {
                float p = 1f - Mathf.Clamp01(rig.ActionTimer / Mathf.Max(0.01f, rig.ActionDuration));
                frameIdx = Mathf.Clamp(Mathf.FloorToInt(p * frames.Length), 0, frames.Length - 1);
            }
            else
            {
                float fps = rig.CurrentState == UnitAnimState.Walk ? 10.5f : 5.5f;
                frameIdx = Mathf.FloorToInt(_sceneElapsed * fps) % frames.Length;
            }

            float size = 232f * rig.Scale;
            float speakShiftX = isSpeaking ? (rig.CurrentX < UiW * 0.5f ? 18f : -18f) : 0f;
            float bobY = isSpeaking
                ? Mathf.Abs(Mathf.Sin(_sceneElapsed * 7.5f)) * -7f
                : Mathf.Sin(_sceneElapsed * 2.5f) * -2f;

            float x = rig.CurrentX + speakShiftX;
            float y = 452f - size + bobY;

            // Vòng sáng sân khấu dưới chân người đang nói
            if (isSpeaking)
            {
                float pulse = 0.45f + 0.25f * Mathf.Sin(_sceneElapsed * 6f);
                drawRect(new Rect(x + 24f, 438f, size - 48f, 14f), new Color(0.92f, 0.78f, 0.36f, pulse));
            }
            else
            {
                drawRect(new Rect(x + 36f, 442f, size - 72f, 8f), new Color(0.02f, 0.02f, 0.04f, 0.65f));
            }

            Color prevColor = GUI.color;
            if (!isNarration && !isSpeaking)
            {
                GUI.color = new Color(0.58f, 0.60f, 0.68f, 0.92f);
            }
            drawSprite(new Rect(x, y, size, size), frames[frameIdx], rig.FlipX);
            GUI.color = prevColor;

            // Bảng tên nhân vật phía trên đầu khi đang nói
            if (isSpeaking)
            {
                Rect tag = new Rect(x + size * 0.5f - 110f, y - 28f, 220f, 24f);
                drawRect(tag, new Color(0.10f, 0.11f, 0.17f, 0.88f));
                drawRect(new Rect(tag.x, tag.yMax - 2f, tag.width, 2f), new Color(0.90f, 0.76f, 0.35f, 0.95f));
                GUI.Label(tag, $"💬 {rig.DisplayName}", nameStyle);
            }
        }

        private void DrawAmbientParticles(Action<Rect, Color> drawRect)
        {
            for (int i = 0; i < 14; i++)
            {
                float seed = i * 97.3f;
                float px = ((seed * 13.7f + _sceneElapsed * (18f + (i % 4) * 7f)) % (UiW + 40f)) - 20f;
                float py = 460f - ((seed * 7.1f + _sceneElapsed * (24f + (i % 3) * 11f)) % 410f);
                float sz = 3f + (i % 3) * 1.5f;
                Color col = (i % 2 == 0)
                    ? new Color(0.90f, 0.76f, 0.35f, 0.40f)
                    : new Color(0.55f, 0.38f, 0.85f, 0.42f);
                drawRect(new Rect(px, py, sz, sz), col);
            }
        }

        private static string ResolveBackground(string stageId)
        {
            switch (stageId)
            {
                case "stage-2": return "bg_catacomb_toxic";
                case "stage-3": return "bg_void_arena";
                case "stage-4": return "bg_fire_arena";
                case "stage-5": return "bg_mausoleum";
                default: return "bg_oakhaven_ruins";
            }
        }

        private static string ResolveStageBossId(string stageId)
        {
            switch (stageId)
            {
                case "stage-2": return UnitCatalog.PlagueWitchId;
                case "stage-3": return UnitCatalog.AbyssKnightId;
                case "stage-4": return UnitCatalog.DemonKingMalkorId;
                case "stage-5": return UnitCatalog.FallenEosphorosId;
                default: return UnitCatalog.AshenExecutionerId;
            }
        }

        private static string ResolveChapterBanner(string stageId, bool isOutro)
        {
            string phase = isOutro ? "HỒI KẾT" : "MỞ MÀN";
            switch (stageId)
            {
                case "stage-1": return $"✦ HỒI I · {phase}: TÀN TÍCH OAKHAVEN & LỜI NGUYỀN THỜI GIAN";
                case "stage-2": return $"✦ HỒI II · {phase}: HẦM MỘ ĐỘC KHÍ — RỪNG BỆNH DỊCH";
                case "stage-3": return $"✦ HỒI II · {phase}: HÀNH LANG HƯ KHÔNG & MẦM MỐNG ĐỐ KỴ";
                case "stage-4": return $"✦ HỒI II · {phase}: VƯƠNG TỌA QUỶ VƯƠNG MALKOR";
                case "stage-5": return isOutro
                    ? "✦ HỒI KẾT · MẬT THẤT THÁNH ĐIỆN — LỰA CHỌN ĐỊNH MỆNH"
                    : "✦ HỒI III · KHÚC KHẢI HOÀN ĐẪM MÁU & MẬT THẤT THÁNH ĐIỆN";
                default: return "✦ BEHIND THE HERO";
            }
        }

        private static string ResolveActorName(string unitId)
        {
            switch (unitId)
            {
                case UnitCatalog.WandererId: return "Kẻ Lang Thang";
                case UnitCatalog.EosphorosId: return "Dũng Giả Eosphoros";
                case UnitCatalog.AshenExecutionerId: return "Đao Phủ Tro Tàn";
                case UnitCatalog.PlagueWitchId: return "Vu Nữ Morbida";
                case UnitCatalog.AbyssKnightId: return "Kỵ Sĩ Hư Không";
                case UnitCatalog.DemonKingMalkorId: return "Quỷ Vương Malkor";
                case UnitCatalog.FallenEosphorosId: return "Eosphoros Sa Ngã";
                default: return unitId;
            }
        }
    }
}
