using System;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Hệ nguyên tố dạng chuỗi để Content thêm hệ mới mà không sửa Core
    /// (Vật lý, Hoả, Thánh, Hắc ám, Xuyên giáp, Thời gian).
    /// </summary>
    public readonly struct ElementId : IEquatable<ElementId>
    {
        public static readonly ElementId None = new ElementId(string.Empty);
        public static readonly ElementId Physical = new ElementId("physical");
        public static readonly ElementId Fire = new ElementId("fire");
        public static readonly ElementId Holy = new ElementId("holy");
        public static readonly ElementId Necrotic = new ElementId("necrotic");
        public static readonly ElementId Piercing = new ElementId("piercing");
        public static readonly ElementId Temporal = new ElementId("temporal");

        public readonly string Value;

        public ElementId(string value)
        {
            Value = value ?? string.Empty;
        }

        public bool IsEmpty => Value.Length == 0;

        public bool Equals(ElementId other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is ElementId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(ElementId a, ElementId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ElementId a, ElementId b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return Value;
        }

        public static explicit operator ElementId(string value)
        {
            return new ElementId(value);
        }
    }
}
