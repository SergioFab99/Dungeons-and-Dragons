using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRpg
{
    public class PlayerCharacter : Character
    {
        private TurnManager activeTurnManager;
        private bool acceptsInput;

        public override void BeginTurn(TurnManager turnManager)
        {
            activeTurnManager = turnManager;
            acceptsInput = true;
        }

        private void Update()
        {
            if (activeTurnManager == null)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                activeTurnManager.RestartGame();
                return;
            }

            if (!acceptsInput || activeTurnManager.Phase != GamePhase.PlayerTurn)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                activeTurnManager.RequestPlayerRoll();
                return;
            }

            GridPosition direction;
            if (TryReadMoveDirection(out direction))
            {
                TryMoveDirection(direction);
                return;
            }

            if (WasAttackPressed())
            {
                TryAttackAction();
            }
        }

        private bool TryReadMoveDirection(out GridPosition direction)
        {
            direction = default;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                direction = GridPosition.Up;
                return true;
            }

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                direction = GridPosition.Down;
                return true;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                direction = GridPosition.Left;
                return true;
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                direction = GridPosition.Right;
                return true;
            }

            return false;
        }

        private bool WasAttackPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        public void TryMoveDirection(GridPosition direction)
        {
            if (!activeTurnManager.CanPlayerMove)
            {
                activeTurnManager.ReportMessage(activeTurnManager.PlayerHasRolledMovement ? "noMovesLeft" : "rollFirst");
                return;
            }

            GridPosition target = GridPosition + direction;
            if (!TryMove(target))
            {
                activeTurnManager.ReportMessage("blockedMove");
                return;
            }

            activeTurnManager.TryConsumePlayerMove(this, target);
        }

        public void TryAttackAction()
        {
            if (!activeTurnManager.CanPlayerUseAction)
            {
                activeTurnManager.ReportMessage("rollFirst");
                return;
            }

            EnemyCharacter enemy = activeTurnManager.FindAdjacentEnemy(GridPosition);
            if (enemy == null)
            {
                activeTurnManager.ReportMessage("noAdjacentEnemy");
                return;
            }

            acceptsInput = false;
            activeTurnManager.ResolveAttack(this, enemy);
            activeTurnManager.CompleteActorTurn(this);
        }

        public void StopAcceptingInput()
        {
            acceptsInput = false;
        }
    }
}
