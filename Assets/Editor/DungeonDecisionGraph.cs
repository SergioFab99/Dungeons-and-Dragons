using System;
using System.Collections.Generic;
using System.Linq;
using DungeonRpg;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace DungeonRpg.EditorTools.DecisionGraphs
{
    [Serializable]
    [Graph(AssetExtension)]
    internal sealed class DungeonDecisionGraph : Graph
    {
        internal const string AssetExtension = "dungeondg";

        [MenuItem("Assets/Create/Dungeon RPG/Enemy Decision Graph (Graph Toolkit)")]
        private static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<DungeonDecisionGraph>("Enemy Decision Graph");
        }

        public override void OnGraphChanged(GraphLogger infos)
        {
            base.OnGraphChanged(infos);
            List<EnemyDecisionStartNode> startNodes = GetNodes().OfType<EnemyDecisionStartNode>().ToList();

            if (startNodes.Count == 0)
            {
                infos.LogError("Add one EnemyDecisionStartNode to choose the first enemy decision.", this);
                return;
            }

            foreach (EnemyDecisionStartNode extraStartNode in startNodes.Skip(1))
            {
                infos.LogWarning("Only the first EnemyDecisionStartNode will be used.", extraStartNode);
            }

            foreach (EnemyDecisionConditionNode conditionNode in GetNodes().OfType<EnemyDecisionConditionNode>())
            {
                if (!conditionNode.GetOutputPortByName(EnemyDecisionConditionNode.TruePortName).isConnected)
                {
                    infos.LogWarning("Connect the True decision output.", conditionNode);
                }

                if (!conditionNode.GetOutputPortByName(EnemyDecisionConditionNode.FalsePortName).isConnected)
                {
                    infos.LogWarning("Connect the False decision output.", conditionNode);
                }
            }
        }
    }

    [Serializable]
    [UseWithGraph(typeof(DungeonDecisionGraph))]
    internal abstract class EnemyDecisionNode : Node
    {
        public const string FlowPortName = "Flow";

        protected void AddInputFlowPort(IPortDefinitionContext context)
        {
            context.AddInputPort(FlowPortName)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        protected void AddOutputFlowPort(IPortDefinitionContext context, string portName = FlowPortName)
        {
            context.AddOutputPort(portName)
                .WithDisplayName(portName == FlowPortName ? string.Empty : portName)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }

    [Serializable]
    [UseWithGraph(typeof(DungeonDecisionGraph))]
    internal sealed class EnemyDecisionStartNode : EnemyDecisionNode
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddOutputFlowPort(context);
        }
    }

    [Serializable]
    [UseWithGraph(typeof(DungeonDecisionGraph))]
    internal sealed class EnemyDecisionConditionNode : EnemyDecisionNode
    {
        public const string ConditionPortName = "Condition";
        public const string TruePortName = "True";
        public const string FalsePortName = "False";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputFlowPort(context);

            context.AddInputPort<EnemyDecisionCondition>(ConditionPortName)
                .WithDisplayName("Condition")
                .WithDefaultValue(EnemyDecisionCondition.PlayerIsAdjacent)
                .Build();

            AddOutputFlowPort(context, TruePortName);
            AddOutputFlowPort(context, FalsePortName);
        }
    }

    [Serializable]
    [UseWithGraph(typeof(DungeonDecisionGraph))]
    internal sealed class EnemyDecisionActionNode : EnemyDecisionNode
    {
        public const string ActionPortName = "Action";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputFlowPort(context);

            context.AddInputPort<EnemyDecisionAction>(ActionPortName)
                .WithDisplayName("Action")
                .WithDefaultValue(EnemyDecisionAction.Wait)
                .Build();
        }
    }
}
