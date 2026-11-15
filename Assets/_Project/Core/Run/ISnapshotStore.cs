using System.Collections.Generic;

namespace BTH.Core.Run
{
    /// <summary>Kho ảnh chụp gần nhất trong RAM (ring buffer). Không ghi đĩa.</summary>
    public interface ISnapshotStore
    {
        int Capacity { get; }

        int Count { get; }

        void Push(GameSnapshot snapshot);

        bool TryGetLatest(out GameSnapshot snapshot);

        IReadOnlyList<GameSnapshot> All { get; }

        void Clear();
    }
}
