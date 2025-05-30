using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace shooter_game.scripts.DOTS.Navigation
{
    public struct PathfindingRequest : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public Entity RequestingEntity;
    }

    public struct CalculatedPathBufferElement : IBufferElementData
    {
        public float3 Waypoint;
    }

    public struct AStarNodeData
    {
        public int NodeIndex; // Индекс в OctreeBlobAsset.Nodes
        public int ParentNodeIndex; // Индекс родительского узла A* в OctreeBlobAsset.Nodes
        public float GCost; // Стоимость от старта до этого узла
        public float HCost; // Эвристическая стоимость от этого узла до цели
        public float FCost => GCost + HCost;

        public int CompareTo(AStarNodeData other)
        {
            var compare = FCost.CompareTo(other.FCost);
            if (compare == 0) compare = HCost.CompareTo(other.HCost);
            return compare;
        }
    }

    public struct PathReconstructionHelper
    {
        public NativeList<int> NodeIndices;

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
            for (var i = 0; i < NodeIndices.Length / 2; i++)
            {
                (NodeIndices[i], NodeIndices[NodeIndices.Length - 1 - i]) = (NodeIndices[NodeIndices.Length - 1 - i], NodeIndices[i]);
            }
        }

        public void Dispose()
        {
            if (NodeIndices.IsCreated) NodeIndices.Dispose();
        }
    }
}