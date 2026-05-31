using System.Collections.Generic;
using UnityEngine;

namespace DungeonRpg
{
    public class GridManager
    {
        private readonly Dictionary<GridPosition, TileData> tiles = new Dictionary<GridPosition, TileData>();
        private readonly float cellSize;
        private readonly Vector3 origin;

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyDictionary<GridPosition, TileData> Tiles => tiles;

        public GridManager(int width, int height, IEnumerable<GridPosition> wallPositions, float cellSize, Vector3 origin)
        {
            Width = width;
            Height = height;
            this.cellSize = cellSize;
            this.origin = origin;

            HashSet<GridPosition> walls = new HashSet<GridPosition>(wallPositions);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridPosition position = new GridPosition(x, y);
                    tiles[position] = new TileData(position, !walls.Contains(position));
                }
            }
        }

        public bool IsWithinBounds(GridPosition position)
        {
            return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
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
            return origin + new Vector3(position.X * cellSize, 0f, position.Y * cellSize);
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - origin;
            return new GridPosition(Mathf.RoundToInt(local.x / cellSize), Mathf.RoundToInt(local.z / cellSize));
        }
    }
}
