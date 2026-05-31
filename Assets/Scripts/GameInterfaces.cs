namespace DungeonRpg
{
    public interface IDamageable
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        bool IsAlive { get; }
        void TakeDamage(int damage);
    }

    public interface IGridOccupant
    {
        GridPosition GridPosition { get; }
        bool BlocksMovement { get; }
        void SetGridPosition(GridPosition position);
    }

    public interface ITurnActor
    {
        bool CanAct { get; }
        void BeginTurn(TurnManager turnManager);
    }
}
