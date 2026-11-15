using System;
using System.Collections.Generic;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Ai
{
    /// <summary>
    /// Telegraph mặc định: một ý định / unit, DangerCells hợp nhất và sort (Y rồi X)
    /// để UI và test đều tất định.
    /// </summary>
    public sealed class TelegraphService : ITelegraphService
    {
        private readonly Dictionary<UnitId, EnemyIntent> _intents =
            new Dictionary<UnitId, EnemyIntent>();
        private readonly IEventBus _events;

        public TelegraphService(IEventBus events)
        {
            _events = events;
        }

        public IReadOnlyCollection<EnemyIntent> All => _intents.Values;

        public IReadOnlyList<GridCoord> DangerCells
        {
            get
            {
                var set = new List<GridCoord>();
                var seen = new HashSet<GridCoord>();
                foreach (var intent in _intents.Values)
                {
                    var cells = intent.DangerCells;
                    for (int i = 0; i < cells.Count; i++)
                    {
                        if (seen.Add(cells[i]))
                        {
                            set.Add(cells[i]);
                        }
                    }
                }

                set.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
                return set;
            }
        }

        public bool TryGet(UnitId unit, out EnemyIntent intent)
        {
            return _intents.TryGetValue(unit, out intent);
        }

        public void Declare(EnemyIntent intent)
        {
            if (intent == null || !intent.Unit.IsValid)
            {
                return;
            }

            _intents[intent.Unit] = intent;
            _events?.Publish(new IntentDeclaredEvent(intent));
        }

        public void Clear()
        {
            int count = _intents.Count;
            _intents.Clear();
            _events?.Publish(new TelegraphClearedEvent(count));
        }

        /// <summary>Chỉ SnapshotService dùng: nạp lại ý định mà không phát event.</summary>
        internal void RestoreSilently(IReadOnlyList<EnemyIntent> intents)
        {
            _intents.Clear();
            if (intents == null)
            {
                return;
            }

            for (int i = 0; i < intents.Count; i++)
            {
                var intent = intents[i];
                if (intent != null && intent.Unit.IsValid)
                {
                    _intents[intent.Unit] = intent;
                }
            }
        }
    }
}
