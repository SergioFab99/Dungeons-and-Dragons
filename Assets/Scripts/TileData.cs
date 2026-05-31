namespace DungeonRpg
{
    public class TileData
    {
        public GridPosition Position { get; }
        public bool IsWalkable { get; set; }
        public IGridOccupant Occupant { get; set; }

        public TileData(GridPosition position, bool isWalkable)
        {
            Position = position;
            IsWalkable = isWalkable;
        }

        public bool CanEnter()
        {
            return IsWalkable && (Occupant == null || !Occupant.BlocksMovement);
        }
    }
}
