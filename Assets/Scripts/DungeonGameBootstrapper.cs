using System.Collections.Generic;
using UnityEngine;

namespace DungeonRpg
{
    public class DungeonGameBootstrapper : MonoBehaviour
    {
        private const float CharacterHeightOffset = 0.55f;
        private const float TreasureHeightOffset = 0.25f;

        [SerializeField] private Transform boardRoot;
        [SerializeField] private PlayerCharacter player;
        [SerializeField] private List<EnemyCharacter> enemies = new List<EnemyCharacter>();
        [SerializeField] private Transform treasure;
        [SerializeField] private DynamicDice movementDie;
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

            if (!TryBuildSceneGrid(out GridManager gridManager, out GridPosition treasurePosition))
            {
                enabled = false;
                return;
            }

            if (!TryGetStartPosition(gridManager, player.transform, "Hero", out GridPosition playerStartPosition))
            {
                enabled = false;
                return;
            }

            player.Configure("Hero", new CharacterStats(20, 3, 12, 4), playerStartPosition, gridManager);
            List<EnemyCharacter> livingEnemies = new List<EnemyCharacter>();
            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyCharacter enemy = enemies[index];
                if (enemy == null)
                {
                    continue;
                }

                if (!TryGetStartPosition(gridManager, enemy.transform, enemy.name, out GridPosition startPosition))
                {
                    enabled = false;
                    return;
                }

                enemy.Configure($"Goblin {index + 1}", new CharacterStats(8, 1, 10, 3), startPosition, gridManager);
                livingEnemies.Add(enemy);
            }

            turnManager.SetMovementDie(movementDie);
            turnManager.Configure(player, livingEnemies, treasurePosition, uiManager);
            uiManager.Initialize(turnManager);
            turnManager.StartGame();
        }

        private bool TryBuildSceneGrid(out GridManager gridManager, out GridPosition treasurePosition)
        {
            gridManager = null;
            treasurePosition = default;

            if (!TryReadTileCenters(out Dictionary<GridPosition, Vector3> tileCenters))
            {
                return false;
            }

            List<GridPosition> wallPositions = ReadWallPositions(tileCenters);
            gridManager = new GridManager(tileCenters, wallPositions);
            treasurePosition = gridManager.WorldToGrid(treasure.position);

            if (!gridManager.TryGetTile(treasurePosition, out TileData treasureTile))
            {
                Debug.LogError("Treasure is not close to any authored dungeon tile.");
                return false;
            }

            if (!treasureTile.IsWalkable)
            {
                Debug.LogError($"Treasure is on wall tile {treasurePosition}. Move it to a walkable tile.");
                return false;
            }

            treasure.position = gridManager.GridToWorld(treasurePosition) + Vector3.up * TreasureHeightOffset;
            return true;
        }

        private void ResolveSceneReferences()
        {
            if (boardRoot == null)
            {
                GameObject boardObject = GameObject.Find("Dungeon Board");
                boardRoot = boardObject != null ? boardObject.transform : null;
            }

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

            if (movementDie == null)
            {
                movementDie = FindFirstObjectByType<DynamicDice>();
            }
        }

        private bool ValidateSceneReferences()
        {
            bool valid = true;
            valid &= ReportMissing(boardRoot, "Dungeon Board");
            valid &= ReportMissing(player, "PlayerCharacter");
            valid &= ReportMissing(turnManager, "TurnManager");
            valid &= ReportMissing(uiManager, "UIManager");
            valid &= ReportMissing(treasure, "Golden Treasure");
            valid &= ReportMissing(movementDie, "DynamicDice");
            valid &= enemies.Count > 0;

            if (enemies.Count == 0)
            {
                Debug.LogError("Dungeon scene needs at least one EnemyCharacter already placed in the scene.");
            }

            return valid;
        }

        private bool TryReadTileCenters(out Dictionary<GridPosition, Vector3> tileCenters)
        {
            tileCenters = new Dictionary<GridPosition, Vector3>();
            GridTileAuthoring[] authoredTiles = boardRoot.GetComponentsInChildren<GridTileAuthoring>(false);
            if (authoredTiles.Length == 0)
            {
                Debug.LogError("Dungeon Board needs GridTileAuthoring markers on its tile objects.");
                return false;
            }

            foreach (GridTileAuthoring tile in authoredTiles)
            {
                if (tileCenters.ContainsKey(tile.GridPosition))
                {
                    Debug.LogError($"Dungeon Board has more than one tile marked as {tile.GridPosition}.");
                    return false;
                }

                tileCenters[tile.GridPosition] = tile.WorldCenter;
            }

            return true;
        }

        private List<GridPosition> ReadWallPositions(IReadOnlyDictionary<GridPosition, Vector3> tileCenters)
        {
            List<GridPosition> walls = new List<GridPosition>();
            HashSet<GridPosition> uniqueWalls = new HashSet<GridPosition>();
            Transform[] sceneTransforms = boardRoot.GetComponentsInChildren<Transform>(false);
            foreach (Transform sceneTransform in sceneTransforms)
            {
                if (sceneTransform == boardRoot || !sceneTransform.name.StartsWith("Wall"))
                {
                    continue;
                }

                GridPosition wallPosition = FindNearestTile(tileCenters, sceneTransform.position);
                if (uniqueWalls.Add(wallPosition))
                {
                    walls.Add(wallPosition);
                }
            }

            return walls;
        }

        private bool TryGetStartPosition(GridManager gridManager, Transform actorTransform, string actorName, out GridPosition position)
        {
            position = gridManager.WorldToGrid(actorTransform.position);
            if (!gridManager.TryGetTile(position, out TileData tile))
            {
                Debug.LogError($"{actorName} is not close to any authored dungeon tile.");
                return false;
            }

            if (!tile.IsWalkable)
            {
                Debug.LogError($"{actorName} is on wall tile {position}. Move it to a walkable tile.");
                return false;
            }

            if (tile.Occupant != null)
            {
                Debug.LogError($"{actorName} shares tile {position} with another character. Move one piece to a different tile.");
                return false;
            }

            actorTransform.position = gridManager.GridToWorld(position) + Vector3.up * CharacterHeightOffset;
            return true;
        }

        private GridPosition FindNearestTile(IReadOnlyDictionary<GridPosition, Vector3> tileCenters, Vector3 worldPosition)
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

            return bestPosition;
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
    }
}
