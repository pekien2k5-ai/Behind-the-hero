using System;
using System.Collections.Generic;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Chạy effect theo priority, dừng nếu nguồn chết giữa chừng để tránh
    /// hiệu ứng "ma" sau khi unit đã chết.
    /// </summary>
    public sealed class EffectQueue
    {
        private readonly List<IGameEffect> _effects = new List<IGameEffect>();

        public int Count => _effects.Count;

        /// <summary>
        /// Các effect đang chờ, chỉ đọc. Content dùng để xem trước effect của một chiêu (AI/telegraph)
        /// mà không chạy chúng trên state thật.
        /// </summary>
        public IReadOnlyList<IGameEffect> Items => _effects;

        public void Enqueue(IGameEffect effect)
        {
            if (effect != null)
            {
                _effects.Add(effect);
            }
        }

        public void Run(EffectContext ctx)
        {
            if (ctx == null)
            {
                return;
            }

            var ordered = StableOrder();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ctx.Source.IsValid && ctx.Units.TryGet(ctx.Source, out var source) && !source.IsAlive)
                {
                    break;
                }

                ordered[i].Resolve(ctx);
            }

            _effects.Clear();
        }

        public void Clear()
        {
            _effects.Clear();
        }

        private List<IGameEffect> StableOrder()
        {
            var indexed = new List<KeyValuePair<int, IGameEffect>>(_effects.Count);
            for (int i = 0; i < _effects.Count; i++)
            {
                indexed.Add(new KeyValuePair<int, IGameEffect>(i, _effects[i]));
            }

            indexed.Sort((a, b) =>
            {
                int byPriority = a.Value.Priority.CompareTo(b.Value.Priority);
                return byPriority != 0 ? byPriority : a.Key.CompareTo(b.Key);
            });

            var result = new List<IGameEffect>(indexed.Count);
            for (int i = 0; i < indexed.Count; i++)
            {
                result.Add(indexed[i].Value);
            }

            return result;
        }
    }
}
