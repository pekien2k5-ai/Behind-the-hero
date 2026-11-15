using System.Collections.Generic;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;
using NUnit.Framework;

namespace BTH.Core.Tests
{
    [TestFixture]
    public sealed class GridTests
    {
        [Test]
        public void Coord_Manhattan_Chebyshev_And_Validity()
        {
            var a = new GridCoord(2, 3);
            var b = new GridCoord(0, 1);

            Assert.That(GridCoord.Manhattan(a, b), Is.EqualTo(4));
            Assert.That(GridCoord.Chebyshev(a, b), Is.EqualTo(2));
            Assert.That(new GridCoord(0, 0).IsValid, Is.True);
            Assert.That(new GridCoord(-1, 0).IsValid, Is.False);
            Assert.That(GridCoord.None.IsValid, Is.False);
        }

        [Test]
        public void GridMap_Terrain_BlocksWalkAndFlying()
        {
            var state = TestWorld.State(5, 5);
            var wall = new GridCoord(2, 2);
            state.Grid.SetTerrain(wall, TerrainId.Wall,
                TerrainFlags.BlocksMove | TerrainFlags.BlocksLineOfSight);

            Assert.That(state.Grid.IsBlocked(wall), Is.True);
            Assert.That(state.Grid.IsWalkable(wall, MoveProfile.Walk(2)), Is.False);
            Assert.That(state.Grid.IsWalkable(wall, MoveProfile.Fly(2)), Is.False);
        }

        [Test]
        public void GridMap_Flying_CrossesPit_ButNotWall()
        {
            var state = TestWorld.State(5, 5);
            var pit = new GridCoord(1, 1);
            state.Grid.SetTerrain(pit, TerrainId.Pit, TerrainFlags.BlocksMove);

            Assert.That(state.Grid.IsWalkable(pit, MoveProfile.Walk(2)), Is.False);
            Assert.That(state.Grid.IsWalkable(pit, MoveProfile.Fly(2)), Is.True);
        }

        [Test]
        public void GridMap_LineOfSight_BlockedByWall()
        {
            var state = TestWorld.State(5, 5);
            Assert.That(state.Grid.HasLineOfSight(new GridCoord(0, 2), new GridCoord(4, 2)), Is.True);

            state.Grid.SetTerrain(new GridCoord(2, 2), TerrainId.Wall,
                TerrainFlags.BlocksMove | TerrainFlags.BlocksLineOfSight);

            Assert.That(state.Grid.HasLineOfSight(new GridCoord(0, 2), new GridCoord(4, 2)), Is.False);
        }

        [Test]
        public void GridMap_Collapse_TurnsCellIntoPit_AndRaisesEvent()
        {
            var events = new EventBus();
            var grid = new GridMap(5, 5, events);
            var target = new GridCoord(3, 3);
            int collapsed = 0;
            using (events.Subscribe<CellCollapsedEvent>((in CellCollapsedEvent e) =>
                   {
                       if (e.Coord == target)
                       {
                           collapsed++;
                       }
                   }))
            {
                grid.ArmCollapse(target, 1);
                grid.TickCollapse();
            }

            Assert.That(grid.Get(target).Terrain, Is.EqualTo(TerrainId.Pit));
            Assert.That(grid.IsBlocked(target), Is.True);
            Assert.That(collapsed, Is.EqualTo(1));
        }

        [Test]
        public void GridMap_LockArea_StopsMovement_UntilUnlocked()
        {
            var state = TestWorld.State(5, 5);
            var cell = new GridCoord(2, 1);

            state.Grid.LockArea(new[] { cell });
            Assert.That(state.Grid.IsLocked(cell), Is.True);
            Assert.That(state.Grid.IsWalkable(cell, MoveProfile.Walk(2)), Is.False);

            state.Grid.UnlockArea();
            Assert.That(state.Grid.IsLocked(cell), Is.False);
            Assert.That(state.Grid.IsWalkable(cell, MoveProfile.Walk(2)), Is.True);
        }

        [Test]
        public void GridMap_ReportsCellChanged()
        {
            var events = new EventBus();
            var grid = new GridMap(4, 4, events);
            int changes = 0;
            using (events.Subscribe<CellChangedEvent>((in CellChangedEvent e) => changes++))
            {
                grid.SetTerrain(new GridCoord(1, 1), TerrainId.Poison, TerrainFlags.IsHazard, 1, 3);
            }

            Assert.That(changes, Is.EqualTo(1));
            Assert.That(grid.Get(new GridCoord(1, 1)).HazardDamagePerTurn, Is.EqualTo(3));
        }

        [Test]
        public void Shapes_Line_Neighbours_Cone_Footprint()
        {
            var line = GridShapes.Line(new GridCoord(0, 0), new GridCoord(3, 0));
            Assert.That(line.Count, Is.EqualTo(4));
            Assert.That(line[0], Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(line[3], Is.EqualTo(new GridCoord(3, 0)));

            Assert.That(GridShapes.Neighbours4(new GridCoord(1, 1)).Count, Is.EqualTo(4));

            var cone = GridShapes.Cone(new GridCoord(0, 0), 1, 0, 2);
            Assert.That(cone.Count, Is.EqualTo(8));

            var footprint = GridShapes.Footprint(new GridCoord(2, 2), 2, 2);
            Assert.That(footprint.Count, Is.EqualTo(4));
            Assert.That(footprint.Contains(new GridCoord(3, 3)), Is.True);
            Assert.That(GridShapes.Contains(footprint, new GridCoord(2, 3)), Is.True);
        }
    }
}
