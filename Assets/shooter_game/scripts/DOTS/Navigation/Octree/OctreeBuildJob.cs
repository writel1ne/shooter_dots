using shooter_game.scripts.DOTS.Collisions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation.Octree
{
    [BurstCompile]
    public struct OctreeBuildJob : IJob
    {
        public OctreeBuildParams BuildParams;
        public EntityWorldColliders Colliders;
        public Allocator ResultAllocator;

        public NativeReference<BlobAssetReference<OctreeBlobAsset>> ResultOctreeRef;

        private struct NodeBuildData
        {
            public readonly AABB Bounds;
            public OctreeNodeType NodeType;
            public readonly int ParentListIndex;
            public readonly int Depth;

            public FixedList64Bytes<int> ChildListIndices;

            public NodeBuildData(AABB bounds, int parentListIndex, int depth)
            {
                Bounds = bounds;
                NodeType = OctreeNodeType.Branch;
                ParentListIndex = parentListIndex;
                Depth = depth;
                ChildListIndices = new FixedList64Bytes<int>();
            }
        }

        public void Execute()
        {
            float3 one = new float3(1, 1, 1);
            NativeList<NodeBuildData> buildNodes = new NativeList<NodeBuildData>(4096, Allocator.Temp);
            NativeQueue<int> processingQueue = new NativeQueue<int>(Allocator.Temp);

            buildNodes.Add(new NodeBuildData(BuildParams.WorldBounds, -1, 0));
            processingQueue.Enqueue(0);

            NativeList<int> tempHitsContainer = new NativeList<int>(8, Allocator.Temp);

            //var collidersArray = new NativeReference<NativeArray<OBB>>(Colliders.Colliders.GetValueArray(Allocator.Temp), Allocator.Temp);
            
            while (processingQueue.TryDequeue(out var currentNodeListIndex))
            {
                NodeBuildData currentNodeData = buildNodes[currentNodeListIndex];
                
                bool shouldSubdivide = !(currentNodeData.Depth >= BuildParams.MaxDepth);

                float3 nodeFullSize = currentNodeData.Bounds.Extents * 2f;
                if (math.min(nodeFullSize.x, math.min(nodeFullSize.y, nodeFullSize.z)) <= BuildParams.MinNodeSize)
                    shouldSubdivide = false;

                tempHitsContainer.Clear();
                
                bool hasObstacle =
                    new OBB(currentNodeData.Bounds, Quaternion.identity, one, currentNodeData.Bounds.Center)
                        .Intersects(ref Colliders);

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
                float3 parentCenter = currentNodeData.Bounds.Center;
                float3 childExtents = currentNodeData.Bounds.Extents * 0.5f;

                for (var i = 0; i < 8; i++)
                {
                    float3 offsetDirection = GetOctantDirection(i);
                    float3 childCenter = parentCenter + offsetDirection * childExtents;
                    AABB childBounds = new AABB { Center = childCenter, Extents = childExtents };

                    int newChildListIndex = buildNodes.Length;
                    NodeBuildData childNodeBuildData = new NodeBuildData(
                        childBounds,
                        currentNodeListIndex,
                        currentNodeData.Depth + 1);

                    buildNodes.Add(childNodeBuildData);

                    if (currentNodeData.ChildListIndices.Length < 8)
                        currentNodeData.ChildListIndices.Add(newChildListIndex);
                    processingQueue.Enqueue(newChildListIndex);
                }

                buildNodes[currentNodeListIndex] = currentNodeData;
            }

            //collidersArray.Dispose();
            tempHitsContainer.Dispose();
            processingQueue.Dispose();

            if (buildNodes.Length > 0)
            {
                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var octreeRoot = ref blobBuilder.ConstructRoot<OctreeBlobAsset>();

                octreeRoot.RootBounds = BuildParams.WorldBounds;
                octreeRoot.MinNodeSize = BuildParams.MinNodeSize;
                octreeRoot.MaxDepth = BuildParams.MaxDepth;

                var nodeArrayBuilder = blobBuilder.Allocate(ref octreeRoot.Nodes, buildNodes.Length);

                for (var i = 0; i < buildNodes.Length; i++)
                {
                    var buildNode = buildNodes[i];
                    var childrenStartIndex = -1;
                    if (buildNode is { NodeType: OctreeNodeType.Branch, ChildListIndices: { Length: > 0 } })
                        // Важно: дети теперь расположены подряд.
                        // ChildrenStartIndex будет указывать на ПЕРВОГО ребенка этой ветви в общем массиве узлов.
                        // Мы должны убедиться, что дети каждой ветви добавляются в buildNodes подряд.
                        // В текущей логике (добавление в конец и обработка через очередь) это не гарантировано для ChildrenStartIndex.
                        // Правильнее было бы назначать ChildrenStartIndex позже, когда известны финальные индексы.
                        // Однако, если мы сохраняем порядок добавления, ChildrenStartIndex может быть индексом первого добавленного ребенка.
                        // Для BlobArray, children будут просто следующими 8 элементами.
                        // При плоской структуре массива, ChildrenStartIndex должен быть глобальным индексом первого ребенка.
                        // Если дети ветви всегда добавляются в buildNodes подряд после родителя,
                        // то childrenStartIndex для buildNodes[X] будет X+1 (если X - ветвь).
                        // Но это не так из-за очереди.
                        // Для BlobAsset, дети должны быть смежными.
                        // Текущий код OctreeBlobAsset.TryGetChildNode ожидает, что ChildrenStartIndex + childOctantIndex даст нужный узел.
                        // Это означает, что при создании BlobArray, узлы-дети для каждой ветви должны быть размещены
                        // в nodeArrayBuilder сразу после всех предыдущих узлов, и ChildrenStartIndex должен указывать на них.
                        // Давайте упростим: в buildNodes мы храним индексы в buildNodes.
                        // При конвертации в OctreeNode для BlobAsset мы должны преобразовать эти индексы.
                        // В OctreeBlobAsset ChildrenStartIndex - это глобальный индекс первого ребенка в BlobArray<OctreeNode>.
                        // Перестроим финальное присвоение ChildrenStartIndex.
                        // Список buildNodes сейчас не имеет "правильных" ChildrenStartIndex для финального BlobAsset.
                        // Это сложная часть при переходе от динамического построения к плоскому BlobAsset.
                        // ПРЕДПОЛОЖЕНИЕ ДЛЯ ПРОСТОТЫ (используемое в вашем оригинальном OctreeBuilder):
                        // Дети узла X, добавленные в buildNodes по индексам C1, C2, ..., C8,
                        // будут также находиться по этим индексам в финальном BlobArray.
                        // Тогда ChildrenStartIndex = C1. Это работает, если buildNodes не переупорядочивается.
                        childrenStartIndex = buildNode.ChildListIndices[0];

                    nodeArrayBuilder[i] = new OctreeNode
                    {
                        Bounds = buildNode.Bounds,
                        NodeType = buildNode.NodeType,
                        Depth = buildNode.Depth,
                        ParentIndex = buildNode.ParentListIndex,
                        ChildrenStartIndex = childrenStartIndex
                    };
                }

                if (ResultOctreeRef.IsCreated && ResultOctreeRef.Value.IsCreated)
                {
                    ResultOctreeRef.Value.Dispose();
                }
                
                ResultOctreeRef.Value = blobBuilder.CreateBlobAssetReference<OctreeBlobAsset>(ResultAllocator);
                blobBuilder.Dispose();
            }
            else
            {
                ResultOctreeRef.Value = BlobAssetReference<OctreeBlobAsset>.Null;
            }

            buildNodes.Dispose();
        }

        private static float3 GetOctantDirection(int octantIndex)
        {
            return new float3(
                (octantIndex & 1) == 0 ? -1f : 1f,
                (octantIndex & 2) == 0 ? -1f : 1f,
                (octantIndex & 4) == 0 ? -1f : 1f
            );
        }
    }
}