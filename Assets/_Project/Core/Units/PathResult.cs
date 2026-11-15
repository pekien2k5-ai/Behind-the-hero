using System;
using System.Collections.Generic;
using BTH.Core.Grid;

namespace BTH.Core.Units
{
    public readonly struct PathResult
    {
        public readonly PathStatus Status;

        /// <summary>Gồm cả ô xuất phát; rỗng khi không có đường.</summary>
        public readonly IReadOnlyList<GridCoord> Path;

        public readonly int Cost;

        public PathResult(PathStatus status, IReadOnlyList<GridCoord> path, int cost)
        {
            Status = status;
            Path = path ?? Array.Empty<GridCoord>();
            Cost = cost;
        }

        public bool IsFound => Status == PathStatus.Found;

        public GridCoord Goal => Path.Count == 0 ? GridCoord.None : Path[Path.Count - 1];

        public static PathResult Found(IReadOnlyList<GridCoord> path, int cost)
        {
            return new PathResult(PathStatus.Found, path, cost);
        }

        public static PathResult Fail(PathStatus status)
        {
            return new PathResult(status, Array.Empty<GridCoord>(), 0);
        }
    }
}
