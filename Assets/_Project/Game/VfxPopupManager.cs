using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BTH.Game
{
    public sealed class FloatingPopup
    {
        public Vector3 WorldPos;
        public string Text;
        public Color Color;
        public float Elapsed;
        public float Duration;
    }

    /// <summary>
    /// Quản lý hiệu ứng kỹ năng VFX (cắt từ 4 sheet VFX) và popup số sát thương / trạng thái trên bàn cờ.
    /// </summary>
    public sealed class VfxPopupManager : MonoBehaviour
    {
        private readonly List<FloatingPopup> _popups = new List<FloatingPopup>();

        public IReadOnlyList<FloatingPopup> ActivePopups => _popups;

        public void SpawnSkillVfx(string skillId, Vector3 worldPos, float scale = 1.4f, float fps = 12f)
        {
            var frames = ArtCatalog.GetVfxFrames(skillId);
            if (frames == null || frames.Length == 0) return;

            var go = new GameObject("VFX_" + skillId);
            go.transform.SetParent(transform, false);
            go.transform.position = worldPos + new Vector3(0f, 0.15f, -0.5f);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            sr.sprite = frames[0];

            StartCoroutine(PlayVfxCoroutine(go, sr, frames, fps));
        }

        private static IEnumerator PlayVfxCoroutine(GameObject go, SpriteRenderer sr, Sprite[] frames, float fps)
        {
            float wait = 1f / Mathf.Max(4f, fps);
            for (int i = 0; i < frames.Length; i++)
            {
                if (sr != null)
                {
                    sr.sprite = frames[i];
                }
                yield return new WaitForSeconds(wait);
            }
            if (go != null)
            {
                Destroy(go);
            }
        }

        public void SpawnPopup(Vector3 worldPos, string text, Color color, float duration = 1.15f)
        {
            _popups.Add(new FloatingPopup
            {
                WorldPos = worldPos + new Vector3(Random.Range(-0.15f, 0.15f), 0.55f, 0f),
                Text = text,
                Color = color,
                Elapsed = 0f,
                Duration = duration
            });
        }

        private void Update()
        {
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                var p = _popups[i];
                p.Elapsed += Time.deltaTime;
                p.WorldPos += new Vector3(0f, 0.55f * Time.deltaTime, 0f);
                if (p.Elapsed >= p.Duration)
                {
                    _popups.RemoveAt(i);
                }
            }
        }
    }
}
