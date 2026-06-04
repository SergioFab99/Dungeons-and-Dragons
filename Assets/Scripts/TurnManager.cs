using System.Collections;
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
        [SerializeField] private DynamicDice movementDie;
        private GridPosition treasurePosition;
        private UIManager uiManager;
        private int turnNumber;
        private string lastSummary = string.Empty;

        public GamePhase Phase { get; private set; } = GamePhase.Setup;
        public PlayerCharacter Player { get; private set; }
        public int CurrentMovementRoll { get; private set; }
        public int RemainingPlayerMoves { get; private set; }
        public bool PlayerHasRolledMovement { get; private set; }
        public bool IsRollingMovementDie { get; private set; }
        public bool IsPlayerTurnActive => Phase == GamePhase.PlayerTurn && Player != null && Player.IsAlive;
        public bool CanRollMovement => IsPlayerTurnActive && !PlayerHasRolledMovement && !IsRollingMovementDie;
        public bool CanPlayerMove => IsPlayerTurnActive && PlayerHasRolledMovement && !IsRollingMovementDie && RemainingPlayerMoves > 0;
        public bool CanPlayerUseAction => IsPlayerTurnActive && PlayerHasRolledMovement && !IsRollingMovementDie;
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

        public void SetMovementDie(DynamicDice die)
        {
            movementDie = die;
            movementDie?.SetTurnManager(this);
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

        public void RequestPlayerRoll()
        {
            if (!CanRollMovement)
            {
                if (IsPlayerTurnActive && !PlayerHasRolledMovement)
                {
                    ReportMessage("rollingDie");
                }

                return;
            }

            StartCoroutine(RollPlayerMovement());
        }

        public void RequestPlayerMove(GridPosition direction)
        {
            if (Player != null)
            {
                Player.TryMoveDirection(direction);
            }
        }

        public void RequestPlayerAttack()
        {
            if (Player != null)
            {
                Player.TryAttackAction();
            }
        }

        public void EndPlayerTurnEarly()
        {
            if (!IsPlayerTurnActive)
            {
                return;
            }

            if (!PlayerHasRolledMovement)
            {
                ReportMessage("rollFirst");
                uiManager?.UpdateHud(this);
                return;
            }

            ReportMessage("playerEndsTurn");
            Player?.StopAcceptingInput();
            CompleteActorTurn(Player);
        }

        public bool TryConsumePlayerMove(PlayerCharacter playerCharacter, GridPosition targetPosition)
        {
            if (playerCharacter != Player || !IsPlayerTurnActive)
            {
                return false;
            }

            if (!PlayerHasRolledMovement)
            {
                ReportMessage("rollFirst");
                uiManager?.UpdateHud(this);
                return false;
            }

            if (RemainingPlayerMoves <= 0)
            {
                ReportMessage("noMovesLeft");
                uiManager?.UpdateHud(this);
                return false;
            }

            RemainingPlayerMoves--;
            ReportMessage("playerMoved", playerCharacter.DisplayName, targetPosition, RemainingPlayerMoves);

            if (CheckEndConditions())
            {
                return true;
            }

            if (RemainingPlayerMoves <= 0)
            {
                Player.StopAcceptingInput();
                CompleteActorTurn(Player);
            }
            else
            {
                uiManager?.UpdateHud(this);
            }

            return true;
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
                if (Phase == GamePhase.PlayerTurn)
                {
                    PreparePlayerTurn();
                }

                uiManager?.UpdateHud(this);
                actor.BeginTurn(this);
                return;
            }

            RebuildTurnQueue();
            BeginNextTurn();
        }

        private IEnumerator RollPlayerMovement()
        {
            IsRollingMovementDie = true;
            ReportMessage("rollingDie");
            uiManager?.UpdateHud(this);

            int result = 1;
            if (movementDie != null)
            {
                yield return movementDie.Roll(value => result = value);
            }
            else
            {
                yield return new WaitForSeconds(0.35f);
                result = Random.Range(1, 7);
            }

            CurrentMovementRoll = result;
            RemainingPlayerMoves = result;
            PlayerHasRolledMovement = true;
            IsRollingMovementDie = false;
            ReportMessage("rolledMovement", result);
            uiManager?.UpdateHud(this);
        }

        private void PreparePlayerTurn()
        {
            CurrentMovementRoll = 0;
            RemainingPlayerMoves = 0;
            PlayerHasRolledMovement = false;
            IsRollingMovementDie = false;
            movementDie?.ResetVisual();
            ReportMessage("rollPrompt");
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
