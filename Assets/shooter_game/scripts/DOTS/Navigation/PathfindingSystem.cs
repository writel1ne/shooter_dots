// Файл: PathfindingSystem.cs

using shooter_game.scripts.DOTS.Navigation.Volume;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// Для Debug

namespace shooter_game.scripts.DOTS.Navigation
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OctreeManagerSystem))] // Убедиться, что Octree уже может быть создан
    public partial class PathfindingSystem : SystemBase
    {
        private EntityQuery _pathRequestQuery;
        
        protected override void OnCreate()
        {
            _pathRequestQuery = GetEntityQuery(ComponentType.ReadOnly<PathfindingRequest>());
            RequireForUpdate<OctreeReference>(); // Требуем наличие Octree
            RequireForUpdate<PathfindingRequest>(); // Не обновляться без запросов
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.HasSingleton<OctreeReference>())
            {
                Debug.LogWarning("[PathfindingSystem] OctreeReference singleton not found. Skipping pathfinding.");
                return;
            }
            
            var octreeRefSingleton = SystemAPI.GetSingleton<OctreeReference>();
            if (!octreeRefSingleton.Value.IsCreated)
            {
                Debug.LogWarning("[PathfindingSystem] Octree BlobAsset is not created. Skipping pathfinding.");
                return;
            }

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            // Dependency.Complete(); // Если были джобы, пишущие в OctreeRef (маловероятно для BlobAsset)

            ref var octreeValue = ref octreeRefSingleton.Value.Value; // Получаем значение для доступа к Nodes.Length
            int estimatedNodeCount = octreeValue.Nodes.Length / 4; // Безопасное получение размера
            if (estimatedNodeCount == 0) estimatedNodeCount = 128; // На случай пустого Octree

            var openList = new NativeList<int>(estimatedNodeCount, Allocator.TempJob);
            var nodeDataMap = new NativeHashMap<int, AStarNodeData>(estimatedNodeCount, Allocator.TempJob);
            var neighborCache = new NativeList<int>(32, Allocator.TempJob); // 32 должно хватить для соседей
            
            Entities
                .WithStoreEntityQueryInField(ref _pathRequestQuery) // Для EntityManager.RemoveComponent вне цикла
                .ForEach((Entity entity, in PathfindingRequest request) =>
            {
                //UnityEngine.Debug.Log($"[PathfindingSystem] Processing path request for entity {entity.Index} from {request.StartPosition} to {request.EndPosition}");

                var pathPointsBuffer = new NativeList<float3>(16, Allocator.TempJob); // Для результата джоба
                
                var pathfindingJob = new AStarPathfindingJob
                {
                    OctreeRef = octreeRefSingleton.Value,
                    StartPosition = request.StartPosition,
                    EndPosition = request.EndPosition,
                    ResultPathPoints = pathPointsBuffer,
                    _openListIndices = openList,
                    _nodeDataMap = nodeDataMap,
                    _neighborCache = neighborCache
                };

                // Запускаем джоб синхронно для простоты примера.
                // В реальном проекте лучше использовать Schedule() и управлять JobHandle.
                // pathfindingJob.Schedule(Dependency).Complete(); // Пример с зависимостью
                pathfindingJob.Run(); // Синхронный запуск

                if (pathPointsBuffer.Length > 0)
                {
                    UnityEngine.Debug.Log($"[PathfindingSystem] Path found for entity {entity.Index} with {pathPointsBuffer.Length} waypoints.");
                    DynamicBuffer<CalculatedPathBufferElement> pathBuffer = ecb.AddBuffer<CalculatedPathBufferElement>(request.RequestingEntity);
                    pathBuffer.ResizeUninitialized(pathPointsBuffer.Length);
                    for (int i = 0; i < pathPointsBuffer.Length; i++)
                    {
                        pathBuffer[i] = new CalculatedPathBufferElement { Waypoint = pathPointsBuffer[i] };
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[PathfindingSystem] Path NOT found for entity {entity.Index}.");
                    // Можно добавить компонент "PathFailedTag" для обработки
                }
                
                pathPointsBuffer.Dispose(); // Освобождаем временный буфер
                ecb.RemoveComponent<PathfindingRequest>(entity); // Удаляем запрос после обработки

            }).WithoutBurst().Run(); // Run() здесь, т.к. ECB и Debug.Log. Можно вынести логику в IJobChunk.
            // Если использовать Schedule, то .ScheduleParallel() и передать ECB.AsParallelWriter()
            // ECB нужно будет добавить как зависимость для воспроизведения команд.
        }
    }
}