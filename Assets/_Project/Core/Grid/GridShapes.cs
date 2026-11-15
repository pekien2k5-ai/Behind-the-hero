using System;
using System.Collections.Generic;

namespace BTH.Core.Grid
{
    /// <summary>
    /// Hình học lưới thuần, không phụ thuộc bản đồ. Mọi hàm trả danh sách mới,
    /// thứ tự xác định (không dùng HashSet để giữ tính tất định khi test/AI).
    /// Các hàm không tự cắt theo biên — caller kiểm tra IGridMap.InBounds.
    /// </summary>
    public static class GridShapes
    {
        public static readonly GridCoord[] Directions4 =
        {
            GridCoord.North, GridCoord.East, GridCoord.South, GridCoord.West
        };

        public static List<GridCoord> Neighbours4(GridCoord center)
        {
            var result = new List<GridCoord>(4);
            for (int i = 0; i < Directions4.Length; i++)
            {
                result.Add(center + Directions4[i]);
            }

            return result;
        }

        /// <summary>Đường Bresenham gồm cả hai đầu, không cắt theo biên.</summary>
        public static List<GridCoord> Line(GridCoord from, GridCoord to)
        {
            var result = new List<GridCoord>();
            int x0 = from.X;
            int y0 = from.Y;
            int x1 = to.X;
            int y1 = to.Y;
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                result.Add(new GridCoord(x0, y0));
                if (x0 == x1 && y0 == y1)
                {
                    return result;
                }

                int e2 = err * 2;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>Hình chữ thập (diamond) bán kính radius, gồm tâm.</summary>
        public static List<GridCoord> Cross(GridCoord center, int radius)
        {
            var result = new List<GridCoord>();
            for (int dx = -radius; dx <= radius; dx++)
            {
                int remaining = radius - Math.Abs(dx);
                for (int dy = -remaining; dy <= remaining; dy++)
                {
                    result.Add(new GridCoord(center.X + dx, center.Y + dy));
                }
            }

            return result;
        }

        /// <summary>Hình vuông (Chebyshev) bán kính radius, gồm tâm. Đây là 3x3 khi radius = 1.</summary>
        public static List<GridCoord> Square(GridCoord center, int radius)
        {
            var result = new List<GridCoord>();
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    result.Add(new GridCoord(center.X + dx, center.Y + dy));
                }
            }

            return result;
        }

        /// <summary>
        /// Nón 45 độ hướng theo (dirX,dirY) — một trong bốn hướng trục.
        /// Ô thứ t (1..length) nhận |lateral| &lt;= t. Không gồm ô gốc.
        /// </summary>
        public static List<GridCoord> Cone(GridCoord origin, int dirX, int dirY, int length)
        {
            var result = new List<GridCoord>();
            for (int forward = 1; forward <= length; forward++)
            {
                for (int lateral = -forward; lateral <= forward; lateral++)
                {
                    // dir là hướng trục: đổi trục theo hướng để lateral luôn là trục còn lại.
                    int x = origin.X + dirX * forward + (dirY == 0 ? 0 : lateral);
                    int y = origin.Y + dirY * forward + (dirX == 0 ? 0 : lateral);
                    result.Add(new GridCoord(x, y));
                }
            }

            return result;
        }

        /// <summary>Hàng thẳng bắt đầu sau ô gốc, gồm length ô theo hướng trục.</summary>
        public static List<GridCoord> Row(GridCoord origin, int dirX, int dirY, int length)
        {
            var result = new List<GridCoord>(length);
            for (int i = 1; i <= length; i++)
            {
                result.Add(new GridCoord(origin.X + dirX * i, origin.Y + dirY * i));
            }

            return result;
        }

        /// <summary>Các ô của footprint, neo là góc dưới-trái (min X, min Y), kéo theo +X/+Y.</summary>
        public static List<GridCoord> Footprint(GridCoord anchor, int sizeX, int sizeY)
        {
            if (sizeX < 1)
            {
                sizeX = 1;
            }

            if (sizeY < 1)
            {
                sizeY = 1;
            }

            var result = new List<GridCoord>(sizeX * sizeY);
            for (int dx = 0; dx < sizeX; dx++)
            {
                for (int dy = 0; dy < sizeY; dy++)
                {
                    result.Add(new GridCoord(anchor.X + dx, anchor.Y + dy));
                }
            }

            return result;
        }

        public static bool Contains(List<GridCoord> cells, GridCoord coord)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == coord)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
