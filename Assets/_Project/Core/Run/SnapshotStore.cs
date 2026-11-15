using System;
using System.Collections.Generic;

namespace BTH.Core.Run
{
    /// <summary>Ring buffer N ảnh gần nhất; ảnh cũ bị đẩy ra khi đầy.</summary>
    public sealed class SnapshotStore : ISnapshotStore
    {
        private readonly List<GameSnapshot> _items;

        public SnapshotStore(int capacity = 8)
        {
            Capacity = capacity < 1 ? 1 : capacity;
            _items = new List<GameSnapshot>(Capacity);
        }

        public int Capacity { get; }

        public int Count => _items.Count;

        public IReadOnlyList<GameSnapshot> All => _items;

        public void Push(GameSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (_items.Count >= Capacity)
            {
                _items.RemoveAt(0);
            }

            _items.Add(snapshot);
        }

        public bool TryGetLatest(out GameSnapshot snapshot)
        {
            if (_items.Count == 0)
            {
                snapshot = null;
                return false;
            }

            snapshot = _items[_items.Count - 1];
            return true;
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
