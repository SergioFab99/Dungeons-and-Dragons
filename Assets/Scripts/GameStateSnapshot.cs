namespace DungeonRpg
{
    public class GameStateSnapshot
    {
        public int TurnNumber { get; }
        public GamePhase Phase { get; }
        public string ActorName { get; }
        public int PlayerHealth { get; }
        public int EnemyCount { get; }
        public string Summary { get; }

        public GameStateSnapshot(int turnNumber, GamePhase phase, string actorName, int playerHealth, int enemyCount, string summary)
        {
            TurnNumber = turnNumber;
            Phase = phase;
            ActorName = actorName;
            PlayerHealth = playerHealth;
            EnemyCount = enemyCount;
            Summary = summary;
        }
    }
}
