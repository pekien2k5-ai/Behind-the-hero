using System;

namespace BTH.Core.Units
{
    /// <summary>
    /// Khoá unit trong một run. None = 0 để default(UnitId) vô nghĩa một cách an toàn.
    /// </summary>
    public readonly struct UnitId : IEquatable<UnitId>
    {
        public static readonly UnitId None = new UnitId(0);

        public readonly int Value;

        public UnitId(int value)
        {
            Value = value;
        }

        public bool IsValid => Value > 0;

        public bool Equals(UnitId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(UnitId a, UnitId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(UnitId a, UnitId b)
        {
            return a.Value != b.Value;
        }

        public override string ToString()
        {
            return IsValid ? "U" + Value : "U(None)";
        }
    }
}
