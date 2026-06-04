namespace DungeonRpg
{
    public readonly struct EnemyDecisionContext
    {
        private readonly GridPosition stepTowardPlayer;
        private readonly bool hasStepTowardPlayer;

        public EnemyDecisionContext(EnemyCharacter enemy, PlayerCharacter player, TurnManager turnManager)
        {
            Enemy = enemy;
            Player = player;
            TurnManager = turnManager;
            GridPosition candidateStep = default;
            hasStepTowardPlayer = enemy != null && player != null && enemy.TryFindBestStepToward(player.GridPosition, out candidateStep);
            stepTowardPlayer = candidateStep;
        }

        public EnemyCharacter Enemy { get; }
        public PlayerCharacter Player { get; }
        public TurnManager TurnManager { get; }
        public bool IsValid => Enemy != null && Player != null;
        public bool PlayerIsAlive => Player != null && Player.IsAlive;
        public bool PlayerIsAdjacent => Enemy != null && Player != null && Enemy.GridPosition.ManhattanDistance(Player.GridPosition) == 1;
        public bool CanStepTowardPlayer => hasStepTowardPlayer;

        public bool TryGetStepTowardPlayer(out GridPosition step)
        {
            step = stepTowardPlayer;
            return hasStepTowardPlayer;
        }
    }
}
