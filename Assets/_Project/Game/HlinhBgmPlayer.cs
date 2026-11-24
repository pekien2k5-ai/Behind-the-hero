using UnityEngine;

namespace BTH.Game
{
    /// <summary>CV4 (hlinh): chuyển nhạc giữa phân cảnh và Duel.</summary>
    internal sealed class HlinhBgmPlayer
    {
        private const float FadeSeconds = 1.25f;

        private readonly AudioSource[] _sources = new AudioSource[2];
        private readonly float[] _gains = new float[2];
        private readonly float[] _startGains = new float[2];
        private int _target = -1;
        private float _elapsed = FadeSeconds;
        private float _volume;

        public HlinhBgmPlayer(GameObject owner)
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                var source = owner.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
                source.volume = 0f;
                _sources[i] = source;
            }
        }

        public void Play(string id, float pitch)
        {
            var clip = Resources.Load<AudioClip>("Audio/BGM/" + id);
            if (clip == null || (_target >= 0 && _sources[_target].clip == clip))
            {
                return;
            }

            int incoming = _target < 0 ? 0 : 1 - _target;
            var source = _sources[incoming];
            source.pitch = pitch;
            if (source.clip != clip || !source.isPlaying)
            {
                _gains[incoming] = 0f;
                source.volume = 0f;
                source.clip = clip;
                source.Play();
            }

            if (_target < 0)
            {
                _target = incoming;
                _gains[incoming] = 1f;
                ApplyVolumes();
                return;
            }

            _target = incoming;
            _startGains[0] = _gains[0];
            _startGains[1] = _gains[1];
            _elapsed = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_elapsed >= FadeSeconds) return;

            _elapsed = Mathf.Min(_elapsed + deltaTime, FadeSeconds);
            float t = _elapsed / FadeSeconds;
            for (int i = 0; i < _sources.Length; i++)
            {
                _gains[i] = Mathf.SmoothStep(_startGains[i], i == _target ? 1f : 0f, t);
            }

            ApplyVolumes();
            if (_elapsed >= FadeSeconds)
            {
                _sources[1 - _target].Stop();
            }
        }

        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                _sources[i].volume = _gains[i] * _volume;
            }
        }
    }
}
