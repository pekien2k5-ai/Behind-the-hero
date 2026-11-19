using System.Collections.Generic;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Content
{
    /// <summary>
    /// Dịch vụ tìm đường A*, truy vấn vùng nguy hiểm AI Telegraph và Tua ngược trạng thái.
    /// </summary>
    public static class TacticalNavigator
    {
        /// <summary>
        /// Tính đường đi tối ưu A* từ vị trí người chơi tới ô mục tiêu để hiển thị Pathfinding Highlight (GDD §6.4).
        /// </summary>
        public static IReadOnlyList<GridCoord> PreviewPlayerPath(GameState state, GridCoord destination)
        {
            var player = state?.Units?.Player;
            if (player == null || !player.IsAlive || state.Pathfinder == null || !state.Grid.InBounds(destination))
            {
                return new List<GridCoord>();
            }

            var req = new PathRequest(player.Position, destination, player.Move, player.MovePoints, player.Id);
            var res = state.Pathfinder.FindPath(req);
            return res.IsFound && res.Path != null ? res.Path : new List<GridCoord>();
        }

        /// <summary>Tính tập ô có thể bước tới trong lượt này bằng thuật toán Dijkstra / A*.</summary>
        public static ReachableSet GetPlayerReachableSet(GameState state)
        {
            var player = state?.Units?.Player;
            if (player == null || !player.IsAlive || state.Pathfinder == null)
            {
                return null;
            }
            return MovementRangeCalculator.Compute(state.Pathfinder, state.Grid, player.Position,
                player.Move, player.MovePoints, player.Id);
        }

        /// <summary>Lấy danh sách các ô đang bị Boss/Quái ngắm bắn (Danger Zone Warning - GDD §3.1.2 & §6.4).</summary>
        public static IReadOnlyList<GridCoord> GetTelegraphDangerCells(GameState state)
        {
            return state?.Telegraph?.DangerCells ?? new List<GridCoord>();
        }

        /// <summary>Thực hiện Tua ngược thời gian (Time Rewind / Undo) về checkpoint gần nhất.</summary>
        public static bool TryRewind(GameSession session)
        {
            if (session?.State?.Snapshots == null || !session.State.Snapshots.CanRewind)
            {
                return false;
            }
            session.State.Snapshots.Rewind();
            return true;
        }
    }
}
