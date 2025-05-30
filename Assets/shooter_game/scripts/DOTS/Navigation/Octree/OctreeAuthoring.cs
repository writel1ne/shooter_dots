using Unity.Entities;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation.Octree
{
    public class OctreeAuthoring : MonoBehaviour
    {
        public Vector3 WorldCenter = Vector3.zero;
        public Vector3 WorldExtents = new(50, 10, 50);
        public float MinNodeSize = 1.0f;
        public int MaxDepth = 5;
        public LayerMask ObstacleLayerMask = 1;

        private class OctreeAuthoringBaker : Baker<OctreeAuthoring>
        {
            public override void Bake(OctreeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                // AddComponent(entity, new OctreeBuildParams
                // {
                //     WorldBounds = new AABB(){Center = authoring.WorldCenter, Extents = authoring.WorldExtents},
                //     MinNodeSize = authoring.MinNodeSize,
                //     MaxDepth = authoring.MaxDepth,
                //     ObstacleLayerMaskValue = authoring.ObstacleLayerMask.value
                // });
            }
        }
    }
}