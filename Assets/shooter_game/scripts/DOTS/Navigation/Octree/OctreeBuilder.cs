// Файл: OctreeBuilder.cs

using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
// Для CollisionWorld, OverlapAabbInput, ColliderCastHit
// Для List<T> во время построения

// Для Debug.Log в этом примере

namespace shooter_game.scripts.DOTS.Navigation.Volume
{
    public static class OctreeBuilder
    {
        // Временная структура данных для узла во время построения.
        // Используем List<NodeBuildData> так как он может изменять размер,
        // а BlobArray<OctreeNode> создается один раз с финальным размером.
        private struct NodeBuildData
        {
            public AABB Bounds;
            public OctreeNodeType NodeType;
            public int ParentIndex;
            public int Depth;
            public int ListIndex; // Индекс этого узла в списке buildNodes

            // Индексы дочерних узлов в списке buildNodes.
            // Будет преобразовано в ChildrenStartIndex при финальной сборке BlobAsset.
            public FixedList128Bytes<int> ChildGlobalIndices; // Max 8 children * 4 bytes/int = 32 bytes. Fits.

            public NodeBuildData(AABB bounds, int parentIndex, int depth, int listIndex)
            {
                Bounds = bounds;
                NodeType = OctreeNodeType.Branch; // Предполагаем ветвь по умолчанию
                ParentIndex = parentIndex;
                Depth = depth;
                ListIndex = listIndex;
                ChildGlobalIndices = new FixedList128Bytes<int>();
            }
        }
        
        public static BlobAssetReference<OctreeBlobAsset> Build(
            AABB rootBounds,
            float minNodeSize,
            int maxDepth,
            in CollisionWorld collisionWorld, // Передаем CollisionWorld для запросов
            LayerMask obstacleLayerMask,      // Маска слоев для определения препятствий
            Allocator allocatorForResult = Allocator.Persistent)
        {
            var buildNodes = new List<NodeBuildData>();

            // Корень дерева всегда по индексу 0
            buildNodes.Add(new NodeBuildData(rootBounds, -1, 0, 0));

            // Используем очередь для итеративного построения (избегаем переполнения стека при глубокой рекурсии)
            var processingQueue = new Queue<int>();
            processingQueue.Enqueue(0); // Начинаем с корневого узла (индекс 0)

            NativeList<int> tempHitsContainer = new NativeList<int>(1, Allocator.Temp); // Переиспользуемый контейнер

            while (processingQueue.Count > 0)
            {
                int currentNodeListIndex = processingQueue.Dequeue();
                NodeBuildData currentNodeData = buildNodes[currentNodeListIndex]; // Получаем копию, т.к. это структура

                bool shouldSubdivide = true;
                if (currentNodeData.Depth >= maxDepth)
                {
                    shouldSubdivide = false;
                }

                float3 nodeFullSize = currentNodeData.Bounds.Extents * 2f;
                // Проверяем наименьшую сторону узла
                if (math.min(nodeFullSize.x, math.min(nodeFullSize.y, nodeFullSize.z)) <= minNodeSize)
                {
                    shouldSubdivide = false;
                }

                tempHitsContainer.Clear();
                var overlapInput = new OverlapAabbInput
                {
                    Aabb = new Aabb(){Min = currentNodeData.Bounds.Min, Max = currentNodeData.Bounds.Max},
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u, // Запрос может принадлежать любому слою
                        //CollidesWith = (uint)obstacleLayerMask.value, // Проверяем столкновения с указанными слоями
                        CollidesWith = (uint)obstacleLayerMask.value, // Проверяем столкновения с указанными слоями
                        GroupIndex = 0
                    }
                };
                
                // Фактический вызов OverlapAabb, который заполняет список попаданий
                bool hasObstacle = collisionWorld.OverlapAabb(overlapInput, ref tempHitsContainer);
                //Debug.Log(hasObstacle);
                
                if (!hasObstacle)
                {
                    currentNodeData.NodeType = OctreeNodeType.LeafFree;
                    buildNodes[currentNodeListIndex] = currentNodeData; // Обновляем узел в списке
                    continue; // Это свободный лист, дальше не делим
                }

                // Узел содержит препятствия
                if (!shouldSubdivide)
                {
                    currentNodeData.NodeType = OctreeNodeType.LeafBlocked;
                    buildNodes[currentNodeListIndex] = currentNodeData; // Обновляем узел в списке
                    continue; // Достигнут предел деления, это заблокированный лист
                }

                // Это ветвь, нужно создать 8 дочерних узлов
                currentNodeData.NodeType = OctreeNodeType.Branch;
                float3 parentCenter = currentNodeData.Bounds.Center;
                float3 childExtents = currentNodeData.Bounds.Extents * 0.5f;

                for (int i = 0; i < 8; i++) // 8 октантов
                {
                    float3 offsetDirection = GetOctantDirection(i);
                    float3 childCenter = parentCenter + offsetDirection * childExtents;
                    AABB childBounds = new AABB(){Center = childCenter, Extents = childExtents};

                    int newChildListIndex = buildNodes.Count;
                    var childNodeBuildData = new NodeBuildData(
                        childBounds,
                        currentNodeListIndex, // Родитель этого ребенка
                        currentNodeData.Depth + 1,
                        newChildListIndex);

                    buildNodes.Add(childNodeBuildData);
                    currentNodeData.ChildGlobalIndices.Add(newChildListIndex);
                    processingQueue.Enqueue(newChildListIndex); // Добавляем нового ребенка в очередь на обработку
                }
                buildNodes[currentNodeListIndex] = currentNodeData; // Обновляем родительский узел с информацией о детях
            }
            tempHitsContainer.Dispose();


            // Конвертация List<NodeBuildData> в BlobAsset
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref OctreeBlobAsset octreeRoot = ref blobBuilder.ConstructRoot<OctreeBlobAsset>();

            octreeRoot.RootBounds = rootBounds;
            octreeRoot.MinNodeSize = minNodeSize;
            octreeRoot.MaxDepth = maxDepth;
            
            BlobBuilderArray<OctreeNode> nodeArrayBuilder = blobBuilder.Allocate(ref octreeRoot.Nodes, buildNodes.Count);

            for (int i = 0; i < buildNodes.Count; i++)
            {
                NodeBuildData buildNode = buildNodes[i];
                nodeArrayBuilder[i] = new OctreeNode
                {
                    Bounds = buildNode.Bounds,
                    NodeType = buildNode.NodeType,
                    Depth = buildNode.Depth,
                    ParentIndex = buildNode.ParentIndex,
                    // Если это ветвь и есть дети, ChildrenStartIndex - это индекс первого ребенка.
                    // Дети были добавлены в buildNodes последовательно.
                    ChildrenStartIndex = (buildNode.NodeType == OctreeNodeType.Branch && buildNode.ChildGlobalIndices.Length > 0)
                                         ? buildNode.ChildGlobalIndices[0]
                                         : -1
                };
            }

            BlobAssetReference<OctreeBlobAsset> result = blobBuilder.CreateBlobAssetReference<OctreeBlobAsset>(allocatorForResult);
            blobBuilder.Dispose();

            return result;
        }

        // Направление от центра родителя к центру ребенка для каждого из 8 октантов
        // Компоненты вектора: -1 или 1
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