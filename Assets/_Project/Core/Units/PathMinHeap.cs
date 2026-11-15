using System;

namespace BTH.Core.Units
{
    /// <summary>Min-heap nhị phân cho A*/Dijkstra, không cấp phát Generic heap.</summary>
    internal sealed class PathMinHeap
    {
        private PathNode[] _items;

        public PathMinHeap(int capacity = 64)
        {
            _items = new PathNode[Math.Max(4, capacity)];
        }

        public int Count { get; private set; }

        public bool IsEmpty => Count == 0;

        public void Push(in PathNode node)
        {
            if (Count == _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }

            _items[Count] = node;
            int child = Count++;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (!_items[child].Before(in _items[parent]))
                {
                    break;
                }

                var swap = _items[child];
                _items[child] = _items[parent];
                _items[parent] = swap;
                child = parent;
            }
        }

        public PathNode Pop()
        {
            var root = _items[0];
            Count--;
            if (Count > 0)
            {
                _items[0] = _items[Count];
                int parent = 0;
                while (true)
                {
                    int left = parent * 2 + 1;
                    if (left >= Count)
                    {
                        break;
                    }

                    int right = left + 1;
                    int best = left;
                    if (right < Count && _items[right].Before(in _items[left]))
                    {
                        best = right;
                    }

                    if (!_items[best].Before(in _items[parent]))
                    {
                        break;
                    }

                    var swap = _items[best];
                    _items[best] = _items[parent];
                    _items[parent] = swap;
                    parent = best;
                }
            }

            return root;
        }
    }
}
