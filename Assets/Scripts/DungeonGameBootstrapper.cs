using System.Collections.Generic;
using UnityEngine;

namespace DungeonRpg
{
    public class DungeonGameBootstrapper : MonoBehaviour
    {
        private const int Width = 8;
        private const int Height = 8;
        private const float CellSize = 1.2f;
        private static readonly GridPosition TreasurePosition = new GridPosition(7, 7);

        [SerializeField] private PlayerCharacter player;
        [SerializeField] private List<EnemyCharacter> enemies = new List<EnemyCharacter>();
        [SerializeField] private Transform treasure;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private UIManager uiManager;

        private void Start()
        {
            StartGameFromScene();
        }

        private void StartGameFromScene()
        {
            ResolveSceneReferences();

            if (!ValidateSceneReferences())
            {
                enabled = false;
                return;
            }

            GridManager gridManager = new GridManager(Width, Height, CreateWalls(), CellSize, new Vector3(-4.2f, 0f, -4.2f));
            uiManager.Initialize(turnManager);

            player.Configure("Hero", new CharacterStats(20, 3, 12, 4), new GridPosition(0, 0), gridManager);
            List<EnemyCharacter> livingEnemies = new List<EnemyCharacter>();
            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyCharacter enemy = enemies[index];
                if (enemy == null)
                {
                    continue;
                }

                GridPosition startPosition = GetEnemyStartPosition(index);
                enemy.Configure($"Goblin {index + 1}", new CharacterStats(8, 1, 10, 3), startPosition, gridManager);
                livingEnemies.Add(enemy);
            }

            turnManager.Configure(player, livingEnemies, TreasurePosition, uiManager);
            turnManager.StartGame();
        }

        public static List<GridPosition> CreateWalls()
        {
            return new List<GridPosition>
            {
                new GridPosition(1, 1),
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                new GridPosition(5, 0),
                new GridPosition(5, 1),
                new GridPosition(5, 2),
                new GridPosition(1, 3),
                new GridPosition(2, 3),
                new GridPosition(4, 4),
                new GridPosition(5, 4),
                new GridPosition(6, 4),
                new GridPosition(3, 5)
            };
        }

        private void ResolveSceneReferences()
        {
            if (turnManager == null)
            {
                turnManager = GetComponent<TurnManager>();
            }

            if (uiManager == null)
            {
                uiManager = GetComponent<UIManager>();
            }

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerCharacter>();
            }

            if (enemies.Count == 0)
            {
                enemies.AddRange(FindObjectsByType<EnemyCharacter>(FindObjectsSortMode.None));
            }

            if (treasure == null)
            {
                GameObject treasureObject = GameObject.Find("Golden Treasure");
                treasure = treasureObject != null ? treasureObject.transform : null;
            }
        }

        private bool ValidateSceneReferences()
        {
            bool valid = true;
            valid &= ReportMissing(player, "PlayerCharacter");
            valid &= ReportMissing(turnManager, "TurnManager");
            valid &= ReportMissing(uiManager, "UIManager");
            valid &= ReportMissing(treasure, "Golden Treasure");
            valid &= enemies.Count > 0;

            if (enemies.Count == 0)
            {
                Debug.LogError("Dungeon scene needs at least one EnemyCharacter already placed in the scene.");
            }

            return valid;
        }

        private bool ReportMissing(Object reference, string referenceName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"Dungeon scene is missing required reference: {referenceName}.");
            return false;
        }

        private GridPosition GetEnemyStartPosition(int enemyIndex)
        {
            GridPosition[] positions =
            {
                new GridPosition(4, 1),
                new GridPosition(6, 5),
                new GridPosition(2, 6)
            };

            return positions[Mathf.Clamp(enemyIndex, 0, positions.Length - 1)];
        }
    }
}
