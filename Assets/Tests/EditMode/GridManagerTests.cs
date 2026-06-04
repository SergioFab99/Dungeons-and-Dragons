using System.Collections.Generic;
using DungeonRpg;
using NUnit.Framework;
using UnityEngine;

namespace DungeonRpg.Tests
{
    public class GridManagerTests
    {
        [Test]
        public void CanEnter_ReturnsFalseForWallsAndOutOfBounds()
        {
            GridManager grid = new GridManager(3, 3, new[] { new GridPosition(1, 1) }, 1f, Vector3.zero);

            Assert.IsFalse(grid.CanEnter(new GridPosition(1, 1)));
            Assert.IsFalse(grid.CanEnter(new GridPosition(-1, 0)));
            Assert.IsTrue(grid.CanEnter(new GridPosition(0, 0)));
        }

        [Test]
        public void TryPlaceOccupant_BlocksOccupiedTiles()
        {
            GridManager grid = new GridManager(3, 3, new List<GridPosition>(), 1f, Vector3.zero);
            FakeOccupant first = new FakeOccupant();
            FakeOccupant second = new FakeOccupant();

            Assert.IsTrue(grid.TryPlaceOccupant(first, new GridPosition(0, 0)));
            Assert.IsFalse(grid.TryPlaceOccupant(second, new GridPosition(0, 0)));
        }

        [Test]
        public void TryMoveOccupant_UpdatesGridPositionAndOccupancy()
        {
            GridManager grid = new GridManager(3, 3, new List<GridPosition>(), 1f, Vector3.zero);
            FakeOccupant occupant = new FakeOccupant();

            grid.TryPlaceOccupant(occupant, new GridPosition(0, 0));
            bool moved = grid.TryMoveOccupant(occupant, new GridPosition(1, 0));

            Assert.IsTrue(moved);
            Assert.AreEqual(new GridPosition(1, 0), occupant.GridPosition);
            Assert.IsFalse(grid.IsOccupied(new GridPosition(0, 0)));
            Assert.IsTrue(grid.IsOccupied(new GridPosition(1, 0)));
        }

        [Test]
        public void GridToWorld_UsesAuthoredTileCenters()
        {
            Dictionary<GridPosition, Vector3> centers = new Dictionary<GridPosition, Vector3>
            {
                [new GridPosition(0, 0)] = new Vector3(10f, 0f, 20f),
                [new GridPosition(1, 0)] = new Vector3(12f, 0f, 20f)
            };
            GridManager grid = new GridManager(centers, new List<GridPosition>());

            Assert.AreEqual(centers[new GridPosition(1, 0)], grid.GridToWorld(new GridPosition(1, 0)));
        }

        [Test]
        public void WorldToGrid_ReturnsNearestAuthoredTile()
        {
            Dictionary<GridPosition, Vector3> centers = new Dictionary<GridPosition, Vector3>
            {
                [new GridPosition(0, 0)] = new Vector3(-5f, 0f, -5f),
                [new GridPosition(1, 0)] = new Vector3(4f, 0f, 2f),
                [new GridPosition(0, 1)] = new Vector3(-5f, 0f, 5f)
            };
            GridManager grid = new GridManager(centers, new List<GridPosition>());

            Assert.AreEqual(new GridPosition(1, 0), grid.WorldToGrid(new Vector3(4.4f, 3f, 2.2f)));
        }

        [Test]
        public void AuthoredGrid_BlocksAuthoredWallPositions()
        {
            Dictionary<GridPosition, Vector3> centers = new Dictionary<GridPosition, Vector3>
            {
                [new GridPosition(0, 0)] = new Vector3(10f, 0f, 20f),
                [new GridPosition(1, 0)] = new Vector3(12f, 0f, 20f)
            };
            GridManager grid = new GridManager(centers, new[] { new GridPosition(1, 0) });

            Assert.IsTrue(grid.CanEnter(new GridPosition(0, 0)));
            Assert.IsFalse(grid.CanEnter(new GridPosition(1, 0)));
        }

        private class FakeOccupant : IGridOccupant
        {
            public GridPosition GridPosition { get; private set; }
            public bool BlocksMovement => true;

            public void SetGridPosition(GridPosition position)
            {
                GridPosition = position;
            }
        }
    }
}
