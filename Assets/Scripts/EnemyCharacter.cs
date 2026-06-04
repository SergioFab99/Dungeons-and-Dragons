using System.Collections;
using UnityEngine;

namespace DungeonRpg
{
    public class EnemyCharacter : Character
    {
        [SerializeField] private EnemyDecisionGraphAsset decisionGraph;

        public EnemyDecisionGraphAsset DecisionGraph => decisionGraph;

        public void SetDecisionGraph(EnemyDecisionGraphAsset graph)
        {
            decisionGraph = graph;
        }

        public override void BeginTurn(TurnManager turnManager)
        {
            StartCoroutine(TakeEnemyTurn(turnManager));
        }

        public bool TryFindBestStepToward(GridPosition targetPosition, out GridPosition bestStep)
        {
            bestStep = GridPosition;
            int bestDistance = GridPosition.ManhattanDistance(targetPosition);
            foreach (GridPosition direction in GridPosition.CardinalDirections)
            {
                GridPosition candidate = GridPosition + direction;
                int distance = candidate.ManhattanDistance(targetPosition);
                if (distance < bestDistance && CanMoveTo(candidate))
                {
                    bestDistance = distance;
                    bestStep = candidate;
                }
            }

            return bestStep != GridPosition;
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

            EnemyDecisionContext context = new EnemyDecisionContext(this, player, turnManager);
            EnemyDecisionAction decision = decisionGraph != null ? decisionGraph.Evaluate(context) : EnemyDecisionGraphAsset.EvaluateDefault(context);

            switch (decision)
            {
                case EnemyDecisionAction.AttackPlayer:
                    if (context.PlayerIsAdjacent)
                    {
                        turnManager.ResolveAttack(this, player);
                    }
                    else
                    {
                        turnManager.ReportMessage("enemyWaits", DisplayName);
                    }
                    break;
                case EnemyDecisionAction.MoveTowardPlayer:
                    if (context.TryGetStepTowardPlayer(out GridPosition bestStep) && TryMove(bestStep))
                    {
                        turnManager.ReportMessage("enemyMoved", DisplayName, bestStep);
                    }
                    else
                    {
                        turnManager.ReportMessage("enemyWaits", DisplayName);
                    }
                    break;
                default:
                    turnManager.ReportMessage("enemyWaits", DisplayName);
                    break;
            }

            turnManager.CompleteActorTurn(this);
        }
    }
}
