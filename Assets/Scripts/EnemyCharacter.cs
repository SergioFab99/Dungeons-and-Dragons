using System.Collections;
using UnityEngine;

namespace DungeonRpg
{
    public class EnemyCharacter : Character
    {
        public override void BeginTurn(TurnManager turnManager)
        {
            StartCoroutine(TakeEnemyTurn(turnManager));
        }

        private IEnumerator TakeEnemyTurn(TurnManager turnManager)
        {
            yield return new WaitForSeconds(0.35f);

            if (!IsAlive || turnManager.Phase != GamePhase.EnemyTurn)
            {
                turnManager.CompleteActorTurn(this);
                yield break;
            }

            PlayerCharacter player = turnManager.Player;
            if (player == null || !player.IsAlive)
            {
                turnManager.CompleteActorTurn(this);
                yield break;
            }

            if (GridPosition.ManhattanDistance(player.GridPosition) == 1)
            {
                turnManager.ResolveAttack(this, player);
                turnManager.CompleteActorTurn(this);
                yield break;
            }

            GridPosition bestStep = GridPosition;
            int bestDistance = GridPosition.ManhattanDistance(player.GridPosition);
            foreach (GridPosition direction in GridPosition.CardinalDirections)
            {
                GridPosition candidate = GridPosition + direction;
                int distance = candidate.ManhattanDistance(player.GridPosition);
                if (distance < bestDistance && Grid.CanEnter(candidate))
                {
                    bestDistance = distance;
                    bestStep = candidate;
                }
            }

            if (bestStep != GridPosition)
            {
                TryMove(bestStep);
                turnManager.ReportMessage("enemyMoved", DisplayName, bestStep);
            }
            else
            {
                turnManager.ReportMessage("enemyWaits", DisplayName);
            }

            turnManager.CompleteActorTurn(this);
        }
    }
}
