using System;
using System.Collections.Generic;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Grid
{
    /// <summary>
    /// Lưới mặc định: mảng ô chữ nhật, địa hình do dữ liệu gán qua SetTerrain.
    /// Không chứa luật chơi; chỉ là state + truy vấn hình học.
    /// </summary>
    public sealed class GridMap : IGridMap
    {
        private readonly CellState[] _cells;
        private readonly IEventBus _events;
        private readonly HashSet<GridCoord> _locked = new HashSet<GridCoord>();

        public GridMap(int width, int height, IEventBus events = null)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            _cells = new CellState[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[Index(x, y)] = new CellState(new GridCoord(x, y));
                }
            }

            _events = events;
        }

        public int Width { get; }

        public int Height { get; }

        public event Action<GridCoord> CellChanged;

        public bool InBounds(GridCoord coord)
        {
            return coord.X >= 0 && coord.Y >= 0 && coord.X < Width && coord.Y < Height;
        }

        public CellState Get(GridCoord coord)
        {
            if (!InBounds(coord))
            {
                throw new ArgumentOutOfRangeException(nameof(coord), coord + " ngoài lưới " + Width + "x" + Height);
            }

            return _cells[Index(coord.X, coord.Y)];
        }

        public bool TryGet(GridCoord coord, out CellState cell)
        {
            if (!InBounds(coord))
            {
                cell = null;
                return false;
            }

            cell = _cells[Index(coord.X, coord.Y)];
            return true;
        }

        public bool IsBlocked(GridCoord coord)
        {
            return !TryGet(coord, out var cell) || cell.BlocksMove;
        }

        public bool IsWalkable(GridCoord coord, MoveProfile profile)
        {
            if (!TryGet(coord, out var cell))
            {
                return false;
            }

            if (cell.IsLocked)
            {
                return false;
            }

            if (cell.IsOccupied && !profile.CanPassUnits)
            {
                return false;
            }

            if (!cell.BlocksMove)
            {
                return true;
            }

            // Quy ước Core: CanFly vượt được vật cản chỉ chặn đường đi (vực, bẫy),
            // nhưng vẫn bị chặn bởi vật cản chặn tầm nhìn (tường).
            return profile.CanFly && !cell.BlocksLineOfSight;
        }

        public bool HasLineOfSight(GridCoord from, GridCoord to)
        {
            if (!InBounds(from) || !InBounds(to))
            {
                return false;
            }

            var line = GridShapes.Line(from, to);
            for (int i = 1; i < line.Count - 1; i++)
            {
                var cell = Get(line[i]);
                if (cell.BlocksLineOfSight)
                {
                    return false;
                }
            }

            return true;
        }

        public int MovementCost(GridCoord coord)
        {
            return Get(coord).MovementCost;
        }

        public bool IsLocked(GridCoord coord)
        {
            return _locked.Contains(coord);
        }

        public void SetTerrain(GridCoord coord, TerrainId terrain, TerrainFlags flags,
            int movementCost = 1, int hazardDamagePerTurn = 0)
        {
            var cell = Get(coord);
            cell.Terrain = terrain;
            cell.Flags = flags;
            cell.MovementCost = movementCost < 1 ? 1 : movementCost;
            cell.HazardDamagePerTurn = hazardDamagePerTurn < 0 ? 0 : hazardDamagePerTurn;
            RaiseChanged(coord);
        }

        public void ArmCollapse(GridCoord coord, int turns)
        {
            var cell = Get(coord);
            cell.TurnsUntilCollapse = turns < 0 ? 0 : turns;
        }

        public void TickCollapse()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i];
                if (cell.TurnsUntilCollapse <= 0)
                {
                    continue;
                }

                cell.TurnsUntilCollapse--;
                if (cell.TurnsUntilCollapse > 0)
                {
                    continue;
                }

                cell.Terrain = TerrainId.Pit;
                cell.Flags = TerrainFlags.BlocksMove;
                cell.MovementCost = 1;
                cell.HazardDamagePerTurn = 0;
                RaiseChanged(cell.Coord);
                _events?.Publish(new CellCollapsedEvent(cell.Coord));
            }
        }

        public void LockArea(IEnumerable<GridCoord> cells)
        {
            if (cells == null)
            {
                return;
            }

            int count = 0;
            foreach (var coord in cells)
            {
                if (!InBounds(coord))
                {
                    continue;
                }

                if (_locked.Add(coord))
                {
                    Get(coord).IsLocked = true;
                    count++;
                }
            }

            if (count > 0)
            {
                _events?.Publish(new ArenaLockedEvent(_locked.Count));
            }
        }

        public void UnlockArea()
        {
            foreach (var coord in _locked)
            {
                if (TryGet(coord, out var cell))
                {
                    cell.IsLocked = false;
                }
            }

            _locked.Clear();
        }

        public void SetOccupant(GridCoord coord, UnitId unit)
        {
            Get(coord).Occupant = unit;
        }

        /// <summary>Chỉ dùng khi test/snapshot: đọc toàn bộ ô theo thứ tự dòng-cột xác định.</summary>
        internal CellState[] CopyCells()
        {
            var copy = new CellState[_cells.Length];
            for (int i = 0; i < _cells.Length; i++)
            {
                copy[i] = new CellState(_cells[i]);
            }

            return copy;
        }

        internal void RestoreCells(CellState[] source, IEnumerable<GridCoord> locked)
        {
            if (source == null || source.Length != _cells.Length)
            {
                throw new ArgumentException("Snapshot lưới không khớp kích thước", nameof(source));
            }

            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].RestoreFrom(source[i]);
            }

            _locked.Clear();
            if (locked != null)
            {
                foreach (var coord in locked)
                {
                    _locked.Add(coord);
                }
            }
        }

        internal List<GridCoord> CopyLockedCells()
        {
            var result = new List<GridCoord>(_locked.Count);
            foreach (var coord in _locked)
            {
                result.Add(coord);
            }

            result.Sort(CompareCoord);
            return result;
        }

        private static int CompareCoord(GridCoord a, GridCoord b)
        {
            int byX = a.X.CompareTo(b.X);
            return byX != 0 ? byX : a.Y.CompareTo(b.Y);
        }

        private void RaiseChanged(GridCoord coord)
        {
            CellChanged?.Invoke(coord);
            var cell = Get(coord);
            _events?.Publish(new CellChangedEvent(coord, cell.Terrain, cell.Flags, cell.HazardDamagePerTurn));
        }

        private int Index(int x, int y)
        {
            return y * Width + x;
        }
    }
}
