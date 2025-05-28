// Файл: OctreeUtils.cs

using shooter_game.scripts.DOTS.Navigation.Volume;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace shooter_game.scripts.DOTS.Navigation
{
    public static class OctreeUtils
    {
        /// <summary>
        /// Находит листовой узел Octree, содержащий данную мировую позицию.
        /// Возвращает индекс узла в OctreeBlobAsset.Nodes или -1, если не найден.
        /// </summary>
        public static int FindLeafNodeAt(float3 position, ref BlobAssetReference<OctreeBlobAsset> octreeAssetRef)
        {
            if (!octreeAssetRef.IsCreated || !octreeAssetRef.Value.RootBounds.Contains(position))
            {
                return -1; // Позиция вне корневых границ
            }

            ref var octree = ref octreeAssetRef.Value; // Получаем ссылку для удобства
            int currentNodeIndex = 0; // Начинаем с корневого узла

            while (currentNodeIndex != -1 && currentNodeIndex < octree.Nodes.Length)
            {
                OctreeNode currentNode = octree.Nodes[currentNodeIndex];

                if (currentNode.NodeType == OctreeNodeType.LeafFree || currentNode.NodeType == OctreeNodeType.LeafBlocked)
                {
                    return currentNodeIndex; // Нашли листовой узел
                }

                if (currentNode.NodeType == OctreeNodeType.Branch)
                {
                    bool foundChild = false;
                    for (int i = 0; i < 8; i++) // Проверяем всех 8 детей
                    {
                        if (octree.TryGetChildNode(currentNode, i, out OctreeNode childNode))
                        {
                            if (childNode.Bounds.Contains(position))
                            {
                                // Ребенок содержит позицию, продолжаем спуск
                                // Индекс ребенка в общем массиве это ChildrenStartIndex + i
                                currentNodeIndex = currentNode.ChildrenStartIndex + i; 
                                foundChild = true;
                                break;
                            }
                        }
                        else
                        {
                             // Это не должно происходить для Branch узла с валидным ChildrenStartIndex,
                             // но для безопасности можно добавить логирование или обработку ошибки.
                             return -1; // Ошибка в структуре дерева
                        }
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
                        return -1; 
                    }
                }
                else
                {
                    // Неизвестный тип узла или ошибка
                    return -1;
                }
            }
            return -1; // Не должен сюда дойти, если все корректно
        }

        /// <summary>
        /// Находит проходимых листовых соседей для данного листового узла.
        /// Это упрощенная версия, которая ищет соседей того же размера или больше.
        /// Для полной версии нужно обрабатывать случаи, когда соседи меньше (родитель соседа - Branch).
        /// </summary>
        public static void FindWalkableLeafNeighbors(
            int nodeIndex,
            ref BlobAssetReference<OctreeBlobAsset> octreeAssetRef,
            ref NativeList<int> neighbors) // Выходной список индексов соседних узлов
        {
            neighbors.Clear();
            if (!octreeAssetRef.IsCreated || nodeIndex < 0 || nodeIndex >= octreeAssetRef.Value.Nodes.Length) return;

            ref var octree = ref octreeAssetRef.Value;
            OctreeNode targetNode = octree.Nodes[nodeIndex];

            if (targetNode.NodeType == OctreeNodeType.Branch) return; // Ищем соседей только для листьев

            // Проверяем 6 основных направлений (по граням)
            for (int face = 0; face < 6; face++)
            {
                float3 direction = GetFaceDirection(face);
                // Точка на внешней стороне грани целевого узла, чуть-чуть смещенная наружу
                float3 queryPoint = targetNode.Bounds.Center + direction * (targetNode.Bounds.Extents * 1.01f); // Маленький офсет

                // Если точка вне всего Octree, то там нет соседа
                if (!octree.RootBounds.Contains(queryPoint)) continue;

                int neighborNodeIndex = FindLeafNodeAt(queryPoint, ref octreeAssetRef);
                
                if (neighborNodeIndex != -1)
                {
                    OctreeNode neighborNode = octree.Nodes[neighborNodeIndex];
                    if (neighborNode.NodeType == OctreeNodeType.LeafFree && neighborNodeIndex != nodeIndex)
                    {
                        if (!neighbors.Contains(neighborNodeIndex)) // Предотвращаем дубликаты
                        {
                             neighbors.Add(neighborNodeIndex);
                        }
                    }
                }
            }
            
            // TODO: Добавить поиск соседей по ребрам и углам для более точных путей (диагональное движение).
            // Это потребует более сложных запросов или другой логики поиска соседей.
            // Например, для соседа по ребру:
            // float3 edgeDirectionPoint = targetNode.Bounds.Center + (direction1 + direction2).normalized * (targetNode.Bounds.Extents.magnitude * 1.01f);
            // И аналогично для углов (3 направления).
            // Или использовать алгоритм "Recursive neighbor finding in a Pointerless Quadtree/Octree"
        }
        
        // Вспомогательная функция для направлений граней
        private static float3 GetFaceDirection(int faceIndex)
        {
            switch (faceIndex)
            {
                case 0: return new float3(1, 0, 0);  // +X
                case 1: return new float3(-1, 0, 0); // -X
                case 2: return new float3(0, 1, 0);  // +Y
                case 3: return new float3(0, -1, 0); // -Y
                case 4: return new float3(0, 0, 1);  // +Z
                case 5: return new float3(0, 0, -1); // -Z
                default: return float3.zero;
            }
        }
    }
}