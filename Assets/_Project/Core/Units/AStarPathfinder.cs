using System;
using System.Collections.Generic;
using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>
    /// A* 4 hướng, không đi chéo, chi phí theo địa hình, chặn unit trừ khi
    /// profile.CanPassUnits. Tie-break tất định: f → h → chỉ số ô (Y rồi X).
    /// </summary>
    public sealed class AStarPathfinder : IPathfinder
    {
        private readonly IGridMap _grid;

        public AStarPathfinder(IGridMap grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public PathResult FindPath(PathRequest request)
        {
            if (!_grid.InBounds(request.Start))
            {
                return PathResult.Fail(PathStatus.InvalidStart);
            }

            if (!_grid.InBounds(request.Goal))
            {
                return PathResult.Fail(PathStatus.InvalidTarget);
            }

            if (request.Start == request.Goal)
            {
                return PathResult.Found(new[] { request.Start }, 0);
            }

            int width = _grid.Width;
            int total = width * _grid.Height;
            var costs = new int[total];
            var previous = new int[total];
            var closed = new bool[total];
            for (int i = 0; i < total; i++)
            {
                costs[i] = int.MaxValue;
                previous[i] = -1;
            }

            int start = IndexOf(request.Start, width);
            int goal = IndexOf(request.Goal, width);
            costs[start] = 0;
            var open = new PathMinHeap(64);
            open.Push(new PathNode(request.Start, 0, Heuristic(request.Start, request.Goal), start));

            while (!open.IsEmpty)
            {
                var node = open.Pop();
                int index = node.Index;
                if (closed[index])
                {
                    continue;
                }

                closed[index] = true;
                if (index == goal)
                {
                    return PathResult.Found(BuildPath(previous, goal, width), node.Cost);
                }

                var directions = GridShapes.Directions4;
                for (int i = 0; i < directions.Length; i++)
                {
                    var next = node.Coord + directions[i];
                    if (!_grid.InBounds(next))
                    {
                        continue;
                    }

                    int nextIndex = IndexOf(next, width);
                    if (closed[nextIndex] || !CanEnter(next, request.Profile, request.IgnoreUnit))
                    {
                        continue;
                    }

                    int step = _grid.MovementCost(next);
                    if (step < 1)
                    {
                        step = 1;
                    }

                    int nextCost = node.Cost + step;
                    if ((request.MaxCost > 0 && nextCost > request.MaxCost) || nextCost >= costs[nextIndex])
                    {
                        continue;
                    }

                    costs[nextIndex] = nextCost;
                    previous[nextIndex] = index;
                    open.Push(new PathNode(next, nextCost, Heuristic(next, request.Goal), nextIndex));
                }
            }

            return PathResult.Fail(request.MaxCost > 0 ? PathStatus.OutOfRange : PathStatus.NoPath);
        }

        public ReachableSet ComputeReachable(GridCoord start, MoveProfile profile, int maxCost, UnitId self = default)
        {
            var cells = new Dictionary<GridCoord, int>();
            if (!_grid.InBounds(start) || maxCost <= 0)
            {
                return new ReachableSet(start, cells);
            }

            int width = _grid.Width;
            int total = width * _grid.Height;
            var costs = new int[total];
            for (int i = 0; i < total; i++)
            {
                costs[i] = int.MaxValue;
            }

            int startIndex = IndexOf(start, width);
            costs[startIndex] = 0;
            var open = new PathMinHeap(64);
            open.Push(new PathNode(start, 0, 0, startIndex));

            while (!open.IsEmpty)
            {
                var node = open.Pop();
                if (node.Cost > costs[node.Index])
                {
                    continue;
                }

                var directions = GridShapes.Directions4;
                for (int i = 0; i < directions.Length; i++)
                {
                    var next = node.Coord + directions[i];
                    if (!_grid.InBounds(next) || !CanEnter(next, profile, self))
                    {
                        continue;
                    }

                    int step = _grid.MovementCost(next);
                    if (step < 1)
                    {
                        step = 1;
                    }

                    int nextCost = node.Cost + step;
                    int nextIndex = IndexOf(next, width);
                    if (nextCost > maxCost || nextCost >= costs[nextIndex])
                    {
                        continue;
                    }

                    costs[nextIndex] = nextCost;
                    cells[next] = nextCost;
                    open.Push(new PathNode(next, nextCost, 0, nextIndex));
                }
            }

            return new ReachableSet(start, cells);
        }

        /// <summary>Vào được ô với profile; bỏ qua chính unit self.</summary>
        private bool CanEnter(GridCoord coord, MoveProfile profile, UnitId self)
        {
            var cell = _grid.Get(coord);
            if (cell.IsLocked)
            {
                return false;
            }

            if (cell.BlocksMove && !(profile.CanFly && !cell.BlocksLineOfSight))
            {
                return false;
            }

            if (cell.Occupant.IsValid && cell.Occupant != self && !profile.CanPassUnits)
            {
                return false;
            }

            return true;
        }

        private static int Heuristic(GridCoord from, GridCoord to)
        {
            return GridCoord.Manhattan(from, to);
        }

        private static int IndexOf(GridCoord coord, int width)
        {
            return coord.Y * width + coord.X;
        }

        private static IReadOnlyList<GridCoord> BuildPath(int[] previous, int goal, int width)
        {
            var reversed = new List<GridCoord>();
            int current = goal;
            while (current >= 0)
            {
                reversed.Add(new GridCoord(current % width, current / width));
                current = previous[current];
            }

            reversed.Reverse();
            return reversed;
        }
    }
}
