using System;

namespace BTH.Core.Combat
{
    /// <summary>Định danh trạng thái dạng chuỗi để Content thêm trạng thái mới.</summary>
    public readonly struct StatusId : IEquatable<StatusId>
    {
        public static readonly StatusId None = new StatusId(string.Empty);

        public readonly string Value;

        public StatusId(string value)
        {
            Value = value ?? string.Empty;
        }

        public bool IsEmpty => Value.Length == 0;

        public bool Equals(StatusId other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is StatusId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(StatusId a, StatusId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(StatusId a, StatusId b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return Value;
        }

        public static explicit operator StatusId(string value)
        {
            return new StatusId(value);
        }
    }
}
