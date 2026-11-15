using System.Collections.Generic;

namespace BTH.Core.Run
{
    /// <summary>
    /// Hiểu biết của người chơi tích luỹ trong run: chiêu boss đã thấy, số lần chết.
    /// Sống ngoài snapshot nên giữ nguyên khi rewind (đây là "trí nhớ" của người chơi).
    /// </summary>
    public interface IRunKnowledge
    {
        int Deaths { get; }

        IReadOnlyDictionary<string, int> Counters { get; }

        IReadOnlyCollection<string> SeenKeys { get; }

        bool HasSeen(string key);

        void MarkSeen(string key);

        int GetCounter(string key);

        void Increment(string key, int amount = 1);

        void RegisterDeath();
    }
}
