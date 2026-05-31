using UnityEngine;

namespace DungeonRpg
{
    public abstract class Character : MonoBehaviour, IDamageable, IGridOccupant, ITurnActor
    {
        private GridManager gridManager;

        public string DisplayName { get; private set; }
        public CharacterStats Stats { get; private set; }
        public int CurrentHealth { get; private set; }
        public int MaxHealth => Stats != null ? Stats.MaxHealth : 0;
        public bool IsAlive => CurrentHealth > 0;
        public GridPosition GridPosition { get; private set; }
        public bool BlocksMovement => IsAlive;
        public bool CanAct => IsAlive;

        protected GridManager Grid => gridManager;

        public virtual void Configure(string displayName, CharacterStats stats, GridPosition startPosition, GridManager grid)
        {
            DisplayName = displayName;
            Stats = stats;
            CurrentHealth = stats.MaxHealth;
            gridManager = grid;

            if (!gridManager.TryPlaceOccupant(this, startPosition))
            {
                Debug.LogError($"{displayName} could not be placed at {startPosition}.");
                enabled = false;
                return;
            }

            transform.position = gridManager.GridToWorld(startPosition) + Vector3.up * 0.55f;
        }

        public void SetGridPosition(GridPosition position)
        {
            GridPosition = position;
        }

        public bool TryMove(GridPosition targetPosition)
        {
            if (gridManager == null || !gridManager.TryMoveOccupant(this, targetPosition))
            {
                return false;
            }

            transform.position = gridManager.GridToWorld(targetPosition) + Vector3.up * 0.55f;
            return true;
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || !IsAlive)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        }

        public void RemoveFromGrid()
        {
            gridManager?.RemoveOccupant(this);
        }

        public abstract void BeginTurn(TurnManager turnManager);
    }
}
