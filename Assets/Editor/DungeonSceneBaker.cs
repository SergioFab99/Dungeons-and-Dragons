using System.Collections.Generic;
using System.IO;
using DungeonRpg;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonRpg.EditorTools
{
    public static class DungeonSceneBaker
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string DecisionGraphPath = "Assets/DecisionGraphs/EnemyDecisionGraph.asset";
        private const string RootName = "Dungeon Scene Root";
        private const int Width = 8;
        private const int Height = 8;
        private const float CellSize = 1.2f;
        private const float DieScale = 1.5f;
        private const float DigitSegmentLength = 0.24f;
        private const float DigitSegmentHeight = 0.18f;
        private const float DigitSegmentThickness = 0.045f;
        private const float DigitSegmentDepth = 0.025f;
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

            DestroyIfFound("Dungeon HUD");
            DestroyIfFound("Movement Die");

            Dictionary<string, Material> materials = CreateMaterials();
            EnemyDecisionGraphAsset decisionGraph = CreateEnemyDecisionGraphAsset();
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
            DynamicDice movementDie = BuildMovementDie(root.transform, materials);
            UIReferences uiReferences = BuildUi(root.transform);
            EnsureEventSystem();

            ConfigureEnemyDecisionGraphs(enemies, decisionGraph);
            ConfigureController(controller, board.transform, player, enemies, treasure, movementDie, turnManager, uiManager);
            ConfigureTurnManager(turnManager, movementDie);
            ConfigureUiManager(uiManager, uiReferences);
            ConfigureCamera();
            ConfigureDynamicDice(movementDie, turnManager, Camera.main);
            ConfigureLight();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Dungeon RPG scene baked: board, pieces, UI, buttons, movement die and references are now saved in SampleScene.");
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
                    tile.AddComponent<GridTileAuthoring>().Configure(position);
                }
            }
        }

        private static void BuildWalls(Transform parent, Dictionary<string, Material> materials)
        {
            foreach (GridPosition position in CreateDefaultWallPositions())
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall {position.X}-{position.Y}";
                wall.transform.SetParent(parent, false);
                wall.transform.position = GridToWorld(position) + new Vector3(0f, 0.55f, 0f);
                wall.transform.localScale = new Vector3(1f, 1.2f, 1f);
                wall.GetComponent<Renderer>().sharedMaterial = materials["wall"];
            }
        }

        private static List<GridPosition> CreateDefaultWallPositions()
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

        private static DynamicDice BuildMovementDie(Transform parent, Dictionary<string, Material> materials)
        {
            GameObject dieObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dieObject.name = "Movement Die";
            dieObject.transform.SetParent(parent, false);
            dieObject.transform.position = new Vector3(5.85f, 0.85f, -4.2f);
            dieObject.transform.localScale = Vector3.one * DieScale;

            Material digitMaterial = materials["wall"];
            AddDiceFace(dieObject.transform, "Dice Face 1", 1, new Vector3(0f, 0.53f, 0f), new Vector3(-90f, 0f, 0f), digitMaterial);
            AddDiceFace(dieObject.transform, "Dice Face 6", 6, new Vector3(0f, -0.53f, 0f), new Vector3(90f, 0f, 0f), digitMaterial);
            AddDiceFace(dieObject.transform, "Dice Face 2", 2, new Vector3(0f, 0f, 0.53f), Vector3.zero, digitMaterial);
            AddDiceFace(dieObject.transform, "Dice Face 5", 5, new Vector3(0f, 0f, -0.53f), new Vector3(0f, 180f, 0f), digitMaterial);
            AddDiceFace(dieObject.transform, "Dice Face 3", 3, new Vector3(-0.53f, 0f, 0f), new Vector3(0f, -90f, 0f), digitMaterial);
            AddDiceFace(dieObject.transform, "Dice Face 4", 4, new Vector3(0.53f, 0f, 0f), new Vector3(0f, 90f, 0f), digitMaterial);

            return dieObject.AddComponent<DynamicDice>();
        }

        private static void AddDiceFace(Transform parent, string name, int value, Vector3 localPosition, Vector3 localEulerAngles, Material digitMaterial)
        {
            GameObject faceObject = new GameObject(name);
            faceObject.transform.SetParent(parent, false);
            faceObject.transform.localPosition = localPosition;
            faceObject.transform.localRotation = Quaternion.Euler(localEulerAngles);

            foreach (DigitSegment segment in GetDigitSegments(value))
            {
                AddDigitSegment(faceObject.transform, segment, digitMaterial);
            }
        }

        private static void AddDigitSegment(Transform parent, DigitSegment segment, Material digitMaterial)
        {
            GameObject segmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segmentObject.name = $"Digit Segment {segment}";
            segmentObject.transform.SetParent(parent, false);
            segmentObject.transform.localPosition = GetSegmentPosition(segment);
            segmentObject.transform.localRotation = Quaternion.identity;
            segmentObject.transform.localScale = GetSegmentScale(segment);

            Renderer renderer = segmentObject.GetComponent<Renderer>();
            renderer.sharedMaterial = digitMaterial;

            Object.DestroyImmediate(segmentObject.GetComponent<Collider>());
        }

        private static IEnumerable<DigitSegment> GetDigitSegments(int value)
        {
            switch (value)
            {
                case 1:
                    return new[] { DigitSegment.UpperRight, DigitSegment.LowerRight };
                case 2:
                    return new[] { DigitSegment.Top, DigitSegment.UpperRight, DigitSegment.Middle, DigitSegment.LowerLeft, DigitSegment.Bottom };
                case 3:
                    return new[] { DigitSegment.Top, DigitSegment.UpperRight, DigitSegment.Middle, DigitSegment.LowerRight, DigitSegment.Bottom };
                case 4:
                    return new[] { DigitSegment.UpperLeft, DigitSegment.UpperRight, DigitSegment.Middle, DigitSegment.LowerRight };
                case 5:
                    return new[] { DigitSegment.Top, DigitSegment.UpperLeft, DigitSegment.Middle, DigitSegment.LowerRight, DigitSegment.Bottom };
                case 6:
                    return new[] { DigitSegment.Top, DigitSegment.UpperLeft, DigitSegment.Middle, DigitSegment.LowerLeft, DigitSegment.LowerRight, DigitSegment.Bottom };
                default:
                    return new[] { DigitSegment.Top, DigitSegment.UpperRight, DigitSegment.LowerRight, DigitSegment.Bottom, DigitSegment.LowerLeft, DigitSegment.UpperLeft };
            }
        }

        private static Vector3 GetSegmentPosition(DigitSegment segment)
        {
            switch (segment)
            {
                case DigitSegment.Top:
                    return new Vector3(0f, -0.22f, DigitSegmentDepth);
                case DigitSegment.Middle:
                    return new Vector3(0f, 0f, DigitSegmentDepth);
                case DigitSegment.Bottom:
                    return new Vector3(0f, 0.22f, DigitSegmentDepth);
                case DigitSegment.UpperLeft:
                    return new Vector3(-0.14f, -0.11f, DigitSegmentDepth);
                case DigitSegment.UpperRight:
                    return new Vector3(0.14f, -0.11f, DigitSegmentDepth);
                case DigitSegment.LowerLeft:
                    return new Vector3(-0.14f, 0.11f, DigitSegmentDepth);
                default:
                    return new Vector3(0.14f, 0.11f, DigitSegmentDepth);
            }
        }

        private static Vector3 GetSegmentScale(DigitSegment segment)
        {
            switch (segment)
            {
                case DigitSegment.Top:
                case DigitSegment.Middle:
                case DigitSegment.Bottom:
                    return new Vector3(DigitSegmentLength, DigitSegmentThickness, DigitSegmentDepth);
                default:
                    return new Vector3(DigitSegmentThickness, DigitSegmentHeight, DigitSegmentDepth);
            }
        }

        private static UIReferences BuildUi(Transform parent)
        {
            Font font = GetDefaultFont();

            GameObject canvasObject = new GameObject("Dungeon HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = CreateUiObject("Hud Panel", canvasObject.transform);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.045f, 0.055f, 0.86f);
            ConfigureRect(panelObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(460f, 510f));

            UIReferences references = new UIReferences
            {
                TitleText = CreateText("TitleText", panelObject.transform, font, "Mazmorra del D20 / D20 Dungeon", 26, FontStyle.Bold, new Vector2(16f, -14f), new Vector2(428f, 36f), TextAnchor.MiddleLeft),
                ObjectiveText = CreateText("ObjectiveText", panelObject.transform, font, "Objetivo: haz clic en el dado, avanza por casillas y alcanza el tesoro. / Objective: click the die, move by tiles and reach the treasure.", 14, FontStyle.Normal, new Vector2(16f, -56f), new Vector2(428f, 58f), TextAnchor.UpperLeft),
                StatusText = CreateText("StatusText", panelObject.transform, font, "Vida: 20/20 | Enemigos: 3 / Health: 20/20 | Enemies: 3", 16, FontStyle.Bold, new Vector2(16f, -124f), new Vector2(428f, 26f), TextAnchor.MiddleLeft),
                TurnText = CreateText("TurnText", panelObject.transform, font, "Turno del jugador / Player turn", 16, FontStyle.Bold, new Vector2(16f, -156f), new Vector2(428f, 26f), TextAnchor.MiddleLeft),
                MovementText = CreateText("MovementText", panelObject.transform, font, "Dado: 0 | Movimientos restantes: 0 / Die: 0 | Moves left: 0", 16, FontStyle.Normal, new Vector2(16f, -188f), new Vector2(428f, 26f), TextAnchor.MiddleLeft),
                MessageText = CreateText("MessageText", panelObject.transform, font, "La aventura comienza. / The adventure begins.", 15, FontStyle.Normal, new Vector2(16f, -224f), new Vector2(428f, 70f), TextAnchor.UpperLeft),
                HistoryText = CreateText("HistoryText", panelObject.transform, font, "Ultimo evento: -- / Last event: --", 14, FontStyle.Italic, new Vector2(16f, -304f), new Vector2(428f, 42f), TextAnchor.UpperLeft)
            };

            references.RollButton = CreateButton("RollButton", "RollButtonText", panelObject.transform, font, "Tirar dado / Roll die", new Vector2(16f, -360f), new Vector2(132f, 38f), out references.RollButtonText);
            references.AttackButton = CreateButton("AttackButton", "AttackButtonText", panelObject.transform, font, "Atacar / Attack", new Vector2(156f, -360f), new Vector2(122f, 38f), out references.AttackButtonText);
            references.EndTurnButton = CreateButton("EndTurnButton", "EndTurnButtonText", panelObject.transform, font, "Terminar / End", new Vector2(286f, -360f), new Vector2(150f, 38f), out references.EndTurnButtonText);
            references.RestartButton = CreateButton("RestartButton", "RestartButtonText", panelObject.transform, font, "Reiniciar / Restart", new Vector2(16f, -408f), new Vector2(132f, 38f), out references.RestartButtonText);

            references.MoveUpButton = CreateButton("MoveUpButton", "MoveUpButtonText", panelObject.transform, font, "W / Up", new Vector2(248f, -408f), new Vector2(58f, 38f), out _);
            references.MoveLeftButton = CreateButton("MoveLeftButton", "MoveLeftButtonText", panelObject.transform, font, "A / Left", new Vector2(184f, -452f), new Vector2(58f, 38f), out _);
            references.MoveDownButton = CreateButton("MoveDownButton", "MoveDownButtonText", panelObject.transform, font, "S / Down", new Vector2(248f, -452f), new Vector2(58f, 38f), out _);
            references.MoveRightButton = CreateButton("MoveRightButton", "MoveRightButtonText", panelObject.transform, font, "D / Right", new Vector2(312f, -452f), new Vector2(58f, 38f), out _);

            return references;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static Text CreateText(string objectName, Transform parent, Font font, string value, int size, FontStyle style, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor anchor)
        {
            GameObject textObject = CreateUiObject(objectName, parent);
            ConfigureRect(textObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = new Color(0.94f, 0.95f, 0.98f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button CreateButton(string objectName, string textName, Transform parent, Font font, string label, Vector2 anchoredPosition, Vector2 sizeDelta, out Text labelText)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent);
            ConfigureRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.25f, 0.34f, 0.96f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.25f, 0.34f, 0.96f);
            colors.highlightedColor = new Color(0.27f, 0.37f, 0.5f, 1f);
            colors.pressedColor = new Color(0.12f, 0.17f, 0.23f, 1f);
            colors.disabledColor = new Color(0.11f, 0.12f, 0.14f, 0.72f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            labelText = CreateText(textName, buttonObject.transform, font, label, 13, FontStyle.Bold, Vector2.zero, sizeDelta, TextAnchor.MiddleCenter);
            labelText.raycastTarget = false;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 9;
            labelText.resizeTextMaxSize = 13;

            RectTransform labelTransform = labelText.GetComponent<RectTransform>();
            ConfigureRect(labelTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static void ConfigureRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static void ConfigureController(DungeonGameBootstrapper controller, Transform boardRoot, PlayerCharacter player, List<EnemyCharacter> enemies, Transform treasure, DynamicDice movementDie, TurnManager turnManager, UIManager uiManager)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("boardRoot").objectReferenceValue = boardRoot;
            serializedController.FindProperty("player").objectReferenceValue = player;
            serializedController.FindProperty("treasure").objectReferenceValue = treasure;
            serializedController.FindProperty("movementDie").objectReferenceValue = movementDie;
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

        private static void ConfigureTurnManager(TurnManager turnManager, DynamicDice movementDie)
        {
            SerializedObject serializedTurnManager = new SerializedObject(turnManager);
            serializedTurnManager.FindProperty("movementDie").objectReferenceValue = movementDie;
            serializedTurnManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDynamicDice(DynamicDice movementDie, TurnManager turnManager, Camera camera)
        {
            SerializedObject serializedDice = new SerializedObject(movementDie);
            serializedDice.FindProperty("turnManager").objectReferenceValue = turnManager;
            serializedDice.FindProperty("targetCamera").objectReferenceValue = camera;
            serializedDice.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureEnemyDecisionGraphs(List<EnemyCharacter> enemies, EnemyDecisionGraphAsset decisionGraph)
        {
            foreach (EnemyCharacter enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                SerializedObject serializedEnemy = new SerializedObject(enemy);
                serializedEnemy.FindProperty("decisionGraph").objectReferenceValue = decisionGraph;
                serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureUiManager(UIManager uiManager, UIReferences references)
        {
            SerializedObject serializedUiManager = new SerializedObject(uiManager);
            serializedUiManager.FindProperty("titleText").objectReferenceValue = references.TitleText;
            serializedUiManager.FindProperty("objectiveText").objectReferenceValue = references.ObjectiveText;
            serializedUiManager.FindProperty("statusText").objectReferenceValue = references.StatusText;
            serializedUiManager.FindProperty("turnText").objectReferenceValue = references.TurnText;
            serializedUiManager.FindProperty("movementText").objectReferenceValue = references.MovementText;
            serializedUiManager.FindProperty("messageText").objectReferenceValue = references.MessageText;
            serializedUiManager.FindProperty("historyText").objectReferenceValue = references.HistoryText;
            serializedUiManager.FindProperty("rollButton").objectReferenceValue = references.RollButton;
            serializedUiManager.FindProperty("rollButtonText").objectReferenceValue = references.RollButtonText;
            serializedUiManager.FindProperty("attackButton").objectReferenceValue = references.AttackButton;
            serializedUiManager.FindProperty("attackButtonText").objectReferenceValue = references.AttackButtonText;
            serializedUiManager.FindProperty("endTurnButton").objectReferenceValue = references.EndTurnButton;
            serializedUiManager.FindProperty("endTurnButtonText").objectReferenceValue = references.EndTurnButtonText;
            serializedUiManager.FindProperty("restartButton").objectReferenceValue = references.RestartButton;
            serializedUiManager.FindProperty("restartButtonText").objectReferenceValue = references.RestartButtonText;
            serializedUiManager.FindProperty("moveUpButton").objectReferenceValue = references.MoveUpButton;
            serializedUiManager.FindProperty("moveDownButton").objectReferenceValue = references.MoveDownButton;
            serializedUiManager.FindProperty("moveLeftButton").objectReferenceValue = references.MoveLeftButton;
            serializedUiManager.FindProperty("moveRightButton").objectReferenceValue = references.MoveRightButton;
            serializedUiManager.ApplyModifiedPropertiesWithoutUndo();
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

        private static EnemyDecisionGraphAsset CreateEnemyDecisionGraphAsset()
        {
            EnsureFolder("Assets/DecisionGraphs");
            EnemyDecisionGraphAsset decisionGraph = AssetDatabase.LoadAssetAtPath<EnemyDecisionGraphAsset>(DecisionGraphPath);
            if (decisionGraph == null)
            {
                decisionGraph = ScriptableObject.CreateInstance<EnemyDecisionGraphAsset>();
                AssetDatabase.CreateAsset(decisionGraph, DecisionGraphPath);
            }

            decisionGraph.ResetToDefaultGraph();
            EditorUtility.SetDirty(decisionGraph);
            return decisionGraph;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string normalizedPath = folderPath.Replace("\\", "/");
            string parentFolder = Path.GetDirectoryName(normalizedPath)?.Replace("\\", "/");
            string folderName = Path.GetFileName(normalizedPath);
            if (string.IsNullOrEmpty(parentFolder))
            {
                parentFolder = "Assets";
            }

            EnsureFolder(parentFolder);
            if (!AssetDatabase.IsValidFolder(normalizedPath))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }

            return font;
        }

        private static void DestroyIfFound(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
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

        private enum DigitSegment
        {
            Top,
            UpperLeft,
            UpperRight,
            Middle,
            LowerLeft,
            LowerRight,
            Bottom
        }

        private sealed class UIReferences
        {
            public Text TitleText;
            public Text ObjectiveText;
            public Text StatusText;
            public Text TurnText;
            public Text MovementText;
            public Text MessageText;
            public Text HistoryText;
            public Button RollButton;
            public Text RollButtonText;
            public Button AttackButton;
            public Text AttackButtonText;
            public Button EndTurnButton;
            public Text EndTurnButtonText;
            public Button RestartButton;
            public Text RestartButtonText;
            public Button MoveUpButton;
            public Button MoveDownButton;
            public Button MoveLeftButton;
            public Button MoveRightButton;
        }
    }
}
