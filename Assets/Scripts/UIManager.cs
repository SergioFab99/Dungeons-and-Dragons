using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonRpg
{
    public class UIManager : MonoBehaviour
    {
        private readonly Dictionary<string, BilingualText> texts = new Dictionary<string, BilingualText>();

        [SerializeField] private Text titleText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text turnText;
        [SerializeField] private Text movementText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text historyText;

        [SerializeField] private Button rollButton;
        [SerializeField] private Text rollButtonText;
        [SerializeField] private Button attackButton;
        [SerializeField] private Text attackButtonText;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Text endTurnButtonText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Text restartButtonText;

        [SerializeField] private Button moveUpButton;
        [SerializeField] private Button moveDownButton;
        [SerializeField] private Button moveLeftButton;
        [SerializeField] private Button moveRightButton;

        private TurnManager turnManager;

        public void Initialize(TurnManager manager)
        {
            turnManager = manager;
            RegisterTexts();
            ResolveSceneReferences();
            BindButtons();
            ApplyStaticText();
            ShowMessage("intro");
            UpdateHud(turnManager);
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

        public void UpdateHud(TurnManager manager)
        {
            if (manager == null || manager.Player == null)
            {
                return;
            }

            if (statusText != null)
            {
                statusText.text = texts["status"].Format(manager.Player.CurrentHealth, manager.Player.MaxHealth, manager.Enemies.Count);
            }

            if (turnText != null)
            {
                turnText.text = GetPhaseText(manager.Phase);
            }

            if (movementText != null)
            {
                movementText.text = texts["movementStatus"].Format(manager.CurrentMovementRoll, manager.RemainingPlayerMoves);
            }

            if (historyText != null)
            {
                historyText.text = BuildHistoryText(manager);
            }

            bool canRoll = manager.CanRollMovement;
            bool canMove = manager.CanPlayerMove;
            bool canAct = manager.CanPlayerUseAction;
            SetInteractable(rollButton, canRoll);
            SetInteractable(attackButton, canAct);
            SetInteractable(endTurnButton, canAct);
            SetInteractable(moveUpButton, canMove);
            SetInteractable(moveDownButton, canMove);
            SetInteractable(moveLeftButton, canMove);
            SetInteractable(moveRightButton, canMove);
            SetInteractable(restartButton, true);
        }

        private void RegisterTexts()
        {
            texts.Clear();
            texts["title"] = new BilingualText("Mazmorra del D20", "D20 Dungeon");
            texts["objective"] = new BilingualText("Objetivo: tira el dado, avanza por casillas y alcanza el tesoro o derrota enemigos.", "Objective: roll the die, move by tiles and reach the treasure or defeat enemies.");
            texts["intro"] = new BilingualText("La aventura comienza. Haz clic en el dado para moverte.", "The adventure begins. Click the die to move.");
            texts["status"] = new BilingualText("Vida: {0}/{1} | Enemigos: {2}", "Health: {0}/{1} | Enemies: {2}");
            texts["movementStatus"] = new BilingualText("Dado: {0} | Movimientos restantes: {1}", "Die: {0} | Moves left: {1}");
            texts["playerTurn"] = new BilingualText("Turno del jugador", "Player turn");
            texts["enemyTurn"] = new BilingualText("Turno de los enemigos", "Enemy turn");
            texts["win"] = new BilingualText("Victoria: la mazmorra ha sido conquistada.", "Victory: the dungeon has been conquered.");
            texts["lose"] = new BilingualText("Derrota: el heroe cayo en la mazmorra.", "Defeat: the hero fell in the dungeon.");
            texts["rollPrompt"] = new BilingualText("Haz clic en el dado o usa Tirar dado para saber cuantas casillas puedes moverte.", "Click the die or use Roll die to know how many tiles you can move.");
            texts["rollingDie"] = new BilingualText("El dado gira desordenadamente...", "The die is spinning wildly...");
            texts["rolledMovement"] = new BilingualText("El dado marca {0} y mira a la camara. Puedes moverte {0} casillas.", "The die shows {0} and faces the camera. You can move {0} tiles.");
            texts["rollFirst"] = new BilingualText("Primero debes tirar el dado.", "You must roll the die first.");
            texts["noMovesLeft"] = new BilingualText("No quedan movimientos este turno.", "No moves left this turn.");
            texts["blockedMove"] = new BilingualText("Movimiento bloqueado: esa casilla no esta disponible.", "Blocked move: that tile is not available.");
            texts["playerMoved"] = new BilingualText("{0} se mueve a {1}. Movimientos restantes: {2}.", "{0} moves to {1}. Moves left: {2}.");
            texts["playerEndsTurn"] = new BilingualText("El jugador termina su turno.", "The player ends the turn.");
            texts["enemyMoved"] = new BilingualText("{0} avanza a {1}.", "{0} advances to {1}.");
            texts["enemyWaits"] = new BilingualText("{0} espera porque no puede avanzar.", "{0} waits because it cannot advance.");
            texts["noAdjacentEnemy"] = new BilingualText("No hay enemigos adyacentes para atacar.", "There are no adjacent enemies to attack.");
            texts["attackHit"] = new BilingualText("{0} golpea a {1}. D20: {2}, total: {3}, dano: {4}.", "{0} hits {1}. D20: {2}, total: {3}, damage: {4}.");
            texts["attackHitDefeated"] = new BilingualText("{0} derrota a {1}. D20: {2}, total: {3}, dano: {4}.", "{0} defeats {1}. D20: {2}, total: {3}, damage: {4}.");
            texts["attackMiss"] = new BilingualText("{0} falla contra {1}. D20: {2}, total: {3}.", "{0} misses {1}. D20: {2}, total: {3}.");
            texts["restart"] = new BilingualText("Reiniciar", "Restart");
            texts["roll"] = new BilingualText("Tirar dado", "Roll die");
            texts["attack"] = new BilingualText("Atacar", "Attack");
            texts["endTurn"] = new BilingualText("Terminar turno", "End turn");
            texts["history"] = new BilingualText("Ultimo evento: {0}", "Last event: {0}");
        }

        private void ResolveSceneReferences()
        {
            titleText ??= FindText("TitleText");
            objectiveText ??= FindText("ObjectiveText");
            statusText ??= FindText("StatusText");
            turnText ??= FindText("TurnText");
            movementText ??= FindText("MovementText");
            messageText ??= FindText("MessageText");
            historyText ??= FindText("HistoryText");

            rollButton ??= FindButton("RollButton");
            rollButtonText ??= FindText("RollButtonText");
            attackButton ??= FindButton("AttackButton");
            attackButtonText ??= FindText("AttackButtonText");
            endTurnButton ??= FindButton("EndTurnButton");
            endTurnButtonText ??= FindText("EndTurnButtonText");
            restartButton ??= FindButton("RestartButton");
            restartButtonText ??= FindText("RestartButtonText");

            moveUpButton ??= FindButton("MoveUpButton");
            moveDownButton ??= FindButton("MoveDownButton");
            moveLeftButton ??= FindButton("MoveLeftButton");
            moveRightButton ??= FindButton("MoveRightButton");

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                Debug.LogError("Dungeon UI needs an EventSystem already placed in the scene hierarchy.");
            }
        }

        private void BindButtons()
        {
            BindButton(rollButton, turnManager.RequestPlayerRoll);
            BindButton(attackButton, turnManager.RequestPlayerAttack);
            BindButton(endTurnButton, turnManager.EndPlayerTurnEarly);
            BindButton(restartButton, turnManager.RestartGame);
            BindButton(moveUpButton, () => turnManager.RequestPlayerMove(GridPosition.Up));
            BindButton(moveDownButton, () => turnManager.RequestPlayerMove(GridPosition.Down));
            BindButton(moveLeftButton, () => turnManager.RequestPlayerMove(GridPosition.Left));
            BindButton(moveRightButton, () => turnManager.RequestPlayerMove(GridPosition.Right));
        }

        private void ApplyStaticText()
        {
            SetText(titleText, texts["title"].Display());
            SetText(objectiveText, texts["objective"].Display());
            SetText(rollButtonText, texts["roll"].Display());
            SetText(attackButtonText, texts["attack"].Display());
            SetText(endTurnButtonText, texts["endTurn"].Display());
            SetText(restartButtonText, texts["restart"].Display());
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

        private string BuildHistoryText(TurnManager manager)
        {
            foreach (GameStateSnapshot snapshot in manager.History)
            {
                string summary = string.IsNullOrEmpty(snapshot.Summary) ? snapshot.ActorName : snapshot.Summary;
                return texts["history"].Format(summary);
            }

            return string.Empty;
        }

        private Text FindText(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            Text text = target != null ? target.GetComponent<Text>() : null;
            if (text == null)
            {
                Debug.LogError($"Scene UI is missing Text component: {objectName}.");
            }

            return text;
        }

        private Button FindButton(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            Button button = target != null ? target.GetComponent<Button>() : null;
            if (button == null)
            {
                Debug.LogError($"Scene UI is missing Button component: {objectName}.");
            }

            return button;
        }

        private void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
