using System;
using System.Collections.Generic;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Bảng khắc hệ. Khoá là cặp (hệ tấn công, hệ phòng thủ).
    /// Mặc định chỉ chứa luật Thánh khắc Hắc ám; Content có thể nạp thêm.
    /// </summary>
    public sealed class ElementMatrix
    {
        private readonly Dictionary<ElementKey, float> _multipliers = new Dictionary<ElementKey, float>();

        public static ElementMatrix CreateDefault()
        {
            var matrix = new ElementMatrix();
            matrix.Set(ElementId.Holy, ElementId.Necrotic, 1.5f);
            matrix.Set(ElementId.Holy, ElementId.Fire, 1.25f);
            matrix.Set(ElementId.Fire, ElementId.Necrotic, 1.25f);
            matrix.Set(ElementId.Necrotic, ElementId.Holy, 0.75f);
            return matrix;
        }

        public void Set(ElementId attack, ElementId defend, float multiplier)
        {
            _multipliers[new ElementKey(attack, defend)] = multiplier;
        }

        public float GetMultiplier(ElementId attack, ElementId defend)
        {
            // Hệ Thời gian bỏ qua mọi kháng/hệ số.
            if (attack == ElementId.Temporal)
            {
                return 1f;
            }

            return _multipliers.TryGetValue(new ElementKey(attack, defend), out var value) ? value : 1f;
        }

        public ElementMatrix Clone()
        {
            var copy = new ElementMatrix();
            foreach (var pair in _multipliers)
            {
                copy._multipliers[pair.Key] = pair.Value;
            }

            return copy;
        }

        private readonly struct ElementKey : IEquatable<ElementKey>
        {
            private readonly ElementId _attack;
            private readonly ElementId _defend;

            public ElementKey(ElementId attack, ElementId defend)
            {
                _attack = attack;
                _defend = defend;
            }

            public bool Equals(ElementKey other)
            {
                return _attack.Equals(other._attack) && _defend.Equals(other._defend);
            }

            public override bool Equals(object obj)
            {
                return obj is ElementKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_attack.GetHashCode() * 397) ^ _defend.GetHashCode();
                }
            }
        }
    }
}
