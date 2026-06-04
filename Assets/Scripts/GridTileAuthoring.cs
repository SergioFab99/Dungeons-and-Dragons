using UnityEngine;

namespace DungeonRpg
{
    public sealed class GridTileAuthoring : MonoBehaviour
    {
        [SerializeField] private GridPosition gridPosition;
        [SerializeField] private float gridPlaneHeightOffset = 0.08f;

        public GridPosition GridPosition => gridPosition;
        public Vector3 WorldCenter => transform.position + Vector3.up * gridPlaneHeightOffset;

        public void Configure(GridPosition position, float planeHeightOffset = 0.08f)
        {
            gridPosition = position;
            gridPlaneHeightOffset = planeHeightOffset;
        }
    }
}
