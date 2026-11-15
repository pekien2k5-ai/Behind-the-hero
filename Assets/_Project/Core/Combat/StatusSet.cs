using System;
using System.Collections.Generic;
using BTH.Core.Run;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Tập trạng thái trên một unit. Quản lý thời gian theo lượt của chủ thể:
    /// status gắn giữa lượt không bị trừ ngay ở EndTurn đầu tiên.
    /// </summary>
    public sealed class StatusSet
    {
        private readonly List<StatusEntry> _entries = new List<StatusEntry>();

        public int Count => _entries.Count;

        public IEnumerable<IStatusEffect> Effects
        {
            get
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    yield return _entries[i].Effect;
                }
            }
        }

        public bool Has(StatusId id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Effect.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Has<T>() where T : class, IStatusEffect
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Effect is T)
                {
                    return true;
                }
            }

            return false;
        }

        public IStatusEffect Get(StatusId id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Effect.Id == id)
                {
                    return _entries[i].Effect;
                }
            }

            return null;
        }

        public int GetRemaining(StatusId id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Effect.Id == id)
                {
                    return _entries[i].Remaining;
                }
            }

            return 0;
        }

        /// <summary>Gắn status; nếu đã có cùng Id thì làm mới thời gian với effect mới.</summary>
        public void Add(IStatusEffect effect, StatusContext ctx)
        {
            if (effect == null || effect.Duration <= 0)
            {
                return;
            }

            int existing = IndexOf(effect.Id);
            if (existing >= 0)
            {
                _entries.RemoveAt(existing);
            }

            _entries.Add(new StatusEntry(effect, effect.Duration, false));
            effect.OnApply(ctx);
            ctx.Events?.Publish(new StatusAppliedEvent(ctx.Owner.Id, effect.Id, effect.Duration));
        }

        public bool Remove(StatusId id)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                return false;
            }

            var entry = _entries[index];
            _entries.RemoveAt(index);
            var ctx = new StatusContext(null, null, null);
            entry.Effect.OnRemoved(ctx);
            return true;
        }

        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>Bắt đầu lượt của chủ thể: mở khoá đếm lượt cho các status vừa gắn.</summary>
        public void TickTurnStart(StatusContext ctx)
        {
            var snapshot = _entries.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].CanTick = true;
                snapshot[i].Effect.OnTurnStart(ctx);
            }
        }

        /// <summary>EndTurn của chủ thể: hiệu ứng cuối lượt, rồi giảm duration và hết hạn.</summary>
        public void TickTurnEnd(StatusContext ctx)
        {
            var snapshot = _entries.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                var entry = snapshot[i];
                if (!entry.CanTick)
                {
                    continue;
                }

                if (InventoryContains(entry) == false)
                {
                    continue;
                }

                entry.Effect.OnTurnEnd(ctx);

                entry.Remaining--;
                if (entry.Remaining > 0)
                {
                    continue;
                }

                RemoveAt(entry);

                if (ctx.Events != null)
                {
                    ctx.Events.Publish(new StatusExpiredEvent(ctx.Owner.Id, entry.Effect.Id));
                }
            }
        }

        public void NotifyDamaged(StatusContext ctx, DamageResult result)
        {
            var snapshot = _entries.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                var entry = snapshot[i];
                if (ContextContains(entry))
                {
                    entry.Effect.OnDamaged(ctx, result);
                }
            }
        }

        public float DamageTakenMultiplier
        {
            get
            {
                float multiplier = 1f;
                for (int i = 0; i < _entries.Count; i++)
                {
                    multiplier *= _entries[i].Effect.DamageTakenMultiplier;
                }

                return multiplier;
            }
        }

        public bool PreventsAction
        {
            get
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].Effect.PreventsAction)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool PreventsMovement
        {
            get
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].Effect.PreventsMovement)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsStaggered => Has(StatusIds.Stagger);

        public bool IsRooted => Has(StatusIds.Root);

        public bool IsGuarding => Has(StatusIds.Guard);

        public bool IsFlanked => Has(StatusIds.Flanked);

        internal StatusEntry[] Capture()
        {
            return _entries.ToArray();
        }

        internal void Restore(StatusEntry[] entries)
        {
            _entries.Clear();
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                _entries.Add(entries[i]);
            }
        }

        private bool InventoryContains(StatusEntry entry)
        {
            return _entries.IndexOf(entry) >= 0;
        }

        private bool ContextContains(StatusEntry entry)
        {
            return _entries.IndexOf(entry) >= 0;
        }

        private int IndexOf(StatusId id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Effect.Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RemoveAt(StatusEntry entry)
        {
            int index = _entries.IndexOf(entry);
            if (index < 0)
            {
                return;
            }

            _entries.RemoveAt(index);
            entry.Effect.OnRemoved(new StatusContext(null, null, null));
        }
    }
}
