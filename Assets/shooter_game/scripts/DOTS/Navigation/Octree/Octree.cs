using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace shooter_game.scripts.DOTS.Navigation.Octree
{
    public struct BuildOctreeRequest : IComponentData
    {
    }

    public struct OctreeReference : IComponentData
    {
        public BlobAssetReference<OctreeBlobAsset> Value;
    }

    public enum OctreeNodeType : byte
    {
        Branch,
        LeafFree,
        LeafBlocked
    }

    [BurstCompile]
    public struct OctreeNode
    {
        public AABB Bounds;
        public OctreeNodeType NodeType;
        public int Depth;

        public int ChildrenStartIndex;
        public int ParentIndex;
    }

    [BurstCompile]
    public struct OctreeBuildParams : IComponentData
    {
        public AABB WorldBounds;
        public float MinNodeSize;
        public int MaxDepth;
        public int ObstacleLayerMaskValue;
    }

    [BurstCompile]
    public struct OctreeBlobAsset
    {
        public AABB RootBounds;
        public float MinNodeSize;
        public int MaxDepth;
        public BlobArray<OctreeNode> Nodes;

        public bool TryGetChildNode(in OctreeNode parentNode, int childOctantIndex, out OctreeNode childNode)
        {
            childNode = default;
            if (parentNode.NodeType != OctreeNodeType.Branch ||
                parentNode.ChildrenStartIndex < 0 ||
                childOctantIndex < 0 || childOctantIndex > 7)
                return false;

            var globalChildIndex = parentNode.ChildrenStartIndex + childOctantIndex;
            if (globalChildIndex >= 0 && globalChildIndex < Nodes.Length)
            {
                childNode = Nodes[globalChildIndex];
                return true;
            }

            return false;
        }

        public bool TryGetParentNode(in OctreeNode childNode, out OctreeNode parentNode)
        {
            parentNode = default;
            if (childNode.ParentIndex < 0 || childNode.ParentIndex >= Nodes.Length) return false;
            parentNode = Nodes[childNode.ParentIndex];
            return true;
        }
    }
}