using System;

namespace BTH.Core.Grid
{
    /// <summary>
    /// Định danh địa hình dạng chuỗi (không dùng enum) để thêm địa hình mới
    /// trong Content mà không sửa Core. So sánh theo ordinal, không phân biệt hoa thường.
    /// </summary>
    public readonly struct TerrainId : IEquatable<TerrainId>
    {
        public static readonly TerrainId None = new TerrainId(string.Empty);
        public static readonly TerrainId Floor = new TerrainId("floor");
        public static readonly TerrainId Wall = new TerrainId("wall");
        public static readonly TerrainId Pit = new TerrainId("pit");
        public static readonly TerrainId Cracked = new TerrainId("cracked");
        public static readonly TerrainId Poison = new TerrainId("poison");
        public static readonly TerrainId Lava = new TerrainId("lava");
        public static readonly TerrainId Chest = new TerrainId("chest");

        public readonly string Value;

        public TerrainId(string value)
        {
            Value = value ?? string.Empty;
        }

        public bool IsEmpty => Value.Length == 0;

        public bool Equals(TerrainId other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is TerrainId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(TerrainId a, TerrainId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(TerrainId a, TerrainId b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return Value;
        }

        /// <summary>Cho phép viết TerrainId tường minh từ chuỗi dữ liệu (asset/JSON).</summary>
        public static explicit operator TerrainId(string value)
        {
            return new TerrainId(value);
        }
    }
}
