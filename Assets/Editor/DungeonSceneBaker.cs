using System.Collections.Generic;
using DungeonRpg;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonRpg.EditorTools
{
    public static class DungeonSceneBaker
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string RootName = "Dungeon Scene Root";
        private const int Width = 8;
        private const int Height = 8;
        private const float CellSize = 1.2f;
        private static readonly Vector3 Origin = new Vector3(-4.2f, 0f, -4.2f);
        private static readonly GridPosition TreasurePosition = new GridPosition(7, 7);

        [MenuItem("Tools/Dungeon RPG/Bake Sample Scene")]
        public static void BakeScene()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot);
            }

            GameObject oldRuntimeRoot = GameObject.Find("Dungeon Runtime Root");
            if (oldRuntimeRoot != null)
            {
                Object.DestroyImmediate(oldRuntimeRoot);
            }

            Dictionary<string, Material> materials = CreateMaterials();
            GameObject root = new GameObject(RootName);
            TurnManager turnManager = root.AddComponent<TurnManager>();
            UIManager uiManager = root.AddComponent<UIManager>();
            DungeonGameBootstrapper controller = root.AddComponent<DungeonGameBootstrapper>();

            GameObject board = new GameObject("Dungeon Board");
            board.transform.SetParent(root.transform, false);

            BuildTiles(board.transform, materials);
            BuildWalls(board.transform, materials);
            Transform treasure = BuildTreasure(root.transform, materials);
            PlayerCharacter player = BuildPlayer(root.transform, materials);
            List<EnemyCharacter> enemies = BuildEnemies(root.transform, materials);

            ConfigureController(controller, player, enemies, treasure, turnManager, uiManager);
            ConfigureCamera();
            ConfigureLight();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Dungeon RPG scene baked: board, walls, pieces, controller and materials are now saved in SampleScene.");
        }

        private static void BuildTiles(Transform parent, Dictionary<string, Material> materials)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    GridPosition position = new GridPosition(x, y);
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile {x}-{y}";
                    tile.transform.SetParent(parent, false);
                    tile.transform.position = GridToWorld(position) + new Vector3(0f, -0.08f, 0f);
                    tile.transform.localScale = new Vector3(1.08f, 0.12f, 1.08f);
                    tile.GetComponent<Renderer>().sharedMaterial = (x + y) % 2 == 0 ? materials["floorA"] : materials["floorB"];
                }
            }
        }

        private static void BuildWalls(Transform parent, Dictionary<string, Material> materials)
        {
            foreach (GridPosition position in DungeonGameBootstrapper.CreateWalls())
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall {position.X}-{position.Y}";
                wall.transform.SetParent(parent, false);
                wall.transform.position = GridToWorld(position) + new Vector3(0f, 0.55f, 0f);
                wall.transform.localScale = new Vector3(1f, 1.2f, 1f);
                wall.GetComponent<Renderer>().sharedMaterial = materials["wall"];
            }
        }

        private static Transform BuildTreasure(Transform parent, Dictionary<string, Material> materials)
        {
            GameObject treasure = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            treasure.name = "Golden Treasure";
            treasure.transform.SetParent(parent, false);
            treasure.transform.position = GridToWorld(TreasurePosition) + new Vector3(0f, 0.25f, 0f);
            treasure.transform.localScale = new Vector3(0.55f, 0.28f, 0.55f);
            treasure.GetComponent<Renderer>().sharedMaterial = materials["treasure"];
            return treasure.transform;
        }

        private static PlayerCharacter BuildPlayer(Transform parent, Dictionary<string, Material> materials)
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "Hero";
            playerObject.transform.SetParent(parent, false);
            playerObject.transform.position = GridToWorld(new GridPosition(0, 0)) + Vector3.up * 0.55f;
            playerObject.transform.localScale = new Vector3(0.65f, 0.9f, 0.65f);
            playerObject.GetComponent<Renderer>().sharedMaterial = materials["player"];
            return playerObject.AddComponent<PlayerCharacter>();
        }

        private static List<EnemyCharacter> BuildEnemies(Transform parent, Dictionary<string, Material> materials)
        {
            GridPosition[] positions =
            {
                new GridPosition(4, 1),
                new GridPosition(6, 5),
                new GridPosition(2, 6)
            };

            List<EnemyCharacter> enemies = new List<EnemyCharacter>();
            for (int index = 0; index < positions.Length; index++)
            {
                GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObject.name = $"Goblin {index + 1}";
                enemyObject.transform.SetParent(parent, false);
                enemyObject.transform.position = GridToWorld(positions[index]) + Vector3.up * 0.55f;
                enemyObject.transform.localScale = new Vector3(0.65f, 0.9f, 0.65f);
                enemyObject.GetComponent<Renderer>().sharedMaterial = materials["enemy"];
                enemies.Add(enemyObject.AddComponent<EnemyCharacter>());
            }

            return enemies;
        }

        private static void ConfigureController(DungeonGameBootstrapper controller, PlayerCharacter player, List<EnemyCharacter> enemies, Transform treasure, TurnManager turnManager, UIManager uiManager)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("player").objectReferenceValue = player;
            serializedController.FindProperty("treasure").objectReferenceValue = treasure;
            serializedController.FindProperty("turnManager").objectReferenceValue = turnManager;
            serializedController.FindProperty("uiManager").objectReferenceValue = uiManager;

            SerializedProperty enemiesProperty = serializedController.FindProperty("enemies");
            enemiesProperty.arraySize = enemies.Count;
            for (int index = 0; index < enemies.Count; index++)
            {
                enemiesProperty.GetArrayElementAtIndex(index).objectReferenceValue = enemies[index];
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            EnsureFolder("Assets/Materials");
            return new Dictionary<string, Material>
            {
                ["floorA"] = CreateMaterial("Dungeon Floor A", new Color(0.22f, 0.23f, 0.25f)),
                ["floorB"] = CreateMaterial("Dungeon Floor B", new Color(0.28f, 0.29f, 0.31f)),
                ["wall"] = CreateMaterial("Dungeon Wall", new Color(0.42f, 0.43f, 0.47f)),
                ["player"] = CreateMaterial("Dungeon Player", new Color(0.15f, 0.45f, 0.95f)),
                ["enemy"] = CreateMaterial("Dungeon Enemy", new Color(0.75f, 0.16f, 0.14f)),
                ["treasure"] = CreateMaterial("Dungeon Treasure", new Color(1f, 0.74f, 0.18f))
            };
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            string path = $"Assets/Materials/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        private static Vector3 GridToWorld(GridPosition position)
        {
            return Origin + new Vector3(position.X * CellSize, 0f, position.Y * CellSize);
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.transform.position = new Vector3(0f, 9.6f, -8.2f);
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 6.3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
        }

        private static void ConfigureLight()
        {
            Light light = Object.FindFirstObjectByType<Light>();
            if (light == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            light.intensity = 1.35f;
        }
    }
}
