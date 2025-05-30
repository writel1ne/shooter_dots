using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation.Octree
{
    public static class OctreeBuilder
    {
        public static BlobAssetReference<OctreeBlobAsset> Build(
            AABB rootBounds,
            float minNodeSize,
            int maxDepth,
            in CollisionWorld collisionWorld,
            LayerMask obstacleLayerMask,
            Allocator allocatorForResult = Allocator.Persistent)
        {
            var buildNodes = new List<NodeBuildData>();

            buildNodes.Add(new NodeBuildData(rootBounds, -1, 0, 0));

            var processingQueue = new Queue<int>();
            processingQueue.Enqueue(0);

            var tempHitsContainer = new NativeList<int>(1, Allocator.Temp);

            while (processingQueue.Count > 0)
            {
                var currentNodeListIndex = processingQueue.Dequeue();
                var currentNodeData = buildNodes[currentNodeListIndex];

                var shouldSubdivide = true;
                if (currentNodeData.Depth >= maxDepth) shouldSubdivide = false;

                var nodeFullSize = currentNodeData.Bounds.Extents * 2f;
                if (math.min(nodeFullSize.x, math.min(nodeFullSize.y, nodeFullSize.z)) <= minNodeSize)
                    shouldSubdivide = false;

                tempHitsContainer.Clear();
                var overlapInput = new OverlapAabbInput
                {
                    Aabb = new Aabb { Min = currentNodeData.Bounds.Min, Max = currentNodeData.Bounds.Max },
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = (uint)obstacleLayerMask.value,
                        GroupIndex = 0
                    }
                };

                //bool hasObstacle = collisionWorld.OverlapAabb(overlapInput, ref tempHitsContainer);
                var hasObstacle = UnityEngine.Physics.OverlapBox(overlapInput.Aabb.Center, overlapInput.Aabb.Extents)
                    .Length > 0;

                if (!hasObstacle)
                {
                    currentNodeData.NodeType = OctreeNodeType.LeafFree;
                    buildNodes[currentNodeListIndex] = currentNodeData;
                    continue;
                }

                if (!shouldSubdivide)
                {
                    currentNodeData.NodeType = OctreeNodeType.LeafBlocked;
                    buildNodes[currentNodeListIndex] = currentNodeData;
                    continue;
                }

                currentNodeData.NodeType = OctreeNodeType.Branch;
                var parentCenter = currentNodeData.Bounds.Center;
                var childExtents = currentNodeData.Bounds.Extents * 0.5f;

                for (var i = 0; i < 8; i++)
                {
                    var offsetDirection = GetOctantDirection(i);
                    var childCenter = parentCenter + offsetDirection * childExtents;
                    var childBounds = new AABB { Center = childCenter, Extents = childExtents };

                    var newChildListIndex = buildNodes.Count;
                    var childNodeBuildData = new NodeBuildData(
                        childBounds,
                        currentNodeListIndex,
                        currentNodeData.Depth + 1,
                        newChildListIndex);

                    buildNodes.Add(childNodeBuildData);
                    currentNodeData.ChildGlobalIndices.Add(newChildListIndex);
                    processingQueue.Enqueue(newChildListIndex);
                }

                buildNodes[currentNodeListIndex] = currentNodeData;
            }

            tempHitsContainer.Dispose();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var octreeRoot = ref blobBuilder.ConstructRoot<OctreeBlobAsset>();

            octreeRoot.RootBounds = rootBounds;
            octreeRoot.MinNodeSize = minNodeSize;
            octreeRoot.MaxDepth = maxDepth;

            var nodeArrayBuilder = blobBuilder.Allocate(ref octreeRoot.Nodes, buildNodes.Count);

            for (var i = 0; i < buildNodes.Count; i++)
            {
                var buildNode = buildNodes[i];
                nodeArrayBuilder[i] = new OctreeNode
                {
                    Bounds = buildNode.Bounds,
                    NodeType = buildNode.NodeType,
                    Depth = buildNode.Depth,
                    ParentIndex = buildNode.ParentIndex,
                    ChildrenStartIndex = buildNode.NodeType == OctreeNodeType.Branch &&
                                         buildNode.ChildGlobalIndices.Length > 0
                        ? buildNode.ChildGlobalIndices[0]
                        : -1
                };
            }

            var result = blobBuilder.CreateBlobAssetReference<OctreeBlobAsset>(allocatorForResult);
            blobBuilder.Dispose();

            return result;
        }

        private static float3 GetOctantDirection(int octantIndex)
        {
            return new float3(
                (octantIndex & 1) == 0 ? -1f : 1f,
                (octantIndex & 2) == 0 ? -1f : 1f,
                (octantIndex & 4) == 0 ? -1f : 1f
            );
        }

        private struct NodeBuildData
        {
            public AABB Bounds;
            public OctreeNodeType NodeType;
            public readonly int ParentIndex;
            public readonly int Depth;
            public int ListIndex;

            public FixedList128Bytes<int> ChildGlobalIndices;

            public NodeBuildData(AABB bounds, int parentIndex, int depth, int listIndex)
            {
                Bounds = bounds;
                NodeType = OctreeNodeType.Branch;
                ParentIndex = parentIndex;
                Depth = depth;
                ListIndex = listIndex;
                ChildGlobalIndices = new FixedList128Bytes<int>();
            }
        }
    }
}