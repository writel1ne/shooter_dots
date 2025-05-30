using shooter_game.scripts.DOTS.Navigation.Octree;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation
{
    //[BurstCompile]
    public struct AStarPathfindingJob : IJob
    {
        [ReadOnly] public BlobAssetReference<OctreeBlobAsset> OctreeRef;
        public float3 StartPosition;
        public float3 EndPosition;

        public NativeList<float3> ResultPathPoints;

        public NativeList<int> _openListIndices; // Индексы узлов в Octree.Nodes
        public NativeHashMap<int, AStarNodeData> _nodeDataMap; // nodeIndex -> AStarNodeData
        public NativeList<int> _neighborCache; // Для FindWalkableLeafNeighbors

        public void Execute()
        {
            if (!OctreeRef.IsCreated)
            {
                Debug.LogError("Octree not created for A* job.");
                return;
            }

            ref var octree = ref OctreeRef.Value;

            var startNodeIndex = OctreeUtils.FindLeafNodeAt(StartPosition, ref OctreeRef, true);
            var endNodeIndex = OctreeUtils.FindLeafNodeAt(EndPosition, ref OctreeRef);

            if (startNodeIndex == -1 || octree.Nodes[startNodeIndex].NodeType != OctreeNodeType.LeafFree)
            {
                Debug.LogWarning($"A*: Start node at {StartPosition} is invalid or blocked.");
                return;
            }

            if (endNodeIndex == -1 || octree.Nodes[endNodeIndex].NodeType != OctreeNodeType.LeafFree)
            {
                Debug.LogWarning($"A*: End node at {EndPosition} is invalid or blocked.");
                return;
            }

            if (startNodeIndex == endNodeIndex)
            {
                ResultPathPoints.Add(octree.Nodes[startNodeIndex].Bounds.Center);
                ResultPathPoints.Add(octree.Nodes[endNodeIndex].Bounds.Center);
                Debug.LogWarning("A*: startNodeIndex at endNodeIndex");
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

            var iterations = 0;
            var maxIterations = octree.Nodes.Length * 2;

            while (_openListIndices.Length > 0 && iterations < maxIterations)
            {
                iterations++;
                var currentNodeListIndex = GetNodeWithLowestFCost(_openListIndices, _nodeDataMap);
                var currentNodeIndex = _openListIndices[currentNodeListIndex];
                var currentNodeAStarData = _nodeDataMap[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex)
                {
                    ReconstructPath(currentNodeIndex, startNodeIndex, ref octree, ref _nodeDataMap,
                        ref ResultPathPoints);
                    CleanUp();
                    return;
                }

                _openListIndices.RemoveAtSwapBack(currentNodeListIndex);

                _neighborCache.Clear();
                OctreeUtils.FindWalkableLeafNeighbors(currentNodeIndex, ref OctreeRef, ref _neighborCache);

                for (var i = 0; i < _neighborCache.Length; i++)
                {
                    var neighborNodeIndex = _neighborCache[i];

                    // Пропускаем, если уже обработан и GCost там лучше (хотя с Admissible Heuristic это не должно быть проблемой)
                    // Для простоты, если он уже в nodeDataMap и НЕ в openList, считаем его "закрытым".
                    // Эта проверка на "закрытость" немного упрощена. Более строгая версия хранила бы closedSet.
                    var isInOpenList = false;
                    for (var j = 0; j < _openListIndices.Length; ++j)
                        if (_openListIndices[j] == neighborNodeIndex)
                        {
                            isInOpenList = true;
                            break;
                        }

                    if (_nodeDataMap.ContainsKey(neighborNodeIndex) && !isInOpenList) continue;

                    var tentativeGCost = currentNodeAStarData.GCost + Cost(
                        octree.Nodes[currentNodeIndex].Bounds.Center,
                        octree.Nodes[neighborNodeIndex].Bounds.Center);

                    AStarNodeData neighborAStarData;
                    var needsAddingToOpenList = true;

                    if (_nodeDataMap.TryGetValue(neighborNodeIndex, out neighborAStarData))
                    {
                        if (tentativeGCost >= neighborAStarData.GCost) continue;
                        needsAddingToOpenList = !isInOpenList;
                    }
                    else
                    {
                        neighborAStarData.HCost = Heuristic(
                            octree.Nodes[neighborNodeIndex].Bounds.Center,
                            octree.Nodes[endNodeIndex].Bounds.Center);
                    }

                    neighborAStarData.NodeIndex = neighborNodeIndex;
                    neighborAStarData.ParentNodeIndex = currentNodeIndex;
                    neighborAStarData.GCost = tentativeGCost;

                    _nodeDataMap[neighborNodeIndex] = neighborAStarData;

                    if (needsAddingToOpenList) _openListIndices.Add(neighborNodeIndex);
                }
            }

            CleanUp();
        }

        private void CleanUp()
        {
            // if (_openListIndices.IsCreated) _openListIndices.Dispose();
            // if (_nodeDataMap.IsCreated) _nodeDataMap.Dispose();
            // if (_neighborCache.IsCreated) _neighborCache.Dispose();
        }

        private int GetNodeWithLowestFCost(NativeList<int> openListIndices,
            NativeHashMap<int, AStarNodeData> nodeDataMap)
        {
            var bestNodeListIndex = 0;
            var lowestFCost = float.MaxValue;

            for (var i = 0; i < openListIndices.Length; i++)
            {
                var nodeData = nodeDataMap[openListIndices[i]];
                if (nodeData.FCost < lowestFCost)
                {
                    lowestFCost = nodeData.FCost;
                    bestNodeListIndex = i;
                }

                else if (Mathf.Approximately(nodeData.FCost, lowestFCost) &&
                         nodeData.HCost < nodeDataMap[openListIndices[bestNodeListIndex]].HCost)
                {
                    bestNodeListIndex = i;
                }
            }

            return bestNodeListIndex;
        }

        private float Heuristic(float3 a, float3 b)
        {
            return math.distance(a, b);
        }

        private float Cost(float3 a, float3 b)
        {
            return math.distance(a, b);
        }

        private void ReconstructPath(int endNodeIndex, int startNodeIndex, ref OctreeBlobAsset octree,
            ref NativeHashMap<int, AStarNodeData> nodeDataMap,
            ref NativeList<float3> outputPath)
        {
            var reconstruction = new PathReconstructionHelper(Allocator.Temp);
            var currentNodeIndex = endNodeIndex;

            while (currentNodeIndex != -1 && nodeDataMap.ContainsKey(currentNodeIndex))
            {
                reconstruction.AddNode(currentNodeIndex);
                if (currentNodeIndex == startNodeIndex) break;

                var nodeData = nodeDataMap[currentNodeIndex];
                currentNodeIndex = nodeData.ParentNodeIndex;
            }

            reconstruction.Reverse();

            outputPath.Clear();
            if (reconstruction.NodeIndices.Length > 0)
            {
                if (math.distancesq(StartPosition, octree.Nodes[reconstruction.NodeIndices[0]].Bounds.Center) > 0.01f)
                    outputPath.Add(StartPosition);

                for (var i = 0; i < reconstruction.NodeIndices.Length; i++)
                    outputPath.Add(octree.Nodes[reconstruction.NodeIndices[i]].Bounds.Center);

                if (math.distancesq(EndPosition,
                        octree.Nodes[reconstruction.NodeIndices[reconstruction.NodeIndices.Length - 1]].Bounds.Center) >
                    0.01f) outputPath.Add(EndPosition);
            }
            else if (startNodeIndex == endNodeIndex)
            {
                outputPath.Add(octree.Nodes[startNodeIndex].Bounds.Center);
            }


            reconstruction.Dispose();
        }
    }
}