using System.Collections.Generic;
using UnityEngine;

namespace DungeonRpg
{
    [CreateAssetMenu(fileName = "EnemyDecisionGraph", menuName = "Dungeon RPG/Enemy Decision Graph")]
    public sealed class EnemyDecisionGraphAsset : ScriptableObject
    {
        [SerializeField] private string startNodeId = "player-adjacent";
        [SerializeField] private string sourceDescription = "Default enemy decision graph.";
        [SerializeField] private List<EnemyDecisionNodeData> nodes = new List<EnemyDecisionNodeData>();

        private readonly Dictionary<string, EnemyDecisionNodeData> nodeLookup = new Dictionary<string, EnemyDecisionNodeData>();
        private bool lookupDirty = true;

        public IReadOnlyList<EnemyDecisionNodeData> Nodes => nodes;
        public string StartNodeId => startNodeId;
        public string SourceDescription => sourceDescription;

        private void OnEnable()
        {
            lookupDirty = true;
        }

        private void OnValidate()
        {
            lookupDirty = true;
        }

        public EnemyDecisionAction Evaluate(EnemyDecisionContext context)
        {
            if (!context.IsValid)
            {
                return EnemyDecisionAction.Wait;
            }

            if (nodes == null || nodes.Count == 0 || string.IsNullOrEmpty(startNodeId))
            {
                return EvaluateDefault(context);
            }

            EnsureLookup();
            HashSet<string> visitedNodes = new HashSet<string>();
            string currentNodeId = startNodeId;

            while (!string.IsNullOrEmpty(currentNodeId))
            {
                if (!visitedNodes.Add(currentNodeId))
                {
                    Debug.LogWarning($"Enemy decision graph '{name}' contains a loop at node '{currentNodeId}'.");
                    return EnemyDecisionAction.Wait;
                }

                if (!nodeLookup.TryGetValue(currentNodeId, out EnemyDecisionNodeData node))
                {
                    Debug.LogWarning($"Enemy decision graph '{name}' is missing node '{currentNodeId}'.");
                    return EnemyDecisionAction.Wait;
                }

                if (node.Kind == EnemyDecisionNodeKind.Action)
                {
                    return node.Action;
                }

                bool conditionPassed = EvaluateCondition(node.Condition, context);
                currentNodeId = conditionPassed ? node.TrueNextNodeId : node.FalseNextNodeId;
            }

            return EnemyDecisionAction.Wait;
        }

        public void ResetToDefaultGraph()
        {
            Initialize(CreateDefaultNodes(), "player-adjacent", "Default Graph Toolkit enemy decision flow.");
        }

        public void Initialize(IEnumerable<EnemyDecisionNodeData> newNodes, string newStartNodeId, string newSourceDescription)
        {
            nodes = new List<EnemyDecisionNodeData>(newNodes);
            startNodeId = newStartNodeId;
            sourceDescription = newSourceDescription;
            lookupDirty = true;
        }

        public static EnemyDecisionAction EvaluateDefault(EnemyDecisionContext context)
        {
            if (!context.PlayerIsAlive)
            {
                return EnemyDecisionAction.Wait;
            }

            if (context.PlayerIsAdjacent)
            {
                return EnemyDecisionAction.AttackPlayer;
            }

            return context.CanStepTowardPlayer ? EnemyDecisionAction.MoveTowardPlayer : EnemyDecisionAction.Wait;
        }

        public static List<EnemyDecisionNodeData> CreateDefaultNodes()
        {
            return new List<EnemyDecisionNodeData>
            {
                new EnemyDecisionNodeData(
                    "player-adjacent",
                    "Is the player adjacent?",
                    EnemyDecisionNodeKind.Condition,
                    EnemyDecisionCondition.PlayerIsAdjacent,
                    EnemyDecisionAction.Wait,
                    "attack-player",
                    "can-step-toward-player"),
                new EnemyDecisionNodeData(
                    "can-step-toward-player",
                    "Can step toward the player?",
                    EnemyDecisionNodeKind.Condition,
                    EnemyDecisionCondition.CanStepTowardPlayer,
                    EnemyDecisionAction.Wait,
                    "move-toward-player",
                    "wait"),
                new EnemyDecisionNodeData(
                    "attack-player",
                    "Attack player",
                    EnemyDecisionNodeKind.Action,
                    EnemyDecisionCondition.Always,
                    EnemyDecisionAction.AttackPlayer,
                    string.Empty,
                    string.Empty),
                new EnemyDecisionNodeData(
                    "move-toward-player",
                    "Move toward player",
                    EnemyDecisionNodeKind.Action,
                    EnemyDecisionCondition.Always,
                    EnemyDecisionAction.MoveTowardPlayer,
                    string.Empty,
                    string.Empty),
                new EnemyDecisionNodeData(
                    "wait",
                    "Wait",
                    EnemyDecisionNodeKind.Action,
                    EnemyDecisionCondition.Always,
                    EnemyDecisionAction.Wait,
                    string.Empty,
                    string.Empty)
            };
        }

        private static bool EvaluateCondition(EnemyDecisionCondition condition, EnemyDecisionContext context)
        {
            switch (condition)
            {
                case EnemyDecisionCondition.Always:
                    return true;
                case EnemyDecisionCondition.PlayerIsAlive:
                    return context.PlayerIsAlive;
                case EnemyDecisionCondition.PlayerIsAdjacent:
                    return context.PlayerIsAdjacent;
                case EnemyDecisionCondition.CanStepTowardPlayer:
                    return context.CanStepTowardPlayer;
                default:
                    return false;
            }
        }

        private void EnsureLookup()
        {
            if (!lookupDirty)
            {
                return;
            }

            nodeLookup.Clear();
            foreach (EnemyDecisionNodeData node in nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }

                nodeLookup[node.Id] = node;
            }

            lookupDirty = false;
        }
    }
}
