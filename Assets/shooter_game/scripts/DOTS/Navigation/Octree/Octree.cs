// Файл: OctreeBlobAsset.cs

using Unity.Entities;
using Unity.Mathematics;

// Для BlobArray и BlobAssetReference

namespace shooter_game.scripts.DOTS.Navigation.Volume
{
    public enum OctreeNodeType : byte
    {
        Branch,      // Внутренний узел с детьми
        LeafFree,    // Листовой узел, свободное пространство
        LeafBlocked  // Листовой узел, заблокированное пространство
    }

    public struct OctreeNode
    {
        public AABB Bounds;             // Границы узла в мировом пространстве
        public OctreeNodeType NodeType; // Тип узла (Branch, LeafFree, LeafBlocked)
        public int Depth;               // Глубина узла в дереве

        // Если NodeType == Branch, это индекс первого дочернего узла в OctreeBlobAsset.Nodes.
        // Дети хранятся последовательно: ChildrenStartIndex, ..., ChildrenStartIndex + 7.
        // Иначе, это -1 или другой невалидный индекс.
        public int ChildrenStartIndex;
        public int ParentIndex;         // Индекс родительского узла, -1 для корня.
    }

    public struct OctreeBlobAsset
    {
        public AABB RootBounds;         // Границы всего Octree
        public float MinNodeSize;       // Минимальный размер узла, при котором прекращается деление
        public int MaxDepth;            // Максимальная глубина рекурсии
        public BlobArray<OctreeNode> Nodes; // Все узлы дерева в плоском массиве

        /// <summary>
        /// Пытается получить дочерний узел по его октантному индексу (0-7).
        /// </summary>
        public bool TryGetChildNode(in OctreeNode parentNode, int childOctantIndex, out OctreeNode childNode)
        {
            childNode = default;
            if (parentNode.NodeType != OctreeNodeType.Branch || 
                parentNode.ChildrenStartIndex < 0 ||
                childOctantIndex < 0 || childOctantIndex > 7)
            {
                return false;
            }

            int globalChildIndex = parentNode.ChildrenStartIndex + childOctantIndex;
            if (globalChildIndex >= 0 && globalChildIndex < Nodes.Length)
            {
                childNode = Nodes[globalChildIndex];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Пытается получить родительский узел.
        /// </summary>
        public bool TryGetParentNode(in OctreeNode childNode, out OctreeNode parentNode)
        {
            parentNode = default;
            if (childNode.ParentIndex < 0 || childNode.ParentIndex >= Nodes.Length)
            {
                return false;
            }
            parentNode = Nodes[childNode.ParentIndex];
            return true;
        }
    }
}