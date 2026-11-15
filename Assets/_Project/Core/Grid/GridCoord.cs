using System;

namespace BTH.Core.Grid
{
    /// <summary>
    /// Toạ độ ô vuông trên lưới. Gốc là góc dưới-trái (0,0), X sang phải, Y lên trên.
    /// Kiểu giá trị bất biến: không cấp phát khi dùng trong vòng lặp AI/pathfinding.
    /// </summary>
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        /// <summary>Toạ độ rỗng. Mọi thao tác với ô rỗng phải kiểm tra <see cref="IsValid"/> trước.</summary>
        public static readonly GridCoord None = new GridCoord(-1, -1);

        public static readonly GridCoord North = new GridCoord(0, 1);
        public static readonly GridCoord South = new GridCoord(0, -1);
        public static readonly GridCoord East = new GridCoord(1, 0);
        public static readonly GridCoord West = new GridCoord(-1, 0);

        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Chỉ hợp lệ khi nằm trong nửa mặt phẳng dương; tài liệu quy ước lưới bắt đầu từ (0,0).</summary>
        public bool IsValid => X >= 0 && Y >= 0;

        public static int Manhattan(GridCoord a, GridCoord b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        public static int Chebyshev(GridCoord a, GridCoord b)
        {
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        public static GridCoord operator +(GridCoord a, GridCoord b)
        {
            return new GridCoord(a.X + b.X, a.Y + b.Y);
        }

        public static GridCoord operator -(GridCoord a, GridCoord b)
        {
            return new GridCoord(a.X - b.X, a.Y - b.Y);
        }

        public static bool operator ==(GridCoord a, GridCoord b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(GridCoord a, GridCoord b)
        {
            return !(a == b);
        }

        public bool Equals(GridCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }
}
