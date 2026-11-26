using System;
using System.Collections.Generic;

namespace BTH.Core.Run
{
    /// <summary>Bộ nhớ run: không nằm trong snapshot, nên rewind không xoá nó.</summary>
    public sealed class RunKnowledge : IRunKnowledge
    {
        public const string DeathsKey = "deaths";
        public const string BossSkillSeenPrefix = "boss-skill:";
        private const string ClashWinPrefix = "clash-win:";

        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _counters = new Dictionary<string, int>(StringComparer.Ordinal);
        private IEventBus _events;

        public RunKnowledge(IEventBus events = null)
        {
            _events = events;
        }

        public int Deaths => GetCounter(DeathsKey);

        public IReadOnlyDictionary<string, int> Counters => _counters;

        public IReadOnlyCollection<string> SeenKeys => _seen;

        public int KnownBossSkillCount { get; private set; }

        internal void AttachEventBus(IEventBus events)
        {
            _events = events;
        }

        public bool HasSeenBossSkill(string skillId)
        {
            return !string.IsNullOrEmpty(skillId) && HasSeen(BossSkillSeenPrefix + skillId);
        }

        public bool HasWonClash(string bossSkillId, string playerSkillId)
        {
            return HasSeen(ClashWinKey(bossSkillId, playerSkillId));
        }

        public void RecordClashWin(string bossSkillId, string playerSkillId)
        {
            if (!string.IsNullOrEmpty(bossSkillId) && !string.IsNullOrEmpty(playerSkillId))
            {
                MarkSeen(ClashWinKey(bossSkillId, playerSkillId));
            }
        }

        public bool HasSeen(string key)
        {
            return !string.IsNullOrEmpty(key) && _seen.Contains(key);
        }

        public void MarkSeen(string key)
        {
            if (string.IsNullOrEmpty(key) || !_seen.Add(key))
            {
                return;
            }

            if (key.Length > BossSkillSeenPrefix.Length && key.StartsWith(BossSkillSeenPrefix, StringComparison.Ordinal))
            {
                KnownBossSkillCount++;
            }

            Publish(key, 1);
        }

        public int GetCounter(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return 0;
            }

            return _counters.TryGetValue(key, out int value) ? value : 0;
        }

        public void Increment(string key, int amount = 1)
        {
            if (string.IsNullOrEmpty(key) || amount == 0)
            {
                return;
            }

            int next = GetCounter(key) + amount;
            _counters[key] = next;
            Publish(key, next);
        }

        public void RegisterDeath()
        {
            Increment(DeathsKey);
        }

        private static string ClashWinKey(string bossSkillId, string playerSkillId)
        {
            return ClashWinPrefix + bossSkillId + "|" + playerSkillId;
        }

        private void Publish(string key, int value)
        {
            _events?.Publish(new RunKnowledgeUpdatedEvent(key, value));
        }
    }
}
