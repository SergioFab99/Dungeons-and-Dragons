using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonRpg
{
    public class TurnManager : MonoBehaviour
    {
        private readonly List<Character> activeCharacters = new List<Character>();
        private readonly List<EnemyCharacter> enemies = new List<EnemyCharacter>();
        private readonly Queue<ITurnActor> turnQueue = new Queue<ITurnActor>();
        private readonly Stack<GameStateSnapshot> history = new Stack<GameStateSnapshot>();

        private readonly CombatResolver combatResolver = new CombatResolver();
        private GridPosition treasurePosition;
        private UIManager uiManager;
        private int turnNumber;
        private string lastSummary = string.Empty;

        public GamePhase Phase { get; private set; } = GamePhase.Setup;
        public PlayerCharacter Player { get; private set; }
        public IReadOnlyList<Character> ActiveCharacters => activeCharacters;
        public IReadOnlyList<EnemyCharacter> Enemies => enemies;
        public IEnumerable<GameStateSnapshot> History => history;

        public void Configure(PlayerCharacter player, IEnumerable<EnemyCharacter> enemyCharacters, GridPosition treasure, UIManager ui)
        {
            Player = player;
            treasurePosition = treasure;
            uiManager = ui;
            activeCharacters.Clear();
            enemies.Clear();
            turnQueue.Clear();
            history.Clear();

            if (Player == null)
            {
                Debug.LogError("TurnManager requires a player.");
                enabled = false;
                return;
            }

            activeCharacters.Add(Player);
            foreach (EnemyCharacter enemy in enemyCharacters)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemies.Add(enemy);
                activeCharacters.Add(enemy);
            }
        }

        public void StartGame()
        {
            turnNumber = 0;
            Phase = GamePhase.PlayerTurn;
            ReportMessage("intro");
            RebuildTurnQueue();
            BeginNextTurn();
        }

        public static Queue<ITurnActor> BuildTurnQueue(IEnumerable<ITurnActor> actors)
        {
            Queue<ITurnActor> queue = new Queue<ITurnActor>();
            foreach (ITurnActor actor in actors)
            {
                if (actor != null && actor.CanAct)
                {
                    queue.Enqueue(actor);
                }
            }

            return queue;
        }

        public void CompleteActorTurn(ITurnActor actor)
        {
            if (Phase == GamePhase.Win || Phase == GamePhase.Lose)
            {
                return;
            }

            turnNumber++;
            RemoveDefeatedEnemies();
            PushSnapshot(actor);

            if (CheckEndConditions())
            {
                return;
            }

            BeginNextTurn();
        }

        public void ResolveAttack(Character attacker, Character defender)
        {
            if (attacker == null || defender == null || !attacker.IsAlive || !defender.IsAlive)
            {
                return;
            }

            CombatResult result = combatResolver.Resolve(attacker.Stats, defender.Stats);
            defender.TakeDamage(result.Damage);

            if (result.Hit)
            {
                if (defender.IsAlive)
                {
                    ReportMessage("attackHit", attacker.DisplayName, defender.DisplayName, result.DiceRoll, result.TotalAttack, result.Damage);
                }
                else
                {
                    ReportMessage("attackHitDefeated", attacker.DisplayName, defender.DisplayName, result.DiceRoll, result.TotalAttack, result.Damage);
                }
            }
            else
            {
                ReportMessage("attackMiss", attacker.DisplayName, defender.DisplayName, result.DiceRoll, result.TotalAttack);
            }

            if (!defender.IsAlive)
            {
                if (defender is EnemyCharacter enemy)
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            uiManager?.UpdateHud(this);
        }

        public EnemyCharacter FindAdjacentEnemy(GridPosition position)
        {
            foreach (EnemyCharacter enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive && enemy.GridPosition.ManhattanDistance(position) == 1)
                {
                    return enemy;
                }
            }

            return null;
        }

        public void ReportMessage(string key, params object[] values)
        {
            string summary = uiManager != null ? uiManager.ShowMessage(key, values) : key;
            lastSummary = summary;
        }

        public void RestartGame()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        private void BeginNextTurn()
        {
            RemoveDefeatedEnemies();
            if (CheckEndConditions())
            {
                return;
            }

            if (turnQueue.Count == 0)
            {
                RebuildTurnQueue();
            }

            while (turnQueue.Count > 0)
            {
                ITurnActor actor = turnQueue.Dequeue();
                if (actor == null || !actor.CanAct)
                {
                    continue;
                }

                Phase = actor is PlayerCharacter ? GamePhase.PlayerTurn : GamePhase.EnemyTurn;
                uiManager?.UpdateHud(this);
                actor.BeginTurn(this);
                return;
            }

            RebuildTurnQueue();
            BeginNextTurn();
        }

        private void RebuildTurnQueue()
        {
            List<ITurnActor> actors = new List<ITurnActor>();
            if (Player != null && Player.IsAlive)
            {
                actors.Add(Player);
            }

            foreach (EnemyCharacter enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    actors.Add(enemy);
                }
            }

            Queue<ITurnActor> rebuiltQueue = BuildTurnQueue(actors);
            turnQueue.Clear();
            while (rebuiltQueue.Count > 0)
            {
                turnQueue.Enqueue(rebuiltQueue.Dequeue());
            }
        }

        private void RemoveDefeatedEnemies()
        {
            for (int index = enemies.Count - 1; index >= 0; index--)
            {
                EnemyCharacter enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive)
                {
                    if (enemy != null)
                    {
                        enemy.RemoveFromGrid();
                    }

                    enemies.RemoveAt(index);
                    activeCharacters.Remove(enemy);
                }
            }
        }

        private bool CheckEndConditions()
        {
            if (Player == null || !Player.IsAlive)
            {
                Phase = GamePhase.Lose;
                ReportMessage("lose");
                uiManager?.UpdateHud(this);
                return true;
            }

            if (enemies.Count == 0 || Player.GridPosition == treasurePosition)
            {
                Phase = GamePhase.Win;
                ReportMessage("win");
                uiManager?.UpdateHud(this);
                return true;
            }

            return false;
        }

        private void PushSnapshot(ITurnActor actor)
        {
            string actorName = actor is Character character ? character.DisplayName : "Unknown";
            int playerHealth = Player != null ? Player.CurrentHealth : 0;
            history.Push(new GameStateSnapshot(turnNumber, Phase, actorName, playerHealth, enemies.Count, lastSummary));
        }
    }
}
