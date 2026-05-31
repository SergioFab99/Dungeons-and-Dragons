using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DungeonRpg
{
    public class UIManager : MonoBehaviour
    {
        private readonly Dictionary<string, BilingualText> texts = new Dictionary<string, BilingualText>();
        private Text titleText;
        private Text objectiveText;
        private Text statusText;
        private Text turnText;
        private Text messageText;
        private Text historyText;
        private Text restartButtonText;
        private Font defaultFont;

        public void Initialize(TurnManager turnManager)
        {
            RegisterTexts();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();

            Canvas canvas = CreateCanvas();
            GameObject panel = CreatePanel(canvas.transform, "HudPanel", new Vector2(12f, -12f), new Vector2(430f, 260f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            titleText = CreateText(panel.transform, "TitleText", 22, FontStyle.Bold, new Vector2(12f, -10f), new Vector2(406f, 34f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            objectiveText = CreateText(panel.transform, "ObjectiveText", 14, FontStyle.Normal, new Vector2(12f, -48f), new Vector2(406f, 44f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            statusText = CreateText(panel.transform, "StatusText", 16, FontStyle.Bold, new Vector2(12f, -96f), new Vector2(406f, 30f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            turnText = CreateText(panel.transform, "TurnText", 15, FontStyle.Normal, new Vector2(12f, -130f), new Vector2(406f, 30f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            messageText = CreateText(panel.transform, "MessageText", 14, FontStyle.Normal, new Vector2(12f, -164f), new Vector2(406f, 54f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            historyText = CreateText(panel.transform, "HistoryText", 12, FontStyle.Italic, new Vector2(12f, -222f), new Vector2(406f, 30f), new Vector2(0f, 1f), new Vector2(0f, 1f));

            CreateRestartButton(canvas.transform, turnManager);

            titleText.text = texts["title"].Display();
            objectiveText.text = texts["objective"].Display();
            messageText.text = texts["intro"].Display();
        }

        public string ShowMessage(string key, params object[] values)
        {
            string message;
            if (texts.TryGetValue(key, out BilingualText text))
            {
                message = values != null && values.Length > 0 ? text.Format(values) : text.Display();
            }
            else
            {
                message = key;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            return message;
        }

        public void UpdateHud(TurnManager turnManager)
        {
            if (turnManager == null || turnManager.Player == null)
            {
                return;
            }

            statusText.text = texts["status"].Format(turnManager.Player.CurrentHealth, turnManager.Player.MaxHealth, turnManager.Enemies.Count);
            turnText.text = GetPhaseText(turnManager.Phase);
            historyText.text = BuildHistoryText(turnManager);

            if (restartButtonText != null)
            {
                restartButtonText.text = texts["restart"].Display();
            }
        }

        private void RegisterTexts()
        {
            texts.Clear();
            texts["title"] = new BilingualText("Mazmorra del D20", "D20 Dungeon");
            texts["objective"] = new BilingualText("Objetivo: alcanza el tesoro dorado o derrota a todos los enemigos.", "Objective: reach the golden treasure or defeat every enemy.");
            texts["intro"] = new BilingualText("La aventura comienza. El héroe entra a la mazmorra.", "The adventure begins. The hero enters the dungeon.");
            texts["status"] = new BilingualText("Vida: {0}/{1} | Enemigos: {2}", "Health: {0}/{1} | Enemies: {2}");
            texts["playerTurn"] = new BilingualText("Turno del jugador", "Player turn");
            texts["enemyTurn"] = new BilingualText("Turno de los enemigos", "Enemy turn");
            texts["win"] = new BilingualText("Victoria: la mazmorra ha sido conquistada.", "Victory: the dungeon has been conquered.");
            texts["lose"] = new BilingualText("Derrota: el héroe cayó en la mazmorra.", "Defeat: the hero fell in the dungeon.");
            texts["blockedMove"] = new BilingualText("Movimiento bloqueado: esa casilla no está disponible.", "Blocked move: that tile is not available.");
            texts["playerMoved"] = new BilingualText("{0} se mueve a {1}.", "{0} moves to {1}.");
            texts["enemyMoved"] = new BilingualText("{0} avanza a {1}.", "{0} advances to {1}.");
            texts["enemyWaits"] = new BilingualText("{0} espera porque no puede avanzar.", "{0} waits because it cannot advance.");
            texts["noAdjacentEnemy"] = new BilingualText("No hay enemigos adyacentes para atacar.", "There are no adjacent enemies to attack.");
            texts["attackHit"] = new BilingualText("{0} golpea a {1}. D20: {2}, total: {3}, daño: {4}.", "{0} hits {1}. D20: {2}, total: {3}, damage: {4}.");
            texts["attackHitDefeated"] = new BilingualText("{0} derrota a {1}. D20: {2}, total: {3}, daño: {4}.", "{0} defeats {1}. D20: {2}, total: {3}, damage: {4}.");
            texts["attackMiss"] = new BilingualText("{0} falla contra {1}. D20: {2}, total: {3}.", "{0} misses {1}. D20: {2}, total: {3}.");
            texts["defeated"] = new BilingualText("{0} ha sido derrotado.", "{0} has been defeated.");
            texts["restart"] = new BilingualText("Reiniciar", "Restart");
            texts["history"] = new BilingualText("Último evento: {0}", "Last event: {0}");
        }

        private string GetPhaseText(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.PlayerTurn:
                    return texts["playerTurn"].Display();
                case GamePhase.EnemyTurn:
                    return texts["enemyTurn"].Display();
                case GamePhase.Win:
                    return texts["win"].Display();
                case GamePhase.Lose:
                    return texts["lose"].Display();
                default:
                    return texts["intro"].Display();
            }
        }

        private string BuildHistoryText(TurnManager turnManager)
        {
            foreach (GameStateSnapshot snapshot in turnManager.History)
            {
                string summary = string.IsNullOrEmpty(snapshot.Summary) ? snapshot.ActorName : snapshot.Summary;
                return texts["history"].Format(summary);
            }

            return string.Empty;
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Dungeon HUD");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private GameObject CreatePanel(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = new GameObject(objectName);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.09f, 0.86f);
            return panel;
        }

        private Text CreateText(Transform parent, string objectName, int fontSize, FontStyle style, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private void CreateRestartButton(Transform parent, TurnManager turnManager)
        {
            GameObject buttonObject = new GameObject("RestartButton");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-16f, 16f);
            rect.sizeDelta = new Vector2(180f, 52f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.34f, 0.55f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(turnManager.RestartGame);

            restartButtonText = CreateText(buttonObject.transform, "RestartButtonText", 16, FontStyle.Bold, new Vector2(0f, 0f), new Vector2(180f, 52f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            RectTransform textRect = restartButtonText.GetComponent<RectTransform>();
            textRect.pivot = new Vector2(0.5f, 0.5f);
            restartButtonText.alignment = TextAnchor.MiddleCenter;
            restartButtonText.text = texts["restart"].Display();
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private Font LoadDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans" }, 16);
            }

            return font;
        }
    }
}
