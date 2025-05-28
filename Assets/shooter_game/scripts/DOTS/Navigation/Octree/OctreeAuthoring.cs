using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation.Volume
{
    // Пример Authoring компонента для запуска построения (для удобства тестирования)
    // Создайте пустой GameObject в сцене и добавьте этот скрипт.
    
    public class OctreeAuthoring : MonoBehaviour
    {
        public Vector3 WorldCenter = Vector3.zero;
        public Vector3 WorldExtents = new Vector3(50, 10, 50);
        public float MinNodeSize = 1.0f;
        public int MaxDepth = 5;
        public LayerMask ObstacleLayerMask = 1; // Обычно "Default"

        class OctreeAuthoringBaker : Baker<OctreeAuthoring>
        {
            public override void Bake(OctreeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BuildOctreeRequest()); 
                // Также можно добавить компонент с параметрами, если они не хардкодятся в системе
                AddComponent(entity, new OctreeBuildParams
                {
                    WorldBounds = new AABB(){Center = authoring.WorldCenter, Extents = authoring.WorldExtents},
                    MinNodeSize = authoring.MinNodeSize,
                    MaxDepth = authoring.MaxDepth,
                    ObstacleLayerMaskValue = authoring.ObstacleLayerMask.value
                });
            }
        }
    }
}