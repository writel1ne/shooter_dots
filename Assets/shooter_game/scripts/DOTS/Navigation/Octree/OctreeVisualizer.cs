// Файл: OctreeVisualizer.cs

using shooter_game.scripts.DOTS.Navigation.Volume;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation
{
    public class OctreeVisualizer : MonoBehaviour
    {
        public float MaxNodeSizeMagnitude = 3;
        public float MaxNodeDistance = 10;
        
        [Tooltip("Включить/выключить отрисовку Gizmos для Octree.")]
        public bool EnableVisualization = true;

        [Header("Фильтры видимости узлов")]
        [Tooltip("Показывать узлы-ветви (Branch nodes).")]
        public bool DrawBranches = true;
        [Tooltip("Цвет для узлов-ветвей.")]
        public Color BranchColor = new Color(0f, 0f, 1f, 0.08f);

        [Tooltip("Показывать свободные листовые узлы (LeafFree nodes).")]
        public bool DrawFreeLeaves = true;
        [Tooltip("Цвет для свободных листовых узлов.")]
        public Color FreeLeafColor = new Color(0f, 1f, 0f, 0.2f);

        [Tooltip("Показывать заблокированные листовые узлы (LeafBlocked nodes).")]
        public bool DrawBlockedLeaves = true;
        [Tooltip("Цвет для заблокированных листовых узлов.")]
        public Color BlockedLeafColor = new Color(1f, 0f, 0f, 0.2f);

        [Header("Фильтры глубины и типа")]
        [Tooltip("Максимальная глубина узлов для отрисовки (-1 для всех глубин). Корень имеет глубину 0.")]
        public int MaxDrawDepth = -1;
    
        [Tooltip("Если true, будут отрисованы только листовые узлы (LeafFree и LeafBlocked), игнорируя DrawBranches.")]
        public bool OnlyDrawLeaves = false;

        private EntityManager _entityManager;
        private EntityQuery _octreeReferenceQuery;
        private bool _initialized = false;
        private OctreeReference _octreeRefComponent;

        void InitializeIfNeeded()
        {
            if (_initialized || !Application.isPlaying) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                _entityManager = world.EntityManager;
                // Создаем запрос для поиска сущности с компонентом OctreeReference
                _octreeReferenceQuery = _entityManager.CreateEntityQuery(typeof(OctreeReference));
                _initialized = true;
            }
        }
        
        void OnDrawGizmos()
        {
            if (!EnableVisualization)
                return;

            InitializeIfNeeded();

            if (!_initialized || !_entityManager.World.IsCreated)
            {
                // Сообщение можно вывести в Scene View для удобства
                // UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "Octree Visualizer: Waiting for EntityManager...");
                return;
            }

            // Проверяем, есть ли хотя бы одна сущность с компонентом OctreeReference
            if (_octreeReferenceQuery.IsEmpty)
            {
                // Debug.LogWarning("[OctreeVisualizer] Сущность с OctreeReference не найдена.");
                return;
            }

            // Предполагаем, что OctreeReference является синглтоном (только одна такая сущность)
            // Если их может быть несколько, логика должна быть сложнее.
            Entity octreeEntity = _octreeReferenceQuery.GetSingletonEntity();
            if (octreeEntity == Entity.Null) // Дополнительная проверка, если GetSingletonEntity вернет Null
            {
                // Debug.LogWarning("[OctreeVisualizer] GetSingletonEntity для OctreeReference вернул Entity.Null.");
                return;
            }

            if (!_octreeRefComponent.Value.IsCreated)
            {
                try
                {
                    _octreeRefComponent = _entityManager.GetComponentData<OctreeReference>(octreeEntity);
                }
                catch (System.ArgumentException)
                {
                    // Сущность могла быть удалена или компонент удален
                    // Debug.LogWarning("[OctreeVisualizer] Не удалось получить OctreeReference компонент с сущности.");
                    _initialized = false; // Попробуем переинициализироваться в следующий раз
                    _octreeReferenceQuery.Dispose();
                    return;
                }
            }
            else
            {
                if (!_octreeRefComponent.Value.IsCreated)
                {
                    // Debug.LogWarning("[OctreeVisualizer] OctreeBlobAsset в OctreeReference не создан.");
                    return;
                }

                ref OctreeBlobAsset octreeAsset = ref _octreeRefComponent.Value.Value;

                if (octreeAsset.Nodes.Length == 0)
                {
                    // Debug.LogWarning("[OctreeVisualizer] Octree не содержит узлов.");
                    return;
                }

                for (int i = 0; i < octreeAsset.Nodes.Length; i++)
                {
                    ref readonly OctreeNode node = ref octreeAsset.Nodes[i];
                    DrawNodeGizmo(in node);
                }
            }
        }
        
        private void DrawNodeGizmo(in OctreeNode node)
        {
            if (MaxDrawDepth >= 0 && node.Depth > MaxDrawDepth)
            {
                return;
            }

            if (OnlyDrawLeaves && node.NodeType == OctreeNodeType.Branch)
            {
                return;
            }

            if (node.Bounds.Size.x > MaxNodeSizeMagnitude)
               // || math.length(new float3(Camera.current.transform.position) - node.Bounds.Min) > MaxNodeDistance)
            {
                return;
            }

            Color gizmoColor = Color.clear; // Инициализация значением по умолчанию
            bool shouldDrawNode = false;

            switch (node.NodeType)
            {
                case OctreeNodeType.Branch:
                    if (DrawBranches)
                    {
                        gizmoColor = BranchColor;
                        shouldDrawNode = false;
                    }
                    break;
                case OctreeNodeType.LeafFree:
                    if (DrawFreeLeaves)
                    {
                        gizmoColor = FreeLeafColor;
                        shouldDrawNode = true;
                    }
                    break;
                case OctreeNodeType.LeafBlocked:
                    if (DrawBlockedLeaves)
                    {
                        gizmoColor = BlockedLeafColor;
                        shouldDrawNode = false;
                    }
                    break;
                default:
                    return;
            }

            if (shouldDrawNode)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireCube(node.Bounds.Center, node.Bounds.Size);
            }
        }
    
        // Важно освободить EntityQuery, когда MonoBehaviour уничтожается
        void OnDestroy()
        {
            if (_initialized && !_octreeReferenceQuery.IsEmpty)
            {
                _octreeReferenceQuery.Dispose();
            }
        }
    }
}