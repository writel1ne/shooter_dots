// Файл: AStarOctreePathfinding.cs

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Для PriorityQueue, если не делать свою

namespace shooter_game.scripts.DOTS.Navigation
{
    // Компонент-запрос на поиск пути
    public struct PathfindingRequest : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public Entity RequestingEntity; // Сущность, для которой ищется путь
    }

    // Компонент, хранящий найденный путь (буфер элементов)
    public struct CalculatedPathBufferElement : IBufferElementData
    {
        public float3 Waypoint;
    }
    
    // Данные для узла A* (внутренние)
    // Мы будем использовать индекс узла в Octree.Nodes как идентификатор узла A*
    public struct AStarNodeData
    {
        public int NodeIndex;       // Индекс в OctreeBlobAsset.Nodes
        public int ParentNodeIndex; // Индекс родительского узла A* в OctreeBlobAsset.Nodes
        public float GCost;         // Стоимость от старта до этого узла
        public float HCost;         // Эвристическая стоимость от этого узла до цели
        public float FCost => GCost + HCost;

        // Для использования в NativePriorityQueue
        public int CompareTo(AStarNodeData other)
        {
            int compare = FCost.CompareTo(other.FCost);
            if (compare == 0)
            {
                compare = HCost.CompareTo(other.HCost); // Приоритет узлам ближе к цели, если F-стоимость одинакова
            }
            return compare;
        }
    }

    // Утилитарный класс/структура для хранения пути во время реконструкции
    // Можно использовать NativeList напрямую, но это для примера
    public struct PathReconstructionHelper
    {
        public NativeList<int> NodeIndices; // Хранит индексы узлов Octree в обратном порядке

        public PathReconstructionHelper(Allocator allocator, int initialCapacity = 16)
        {
            NodeIndices = new NativeList<int>(initialCapacity, allocator);
        }

        public void AddNode(int nodeIndex)
        {
            NodeIndices.Add(nodeIndex);
        }

        public void Reverse()
        {
            // NativeList не имеет встроенного Reverse, реализуем простой
            for (int i = 0; i < NodeIndices.Length / 2; i++)
            {
                int temp = NodeIndices[i];
                NodeIndices[i] = NodeIndices[NodeIndices.Length - 1 - i];
                NodeIndices[NodeIndices.Length - 1 - i] = temp;
            }
        }

        public void Dispose()
        {
            if (NodeIndices.IsCreated) NodeIndices.Dispose();
        }
    }
}