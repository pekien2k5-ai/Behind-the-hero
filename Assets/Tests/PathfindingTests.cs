using BTH.Core.Grid;
using BTH.Core.Units;
using NUnit.Framework;

namespace BTH.Core.Tests
{
    [TestFixture]
    public sealed class PathfindingTests
    {
        [Test]
        public void FindPath_StraightLine_IncludesStart_AndCostsTiles()
        {
            var state = TestWorld.State(5, 5);
            var pathfinder = new AStarPathfinder(state.Grid);

            var result = pathfinder.FindPath(new PathRequest(new GridCoord(0, 0),
                new GridCoord(3, 0), MoveProfile.Walk(4), 4));

            Assert.That(result.Status, Is.EqualTo(PathStatus.Found));
            Assert.That(result.Cost, Is.EqualTo(3));
            Assert.That(result.Path.Count, Is.EqualTo(4));
            Assert.That(result.Path[0], Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(result.Path[3], Is.EqualTo(new GridCoord(3, 0)));
        }

        [Test]
        public void FindPath_AvoidsWalls()
        {
            var state = TestWorld.State(5, 5);
            state.Grid.SetTerrain(new GridCoord(1, 0), TerrainId.Wall, TerrainFlags.BlocksMove);
            state.Grid.SetTerrain(new GridCoord(1, 1), TerrainId.Wall, TerrainFlags.BlocksMove);

            var result = new AStarPathfinder(state.Grid).FindPath(new PathRequest(
                new GridCoord(0, 0), new GridCoord(2, 0), MoveProfile.Walk(8), 8));

            Assert.That(result.Status, Is.EqualTo(PathStatus.Found));
            Assert.That(result.Cost, Is.GreaterThan(2));
        }

        [Test]
        public void FindPath_BlockedByUnit_Detours()
        {
            var state = TestWorld.State(3, 3);
            var blocker = state.Units.Add(TestWorld.Unit("imp", UnitFaction.Enemy), new GridCoord(1, 0));

            var result = new AStarPathfinder(state.Grid).FindPath(new PathRequest(
                new GridCoord(0, 0), new GridCoord(2, 0), MoveProfile.Walk(6), 6));

            Assert.That(blocker.IsAlive, Is.True);
            Assert.That(result.Status, Is.EqualTo(PathStatus.Found));
            Assert.That(result.Cost, Is.EqualTo(4));
            Assert.That(System.Linq.Enumerable.Contains(result.Path, new GridCoord(1, 0)),
                Is.False, "Đường đi không được cắt qua ô đang có unit.");
        }

        [Test]
        public void FindPath_NoRoute_WhenCorridorBlocked()
        {
            var state = TestWorld.State(3, 1);
            state.Grid.SetTerrain(new GridCoord(1, 0), TerrainId.Wall, TerrainFlags.BlocksMove);

            var result = new AStarPathfinder(state.Grid).FindPath(new PathRequest(
                new GridCoord(0, 0), new GridCoord(2, 0), MoveProfile.Walk(4), 0));

            Assert.That(result.Status, Is.EqualTo(PathStatus.NoPath));
        }

        [Test]
        public void FindPath_OutOfRange_WhenBudgetTooSmall()
        {
            var state = TestWorld.State(5, 5);

            var result = new AStarPathfinder(state.Grid).FindPath(new PathRequest(
                new GridCoord(0, 0), new GridCoord(3, 0), MoveProfile.Walk(8), 2));

            Assert.That(result.Status, Is.EqualTo(PathStatus.OutOfRange));
        }

        [Test]
        public void ComputeReachable_ManhattanTwo_OnEmptyGrid()
        {
            var state = TestWorld.State(7, 7);
            var reachable = new AStarPathfinder(state.Grid).ComputeReachable(new GridCoord(3, 3),
                MoveProfile.Walk(2), 2);

            Assert.That(reachable.Count, Is.EqualTo(12));
            Assert.That(reachable.Contains(new GridCoord(3, 3)), Is.False);
            Assert.That(reachable.CostTo(new GridCoord(3, 5)), Is.EqualTo(2));
            Assert.That(reachable.CostTo(new GridCoord(5, 5)), Is.LessThan(0));
        }
    }
}
