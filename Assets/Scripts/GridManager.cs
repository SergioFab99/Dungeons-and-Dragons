using System.Collections.Generic;
using UnityEngine;

namespace DungeonRpg
{
    public class GridManager
    {
        private readonly Dictionary<GridPosition, TileData> tiles = new Dictionary<GridPosition, TileData>();
        private readonly Dictionary<GridPosition, Vector3> tileCenters = new Dictionary<GridPosition, Vector3>();
        private readonly float cellSize;
        private readonly Vector3 origin;
        private readonly bool useNearestWorldLookup;

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyDictionary<GridPosition, TileData> Tiles => tiles;
        public IReadOnlyDictionary<GridPosition, Vector3> TileCenters => tileCenters;

        public GridManager(int width, int height, IEnumerable<GridPosition> wallPositions, float cellSize, Vector3 origin)
        {
            Width = width;
            Height = height;
            this.cellSize = cellSize;
            this.origin = origin;
            useNearestWorldLookup = false;

            HashSet<GridPosition> walls = new HashSet<GridPosition>(wallPositions);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridPosition position = new GridPosition(x, y);
                    tiles[position] = new TileData(position, !walls.Contains(position));
                    tileCenters[position] = origin + new Vector3(position.X * cellSize, 0f, position.Y * cellSize);
                }
            }
        }

        public GridManager(IReadOnlyDictionary<GridPosition, Vector3> authoredTileCenters, IEnumerable<GridPosition> wallPositions)
        {
            cellSize = 1f;
            origin = Vector3.zero;
            useNearestWorldLookup = true;
            HashSet<GridPosition> walls = new HashSet<GridPosition>(wallPositions);
            int maxX = 0;
            int maxY = 0;

            if (authoredTileCenters == null || authoredTileCenters.Count == 0)
            {
                Debug.LogError("GridManager requires at least one authored tile center.");
                Width = 0;
                Height = 0;
                return;
            }

            foreach (KeyValuePair<GridPosition, Vector3> tileCenter in authoredTileCenters)
            {
                GridPosition position = tileCenter.Key;
                tileCenters[position] = tileCenter.Value;
                tiles[position] = new TileData(position, !walls.Contains(position));
                maxX = Mathf.Max(maxX, position.X);
                maxY = Mathf.Max(maxY, position.Y);
            }

            Width = maxX + 1;
            Height = maxY + 1;
        }

        public bool IsWithinBounds(GridPosition position)
        {
            return tiles.ContainsKey(position);
        }

        public bool TryGetTile(GridPosition position, out TileData tile)
        {
            return tiles.TryGetValue(position, out tile);
        }

        public bool IsWalkable(GridPosition position)
        {
            return TryGetTile(position, out TileData tile) && tile.IsWalkable;
        }

        public bool IsOccupied(GridPosition position)
        {
            return TryGetTile(position, out TileData tile) && tile.Occupant != null;
        }

        public bool CanEnter(GridPosition position)
        {
            return TryGetTile(position, out TileData tile) && tile.CanEnter();
        }

        public bool TryPlaceOccupant(IGridOccupant occupant, GridPosition position)
        {
            if (occupant == null || !CanEnter(position))
            {
                return false;
            }

            TileData tile = tiles[position];
            tile.Occupant = occupant;
            occupant.SetGridPosition(position);
            return true;
        }

        public bool TryMoveOccupant(IGridOccupant occupant, GridPosition targetPosition)
        {
            if (occupant == null || !CanEnter(targetPosition))
            {
                return false;
            }

            if (TryGetTile(occupant.GridPosition, out TileData currentTile) && currentTile.Occupant == occupant)
            {
                currentTile.Occupant = null;
            }

            TileData targetTile = tiles[targetPosition];
            targetTile.Occupant = occupant;
            occupant.SetGridPosition(targetPosition);
            return true;
        }

        public void RemoveOccupant(IGridOccupant occupant)
        {
            if (occupant == null)
            {
                return;
            }

            if (TryGetTile(occupant.GridPosition, out TileData tile) && tile.Occupant == occupant)
            {
                tile.Occupant = null;
            }
        }

        public Vector3 GridToWorld(GridPosition position)
        {
            if (tileCenters.TryGetValue(position, out Vector3 center))
            {
                return center;
            }

            return origin + new Vector3(position.X * cellSize, 0f, position.Y * cellSize);
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            if (useNearestWorldLookup && tileCenters.Count > 0)
            {
                bool foundTile = false;
                GridPosition bestPosition = default;
                float bestDistance = float.PositiveInfinity;

                foreach (KeyValuePair<GridPosition, Vector3> tileCenter in tileCenters)
                {
                    float deltaX = worldPosition.x - tileCenter.Value.x;
                    float deltaZ = worldPosition.z - tileCenter.Value.z;
                    float distance = deltaX * deltaX + deltaZ * deltaZ;
                    if (!foundTile || distance < bestDistance)
                    {
                        foundTile = true;
                        bestDistance = distance;
                        bestPosition = tileCenter.Key;
                    }
                }

                if (foundTile)
                {
                    return bestPosition;
                }
            }

            Vector3 local = worldPosition - origin;
            return new GridPosition(Mathf.RoundToInt(local.x / cellSize), Mathf.RoundToInt(local.z / cellSize));
        }
    }
}
