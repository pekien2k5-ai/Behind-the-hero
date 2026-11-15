using System;
using System.Collections.Generic;

namespace BTH.Core.Ai
{
    /// <summary>Kho brain mặc định; tra cứu Ordinal và giữ thứ tự đăng ký.</summary>
    public sealed class EnemyBrainLibrary : IEnemyBrainLibrary
    {
        private readonly Dictionary<string, IEnemyBrain> _brains =
            new Dictionary<string, IEnemyBrain>(StringComparer.Ordinal);
        private readonly List<IEnemyBrain> _ordered = new List<IEnemyBrain>();

        public IReadOnlyList<IEnemyBrain> All => _ordered;

        public void Register(IEnemyBrain brain)
        {
            if (brain == null || string.IsNullOrEmpty(brain.Id))
            {
                return;
            }

            if (_brains.TryGetValue(brain.Id, out var old))
            {
                _ordered.Remove(old);
            }

            _brains[brain.Id] = brain;
            _ordered.Add(brain);
        }

        public bool TryGet(string brainId, out IEnemyBrain brain)
        {
            if (string.IsNullOrEmpty(brainId))
            {
                brain = null;
                return false;
            }

            return _brains.TryGetValue(brainId, out brain);
        }

        public IEnemyBrain Get(string brainId)
        {
            if (!TryGet(brainId, out var brain))
            {
                throw new KeyNotFoundException("Không có brain " + brainId);
            }

            return brain;
        }
    }
}
