using System.Collections.Generic;
using System.Linq;
using DungeonRpg;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace DungeonRpg.EditorTools.DecisionGraphs
{
    [ScriptedImporter(1, DungeonDecisionGraph.AssetExtension)]
    internal sealed class DungeonDecisionGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            DungeonDecisionGraph graph = GraphDatabase.LoadGraphForImporter<DungeonDecisionGraph>(ctx.assetPath);
            EnemyDecisionGraphAsset runtimeAsset = ScriptableObject.CreateInstance<EnemyDecisionGraphAsset>();

            if (graph == null || !TryBuildRuntimeGraph(graph, runtimeAsset, ctx.assetPath))
            {
                runtimeAsset.ResetToDefaultGraph();
            }

            ctx.AddObjectToAsset("RuntimeDecisionGraph", runtimeAsset);
            ctx.SetMainObject(runtimeAsset);
        }

        private static bool TryBuildRuntimeGraph(DungeonDecisionGraph graph, EnemyDecisionGraphAsset runtimeAsset, string assetPath)
        {
            EnemyDecisionStartNode startNode = graph.GetNodes().OfType<EnemyDecisionStartNode>().FirstOrDefault();
            INode firstDecisionNode = startNode != null ? GetNextNode(startNode, EnemyDecisionNode.FlowPortName) : null;
            if (firstDecisionNode == null)
            {
                return false;
            }

            Dictionary<INode, string> nodeIds = new Dictionary<INode, string>();
            List<EnemyDecisionNodeData> runtimeNodes = new List<EnemyDecisionNodeData>();
            Queue<INode> pendingNodes = new Queue<INode>();
            HashSet<INode> visitedNodes = new HashSet<INode>();

            pendingNodes.Enqueue(firstDecisionNode);
            string startNodeId = GetNodeId(firstDecisionNode, nodeIds);

            while (pendingNodes.Count > 0)
            {
                INode node = pendingNodes.Dequeue();
                if (!visitedNodes.Add(node))
                {
                    continue;
                }

                switch (node)
                {
                    case EnemyDecisionConditionNode conditionNode:
                        INode trueNode = GetNextNode(conditionNode, EnemyDecisionConditionNode.TruePortName);
                        INode falseNode = GetNextNode(conditionNode, EnemyDecisionConditionNode.FalsePortName);
                        EnqueueIfPresent(trueNode, pendingNodes);
                        EnqueueIfPresent(falseNode, pendingNodes);

                        EnemyDecisionCondition condition = GetInputPortValue(
                            conditionNode.GetInputPortByName(EnemyDecisionConditionNode.ConditionPortName),
                            EnemyDecisionCondition.PlayerIsAdjacent);

                        runtimeNodes.Add(new EnemyDecisionNodeData(
                            GetNodeId(conditionNode, nodeIds),
                            condition.ToString(),
                            EnemyDecisionNodeKind.Condition,
                            condition,
                            EnemyDecisionAction.Wait,
                            trueNode != null ? GetNodeId(trueNode, nodeIds) : string.Empty,
                            falseNode != null ? GetNodeId(falseNode, nodeIds) : string.Empty));
                        break;
                    case EnemyDecisionActionNode actionNode:
                        EnemyDecisionAction action = GetInputPortValue(
                            actionNode.GetInputPortByName(EnemyDecisionActionNode.ActionPortName),
                            EnemyDecisionAction.Wait);

                        runtimeNodes.Add(new EnemyDecisionNodeData(
                            GetNodeId(actionNode, nodeIds),
                            action.ToString(),
                            EnemyDecisionNodeKind.Action,
                            EnemyDecisionCondition.Always,
                            action,
                            string.Empty,
                            string.Empty));
                        break;
                }
            }

            if (runtimeNodes.Count == 0)
            {
                return false;
            }

            runtimeAsset.Initialize(runtimeNodes, startNodeId, $"Imported from Graph Toolkit asset: {assetPath}");
            return true;
        }

        private static INode GetNextNode(INode currentNode, string outputPortName)
        {
            IPort outputPort = currentNode.GetOutputPortByName(outputPortName);
            return outputPort?.firstConnectedPort?.GetNode();
        }

        private static T GetInputPortValue<T>(IPort port, T fallback)
        {
            if (port == null)
            {
                return fallback;
            }

            if (port.isConnected)
            {
                INode connectedNode = port.firstConnectedPort?.GetNode();
                if (connectedNode is IVariableNode variableNode && variableNode.variable.TryGetDefaultValue(out T variableValue))
                {
                    return variableValue;
                }

                if (connectedNode is IConstantNode constantNode && constantNode.TryGetValue(out T constantValue))
                {
                    return constantValue;
                }
            }

            return port.TryGetValue(out T embeddedValue) ? embeddedValue : fallback;
        }

        private static void EnqueueIfPresent(INode node, Queue<INode> queue)
        {
            if (node != null)
            {
                queue.Enqueue(node);
            }
        }

        private static string GetNodeId(INode node, Dictionary<INode, string> nodeIds)
        {
            if (!nodeIds.TryGetValue(node, out string nodeId))
            {
                nodeId = $"node-{nodeIds.Count + 1}";
                nodeIds[node] = nodeId;
            }

            return nodeId;
        }
    }
}
