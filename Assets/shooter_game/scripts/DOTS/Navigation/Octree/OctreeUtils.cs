using shooter_game.scripts.DOTS.Navigation.Octree;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation
{
    [BurstCompile]
    public static class OctreeUtils
    {
        public static int FindLeafNodeAt(float3 position, ref BlobAssetReference<OctreeBlobAsset> octreeAssetRef)
        {
            return FindLeafNodeAt(position, ref octreeAssetRef, false);
        }

        public static int FindLeafNodeAt(float3 position, ref BlobAssetReference<OctreeBlobAsset> octreeAssetRef,
            bool findFree)
        {
            if (!octreeAssetRef.IsCreated || !octreeAssetRef.Value.RootBounds.Contains(position)) return -1;

            ref var octree = ref octreeAssetRef.Value;
            var currentNodeIndex = 0;

            while (currentNodeIndex != -1 && currentNodeIndex < octree.Nodes.Length)
            {
                var currentNode = octree.Nodes[currentNodeIndex];

                if (currentNode.NodeType == OctreeNodeType.LeafFree) return currentNodeIndex;

                if (currentNode.NodeType == OctreeNodeType.LeafBlocked)
                {
                    if (findFree)
                    {
                        currentNodeIndex++;
                        continue;
                    }

                    return currentNodeIndex;
                }

                if (currentNode.NodeType == OctreeNodeType.Branch)
                {
                    var foundChild = false;
                    for (var i = 0; i < 8; i++)
                        if (octree.TryGetChildNode(currentNode, i, out var childNode))
                        {
                            if (childNode.Bounds.Contains(position))
                            {
                                currentNodeIndex = currentNode.ChildrenStartIndex + i;
                                foundChild = true;
                                break;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Ошибка в структуре дерева");
                            return -1;
                        }

                    if (!foundChild)
                    {
                        // Позиция внутри Branch-узла, но не попадает ни в одного ребенка.
                        // Это может случиться, если позиция точно на границе между детьми,
                        // или из-за ошибок точности float.
                        // Можно вернуть текущий Branch-узел или ошибку.
                        // Для pathfinding'а нам нужен лист.
                        // Простой вариант: вернуть ошибку, т.к. мы не на листе.
                        // Более сложный: найти ближайшего ребенка.
                        Debug.LogWarning("Не найден узел");
                        return -1;
                    }
                }
                else
                {
                    Debug.LogWarning("Ошибка при нахождении узла");
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Находит проходимых листовых соседей для данного листового узла.
        ///     Это упрощенная версия, которая ищет соседей того же размера или больше.
        ///     Для полной версии нужно обрабатывать случаи, когда соседи меньше (родитель соседа - Branch).
        /// </summary>
        public static void FindWalkableLeafNeighbors(
            int nodeIndex,
            ref BlobAssetReference<OctreeBlobAsset> octreeAssetRef,
            ref NativeList<int> neighbors)
        {
            neighbors.Clear();
            if (!octreeAssetRef.IsCreated || nodeIndex < 0 || nodeIndex >= octreeAssetRef.Value.Nodes.Length) return;

            ref var octree = ref octreeAssetRef.Value;
            var targetNode = octree.Nodes[nodeIndex];

            if (targetNode.NodeType == OctreeNodeType.Branch) return;

            for (var face = 0; face < 6; face++)
            {
                var direction = GetFaceDirection(face);
                var queryPoint =
                    targetNode.Bounds.Center + direction * (targetNode.Bounds.Extents * 1.01f); // Маленький офсет

                if (!octree.RootBounds.Contains(queryPoint)) continue;

                var neighborNodeIndex = FindLeafNodeAt(queryPoint, ref octreeAssetRef);

                if (neighborNodeIndex != -1)
                {
                    var neighborNode = octree.Nodes[neighborNodeIndex];
                    if (neighborNode.NodeType == OctreeNodeType.LeafFree && neighborNodeIndex != nodeIndex)
                        if (!neighbors.Contains(neighborNodeIndex))
                            neighbors.Add(neighborNodeIndex);
                }
            }

            // TODO: Добавить поиск соседей по ребрам и углам для более точных путей (диагональное движение).
            // Это потребует более сложных запросов или другой логики поиска соседей.
            // Например, для соседа по ребру:
            // float3 edgeDirectionPoint = targetNode.Bounds.Center + (direction1 + direction2).normalized * (targetNode.Bounds.Extents.magnitude * 1.01f);
            // И аналогично для углов (3 направления).
            // Или использовать алгоритм "Recursive neighbor finding in a Pointerless Quadtree/Octree"
        }

        private static float3 GetFaceDirection(int faceIndex)
        {
            switch (faceIndex)
            {
                case 0: return new float3(1, 0, 0); // +X
                case 1: return new float3(-1, 0, 0); // -X
                case 2: return new float3(0, 1, 0); // +Y
                case 3: return new float3(0, -1, 0); // -Y
                case 4: return new float3(0, 0, 1); // +Z
                case 5: return new float3(0, 0, -1); // -Z
                default: return float3.zero;
            }
        }
    }
}