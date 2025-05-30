using System;
using shooter_game.scripts.DOTS.Collisions;
using shooter_game.scripts.DOTS.Navigation.Octree;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation
{
    [BurstCompile]
    public class OctreeVisualizer : MonoBehaviour
    {
        public float MaxNodeSizeMagnitude = 3;
        public float MinNodeSizeMagnitude = 3;

        public bool drawFree = true;
        public bool drawBlocked = true;
        public bool drawBranch = true;

        [Tooltip("Включить/выключить отрисовку Gizmos для Octree.")]
        public bool EnableVisualization = true;

        [Header("Фильтры видимости узлов")] [Tooltip("Показывать узлы-ветви (Branch nodes).")]
        public bool DrawBranches = true;

        [Tooltip("Цвет для узлов-ветвей.")] public Color BranchColor = new(0f, 0f, 1f, 0.08f);

        [Tooltip("Показывать свободные листовые узлы (LeafFree nodes).")]
        public bool DrawFreeLeaves = true;

        [Tooltip("Цвет для свободных листовых узлов.")]
        public Color FreeLeafColor = new(0f, 1f, 0f, 0.2f);

        [Tooltip("Показывать заблокированные листовые узлы (LeafBlocked nodes).")]
        public bool DrawBlockedLeaves = true;

        [Tooltip("Цвет для заблокированных листовых узлов.")]
        public Color BlockedLeafColor = new(1f, 0f, 0f, 0.2f);

        [Header("Фильтры глубины и типа")]
        [Tooltip("Максимальная глубина узлов для отрисовки (-1 для всех глубин). Корень имеет глубину 0.")]
        public int MaxDrawDepth = -1;

        [Tooltip("Если true, будут отрисованы только листовые узлы (LeafFree и LeafBlocked), игнорируя DrawBranches.")]
        public bool OnlyDrawLeaves;

        private EntityWorldColliders _colliderRefComponent;
        private EntityQuery _colliderReferenceQuery;

        private EntityManager _entityManager;
        private bool _initialized;
        private OctreeReference _octreeRefComponent;
        private EntityQuery _octreeReferenceQuery;

        private void OnDestroy()
        {
            if (_initialized && !_octreeReferenceQuery.IsEmpty) _octreeReferenceQuery.Dispose();
        }

        private void OnDrawGizmos()
        {
            if (!EnableVisualization)
                return;

            InitializeIfNeeded();

            if (!_initialized || !_entityManager.World.IsCreated) return;

            if (_octreeReferenceQuery.IsEmpty) return;

            var octreeEntity = _octreeReferenceQuery.GetSingletonEntity();
            var colliderEntity = _colliderReferenceQuery.GetSingletonEntity();
            if (octreeEntity == Entity.Null) return;

            try
            {
                _octreeRefComponent = _entityManager.GetComponentData<OctreeReference>(octreeEntity);
                _colliderRefComponent = _entityManager.GetComponentData<EntityWorldColliders>(colliderEntity);
            }
            catch (ArgumentException)
            {
                _initialized = false;
                _octreeReferenceQuery.Dispose();
                return;
            }


            ref var octreeAsset = ref _octreeRefComponent.Value.Value;

            if (octreeAsset.Nodes.Length == 0 || _colliderRefComponent.Colliders.Count == 0) return;

            for (var i = 0; i < octreeAsset.Nodes.Length; i += 1)
            {
                ref readonly var node = ref octreeAsset.Nodes[i];
                DrawNodeGizmo(in node);
            }

            var valueArray = _colliderRefComponent.Colliders.GetValueArray(Allocator.Temp);

            foreach (var v in valueArray)
            {
                Gizmos.color = Color.blueViolet;
                v.DrawGizmos(Color.blueViolet);
                //Gizmos.DrawCube(v.Center, v.Extents);
            }

            valueArray.Dispose();
        }

        private void InitializeIfNeeded()
        {
            if (_initialized || !Application.isPlaying) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                _entityManager = world.EntityManager;
                _octreeReferenceQuery = _entityManager.CreateEntityQuery(typeof(OctreeReference));
                _colliderReferenceQuery = _entityManager.CreateEntityQuery(typeof(EntityWorldColliders));
                _initialized = true;
            }
        }

        private void DrawNodeGizmo(in OctreeNode node)
        {
            if (OnlyDrawLeaves && node.NodeType == OctreeNodeType.Branch) return;

            if (MaxDrawDepth >= 0 && node.Depth > MaxDrawDepth) return;

            if (node.Bounds.Size.x > MaxNodeSizeMagnitude || node.Bounds.Size.x < MinNodeSizeMagnitude) return;

            var gizmoColor = Color.clear;
            var shouldDrawNode = false;

            switch (node.NodeType)
            {
                case OctreeNodeType.Branch:
                    if (DrawBranches)
                    {
                        gizmoColor = BranchColor;
                        shouldDrawNode = drawBranch;
                    }

                    break;
                case OctreeNodeType.LeafFree:
                    if (DrawFreeLeaves)
                    {
                        gizmoColor = FreeLeafColor;
                        shouldDrawNode = drawFree;
                    }

                    break;
                case OctreeNodeType.LeafBlocked:
                    if (DrawBlockedLeaves)
                    {
                        gizmoColor = BlockedLeafColor;
                        shouldDrawNode = drawBlocked;
                    }

                    break;
                default:
                    return;
            }

            if (shouldDrawNode)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireCube(node.Bounds.Center, node.Bounds.Size);
            }
        }
    }
}