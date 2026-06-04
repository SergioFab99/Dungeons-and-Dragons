using System.Collections.Generic;
using DungeonRpg;
using NUnit.Framework;
using UnityEngine;

namespace DungeonRpg.Tests
{
    public class EnemyDecisionGraphAssetTests
    {
        private readonly List<Object> objectsToDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object target in objectsToDestroy)
            {
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void Evaluate_ReturnsAttack_WhenPlayerIsAdjacent()
        {
            EnemyDecisionGraphAsset graph = CreateGraph();
            CreateCharacters(new GridPosition(1, 1), new GridPosition(1, 2), new List<GridPosition>(), out EnemyCharacter enemy, out PlayerCharacter player);

            EnemyDecisionAction action = graph.Evaluate(new EnemyDecisionContext(enemy, player, null));

            Assert.AreEqual(EnemyDecisionAction.AttackPlayer, action);
        }

        [Test]
        public void Evaluate_ReturnsMove_WhenEnemyCanStepTowardPlayer()
        {
            EnemyDecisionGraphAsset graph = CreateGraph();
            CreateCharacters(new GridPosition(0, 0), new GridPosition(2, 0), new List<GridPosition>(), out EnemyCharacter enemy, out PlayerCharacter player);

            EnemyDecisionAction action = graph.Evaluate(new EnemyDecisionContext(enemy, player, null));

            Assert.AreEqual(EnemyDecisionAction.MoveTowardPlayer, action);
        }

        [Test]
        public void Evaluate_ReturnsWait_WhenEnemyCannotStepTowardPlayer()
        {
            EnemyDecisionGraphAsset graph = CreateGraph();
            CreateCharacters(new GridPosition(0, 0), new GridPosition(2, 0), new List<GridPosition> { new GridPosition(1, 0) }, out EnemyCharacter enemy, out PlayerCharacter player);

            EnemyDecisionAction action = graph.Evaluate(new EnemyDecisionContext(enemy, player, null));

            Assert.AreEqual(EnemyDecisionAction.Wait, action);
        }

        private EnemyDecisionGraphAsset CreateGraph()
        {
            EnemyDecisionGraphAsset graph = ScriptableObject.CreateInstance<EnemyDecisionGraphAsset>();
            graph.ResetToDefaultGraph();
            objectsToDestroy.Add(graph);
            return graph;
        }

        private void CreateCharacters(GridPosition enemyPosition, GridPosition playerPosition, IEnumerable<GridPosition> walls, out EnemyCharacter enemy, out PlayerCharacter player)
        {
            GridManager grid = new GridManager(3, 3, walls, 1f, Vector3.zero);

            GameObject enemyObject = new GameObject("Enemy");
            objectsToDestroy.Add(enemyObject);
            enemy = enemyObject.AddComponent<EnemyCharacter>();
            enemy.Configure("Enemy", new CharacterStats(8, 1, 10, 3), enemyPosition, grid);

            GameObject playerObject = new GameObject("Player");
            objectsToDestroy.Add(playerObject);
            player = playerObject.AddComponent<PlayerCharacter>();
            player.Configure("Player", new CharacterStats(20, 3, 12, 4), playerPosition, grid);
        }
    }
}
