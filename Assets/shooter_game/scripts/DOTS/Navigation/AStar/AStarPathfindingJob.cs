// Файл: AStarPathfindingJob.cs (или продолжение AStarOctreePathfinding.cs)

using shooter_game.scripts.DOTS.Navigation.Volume;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation
{
    [BurstCompile]
    public struct AStarPathfindingJob : IJob
    {
        [ReadOnly] public BlobAssetReference<OctreeBlobAsset> OctreeRef;
        public float3 StartPosition;
        public float3 EndPosition;

        // Выходной буфер для точек пути (центры узлов Octree)
        public NativeList<float3> ResultPathPoints;

        // Внутренние коллекции, требующие освобождения 
        public NativeList<int> _openListIndices; // Индексы узлов в Octree.Nodes
        public NativeHashMap<int, AStarNodeData> _nodeDataMap; // nodeIndex -> AStarNodeData
        public NativeList<int> _neighborCache; // Для FindWalkableLeafNeighbors

        public void Execute()
        {
            if (!OctreeRef.IsCreated)
            {
                UnityEngine.Debug.LogError("Octree not created for A* job.");
                return;
            }

            ref var octree = ref OctreeRef.Value;

            int startNodeIndex = OctreeUtils.FindLeafNodeAt(StartPosition, ref OctreeRef);
            int endNodeIndex = OctreeUtils.FindLeafNodeAt(EndPosition, ref OctreeRef);

            if (startNodeIndex == -1 || octree.Nodes[startNodeIndex].NodeType != OctreeNodeType.LeafFree)
            {
                UnityEngine.Debug.LogWarning($"A*: Start node at {StartPosition} is invalid or blocked.");
                return;
            }
            if (endNodeIndex == -1 || octree.Nodes[endNodeIndex].NodeType != OctreeNodeType.LeafFree)
            {
                UnityEngine.Debug.LogWarning($"A*: End node at {EndPosition} is invalid or blocked.");
                return;
            }

            if (startNodeIndex == endNodeIndex)
            {
                ResultPathPoints.Add(octree.Nodes[startNodeIndex].Bounds.Center);
                ResultPathPoints.Add(octree.Nodes[endNodeIndex].Bounds.Center); // Или просто конечную точку
                return;
            }

            // Инициализация коллекций (размер можно подобрать опытным путем)
            // Вместо NativeList для openList лучше использовать NativePriorityQueue, если доступна/реализована
            // Сейчас _openListIndices будет простой список, и мы будем искать минимум вручную (неэффективно для больших списков)
            // _openListIndices = new NativeList<int>(octree.Nodes.Length / 4, Allocator.Temp); 
            // _nodeDataMap = new NativeHashMap<int, AStarNodeData>(octree.Nodes.Length / 4, Allocator.Temp);
            // _neighborCache = new NativeList<int>(8, Allocator.Temp); // Обычно не более 8-26 соседей

            var startAStarNode = new AStarNodeData
            {
                NodeIndex = startNodeIndex,
                ParentNodeIndex = -1,
                GCost = 0,
                HCost = Heuristic(octree.Nodes[startNodeIndex].Bounds.Center, octree.Nodes[endNodeIndex].Bounds.Center)
            };
            _openListIndices.Add(startNodeIndex);
            _nodeDataMap.Add(startNodeIndex, startAStarNode);
            
            int iterations = 0;
            int maxIterations = octree.Nodes.Length * 2; // Ограничение на случай ошибок

            while (_openListIndices.Length > 0 && iterations < maxIterations)
            {
                iterations++;
                int currentNodeListIndex = GetNodeWithLowestFCost(_openListIndices, _nodeDataMap);
                int currentNodeIndex = _openListIndices[currentNodeListIndex];
                AStarNodeData currentNodeAStarData = _nodeDataMap[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex)
                {
                    ReconstructPath(currentNodeIndex, startNodeIndex, ref octree, ref _nodeDataMap, ref ResultPathPoints);
                    CleanUp();
                    return;
                }

                _openListIndices.RemoveAtSwapBack(currentNodeListIndex); // Удаляем из open list
                // Пометка как "закрытый" происходит неявным отсутствием в open list и наличием в nodeDataMap

                _neighborCache.Clear();
                OctreeUtils.FindWalkableLeafNeighbors(currentNodeIndex, ref OctreeRef, ref _neighborCache);

                for (int i = 0; i < _neighborCache.Length; i++)
                {
                    int neighborNodeIndex = _neighborCache[i];
                    
                    // Пропускаем, если уже обработан и GCost там лучше (хотя с Admissible Heuristic это не должно быть проблемой)
                    // Для простоты, если он уже в nodeDataMap и НЕ в openList, считаем его "закрытым".
                    // Эта проверка на "закрытость" немного упрощена. Более строгая версия хранила бы closedSet.
                    bool isInOpenList = false;
                    for(int j=0; j < _openListIndices.Length; ++j) if(_openListIndices[j] == neighborNodeIndex) { isInOpenList = true; break;}
                    
                    if (_nodeDataMap.ContainsKey(neighborNodeIndex) && !isInOpenList) 
                    {
                        // Уже был в open list и извлечен, значит "закрыт"
                        continue;
                    }

                    float tentativeGCost = currentNodeAStarData.GCost + Cost(
                        octree.Nodes[currentNodeIndex].Bounds.Center, 
                        octree.Nodes[neighborNodeIndex].Bounds.Center);

                    AStarNodeData neighborAStarData;
                    bool needsAddingToOpenList = true;

                    if (_nodeDataMap.TryGetValue(neighborNodeIndex, out neighborAStarData))
                    {
                        // Узел уже известен (мог быть в open list или добавлен ранее)
                        if (tentativeGCost >= neighborAStarData.GCost)
                        {
                            continue; // Этот путь не лучше
                        }
                        needsAddingToOpenList = !isInOpenList; // Если он уже в open list, не добавляем снова, только обновляем данные
                    }
                    else
                    {
                        // Новый узел
                        neighborAStarData.HCost = Heuristic(
                            octree.Nodes[neighborNodeIndex].Bounds.Center, 
                            octree.Nodes[endNodeIndex].Bounds.Center);
                    }
                    
                    neighborAStarData.NodeIndex = neighborNodeIndex;
                    neighborAStarData.ParentNodeIndex = currentNodeIndex;
                    neighborAStarData.GCost = tentativeGCost;
                    
                    _nodeDataMap[neighborNodeIndex] = neighborAStarData; // Обновляем или добавляем

                    if (needsAddingToOpenList)
                    {
                        _openListIndices.Add(neighborNodeIndex);
                    }
                }
            }
            UnityEngine.Debug.LogWarning($"A*: Path not found or iterations exceeded ({iterations}). Start: {startNodeIndex}, End: {endNodeIndex}");
            CleanUp();
        }

        private void CleanUp()
        {
            if (_openListIndices.IsCreated) _openListIndices.Dispose();
            if (_nodeDataMap.IsCreated) _nodeDataMap.Dispose();
            if (_neighborCache.IsCreated) _neighborCache.Dispose();
        }

        private int GetNodeWithLowestFCost(NativeList<int> openListIndices, NativeHashMap<int, AStarNodeData> nodeDataMap)
        {
            int bestNodeListIndex = 0;
            float lowestFCost = float.MaxValue;

            for (int i = 0; i < openListIndices.Length; i++)
            {
                AStarNodeData nodeData = nodeDataMap[openListIndices[i]];
                if (nodeData.FCost < lowestFCost)
                {
                    lowestFCost = nodeData.FCost;
                    bestNodeListIndex = i;
                }
                // Дополнительная проверка для выбора узла с меньшим HCost при равных FCost (tie-breaking)
                else if (Mathf.Approximately(nodeData.FCost, lowestFCost) && nodeData.HCost < nodeDataMap[openListIndices[bestNodeListIndex]].HCost)
                {
                     bestNodeListIndex = i;
                }
            }
            return bestNodeListIndex;
        }
        
        // Эвристика: Евклидово расстояние
        private float Heuristic(float3 a, float3 b)
        {
            return math.distance(a, b);
        }

        // Стоимость перехода между узлами: Евклидово расстояние между центрами
        private float Cost(float3 a, float3 b)
        {
            return math.distance(a, b);
        }

        private void ReconstructPath(int endNodeIndex, int startNodeIndex, ref OctreeBlobAsset octree,
                                      ref NativeHashMap<int, AStarNodeData> nodeDataMap,
                                      ref NativeList<float3> outputPath)
        {
            PathReconstructionHelper reconstruction = new PathReconstructionHelper(Allocator.Temp, 16);
            int currentNodeIndex = endNodeIndex;

            while (currentNodeIndex != -1 && nodeDataMap.ContainsKey(currentNodeIndex))
            {
                reconstruction.AddNode(currentNodeIndex);
                if (currentNodeIndex == startNodeIndex) break; // Дошли до начала
                
                AStarNodeData nodeData = nodeDataMap[currentNodeIndex];
                currentNodeIndex = nodeData.ParentNodeIndex;
            }
            
            reconstruction.Reverse(); // Путь теперь от старта к цели

            outputPath.Clear();
            if (reconstruction.NodeIndices.Length > 0)
            {
                 // Добавляем начальную позицию для плавного старта, если она не совпадает с центром первого узла
                if (math.distancesq(StartPosition, octree.Nodes[reconstruction.NodeIndices[0]].Bounds.Center) > 0.01f)
                {
                    //outputPath.Add(StartPosition); // Опционально, зависит от того, как будет использоваться путь
                }

                for (int i = 0; i < reconstruction.NodeIndices.Length; i++)
                {
                    outputPath.Add(octree.Nodes[reconstruction.NodeIndices[i]].Bounds.Center);
                }

                // Добавляем конечную позицию, если она не совпадает с центром последнего узла
                if (math.distancesq(EndPosition, octree.Nodes[reconstruction.NodeIndices[reconstruction.NodeIndices.Length-1]].Bounds.Center) > 0.01f)
                {
                    //outputPath.Add(EndPosition); // Опционально
                }
            } 
            else if (startNodeIndex == endNodeIndex) // Если старт и цель в одном узле
            {
                 outputPath.Add(octree.Nodes[startNodeIndex].Bounds.Center);
            }


            reconstruction.Dispose();
        }
    }
}