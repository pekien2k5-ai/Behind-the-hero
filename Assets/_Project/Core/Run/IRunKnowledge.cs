using System.Collections.Generic;

namespace BTH.Core.Run
{
    /// <summary>
    /// Hiểu biết của người chơi tích luỹ trong run: chiêu boss đã thấy, số lần chết.
    /// Sống qua các ải trong cùng run và ngoài snapshot.
    /// </summary>
    public interface IRunKnowledge
    {
        int Deaths { get; }

        IReadOnlyDictionary<string, int> Counters { get; }

        IReadOnlyCollection<string> SeenKeys { get; }

        int KnownBossSkillCount { get; }

        bool HasSeenBossSkill(string skillId);

        bool HasWonClash(string bossSkillId, string playerSkillId);

        void RecordClashWin(string bossSkillId, string playerSkillId);

        bool HasSeen(string key);

        void MarkSeen(string key);

        int GetCounter(string key);

        void Increment(string key, int amount = 1);

        void RegisterDeath();
    }
}
