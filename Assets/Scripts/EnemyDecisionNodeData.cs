using System;
using UnityEngine;

namespace DungeonRpg
{
    [Serializable]
    public sealed class EnemyDecisionNodeData
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private EnemyDecisionNodeKind kind;
        [SerializeField] private EnemyDecisionCondition condition;
        [SerializeField] private EnemyDecisionAction action;
        [SerializeField] private string trueNextNodeId;
        [SerializeField] private string falseNextNodeId;

        public string Id => id;
        public string DisplayName => displayName;
        public EnemyDecisionNodeKind Kind => kind;
        public EnemyDecisionCondition Condition => condition;
        public EnemyDecisionAction Action => action;
        public string TrueNextNodeId => trueNextNodeId;
        public string FalseNextNodeId => falseNextNodeId;

        public EnemyDecisionNodeData()
        {
        }

        public EnemyDecisionNodeData(
            string id,
            string displayName,
            EnemyDecisionNodeKind kind,
            EnemyDecisionCondition condition,
            EnemyDecisionAction action,
            string trueNextNodeId,
            string falseNextNodeId)
        {
            this.id = id;
            this.displayName = displayName;
            this.kind = kind;
            this.condition = condition;
            this.action = action;
            this.trueNextNodeId = trueNextNodeId;
            this.falseNextNodeId = falseNextNodeId;
        }
    }
}
