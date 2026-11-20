using UnityEngine;

namespace BTH.Game
{
    /// <summary>
    /// Bộ điều khiển chuỗi Spritesheets chuyển động 4 trạng thái (Idle, Walk, Attack, Death)
    /// và hiệu ứng lướt/nhấp nháy khi nhận sát thương.
    /// </summary>
    public sealed class UnitSpriteAnimator : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private string _unitId = "wanderer";
        private UnitAnimState _state = UnitAnimState.Idle;
        private Sprite[] _frames;
        private int _frameIndex;
        private float _frameTimer;
        private float _fps = 6f;
        private bool _loop = true;

        private Vector3 _moveTarget;
        private bool _isMoving;
        private float _moveSpeed = 8.5f;
        private float _hitFlashTimer;
        private Vector3 _baseScale = Vector3.one;

        public UnitAnimState CurrentState => _state;

        public void Initialize(string unitId, SpriteRenderer renderer, Vector3 worldPos, Vector3 scale, bool flipX)
        {
            _unitId = unitId;
            _renderer = renderer;
            _baseScale = scale;
            transform.position = worldPos;
            transform.localScale = scale;
            _moveTarget = worldPos;
            if (_renderer != null)
            {
                _renderer.flipX = flipX;
            }
            PlayState(UnitAnimState.Idle);
        }

        public void PlayState(UnitAnimState newState)
        {
            _state = newState;
            _frames = ArtCatalog.GetUnitAnimFrames(_unitId, newState);
            _frameIndex = 0;
            _frameTimer = 0f;

            switch (newState)
            {
                case UnitAnimState.Idle:
                    _fps = 5.5f;
                    _loop = true;
                    break;
                case UnitAnimState.Walk:
                    _fps = 11f;
                    _loop = true;
                    break;
                case UnitAnimState.Attack:
                    _fps = 12f;
                    _loop = false;
                    break;
                case UnitAnimState.Death:
                    _fps = 7.5f;
                    _loop = false;
                    break;
            }

            if (_renderer != null && _frames != null && _frames.Length > 0)
            {
                _renderer.sprite = _frames[0];
            }
        }

        public void MoveToWorld(Vector3 targetWorldPos)
        {
            _moveTarget = targetWorldPos;
            if (Vector3.Distance(transform.position, targetWorldPos) > 0.05f)
            {
                if (_renderer != null && Mathf.Abs(targetWorldPos.x - transform.position.x) > 0.05f)
                {
                    _renderer.flipX = targetWorldPos.x < transform.position.x;
                }
                _isMoving = true;
                if (_state != UnitAnimState.Death)
                {
                    PlayState(UnitAnimState.Walk);
                }
            }
            else
            {
                transform.position = targetWorldPos;
            }
        }

        public void TriggerAttack(Vector3 targetWorldPos)
        {
            if (_state == UnitAnimState.Death) return;
            if (_renderer != null && Mathf.Abs(targetWorldPos.x - transform.position.x) > 0.05f)
            {
                _renderer.flipX = targetWorldPos.x < transform.position.x;
            }
            PlayState(UnitAnimState.Attack);
        }

        public void TriggerHitFlash()
        {
            _hitFlashTimer = 0.22f;
        }

        public void TriggerDeath()
        {
            _isMoving = false;
            PlayState(UnitAnimState.Death);
        }

        private void Update()
        {
            if (_isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, _moveTarget, _moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, _moveTarget) <= 0.01f)
                {
                    transform.position = _moveTarget;
                    _isMoving = false;
                    if (_state == UnitAnimState.Walk)
                    {
                        PlayState(UnitAnimState.Idle);
                    }
                }
            }

            if (_hitFlashTimer > 0f)
            {
                _hitFlashTimer -= Time.deltaTime;
                if (_renderer != null)
                {
                    _renderer.color = _hitFlashTimer > 0f ? new Color(1f, 0.38f, 0.38f, 1f) : Color.white;
                }
            }

            if (_frames == null || _frames.Length == 0 || _renderer == null) return;

            _frameTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, _fps);
            if (_frameTimer >= interval)
            {
                _frameTimer -= interval;
                if (_frameIndex + 1 < _frames.Length)
                {
                    _frameIndex++;
                    _renderer.sprite = _frames[_frameIndex];
                }
                else if (_loop)
                {
                    _frameIndex = 0;
                    _renderer.sprite = _frames[0];
                }
                else if (_state == UnitAnimState.Attack)
                {
                    PlayState(_isMoving ? UnitAnimState.Walk : UnitAnimState.Idle);
                }
            }
        }
    }
}
