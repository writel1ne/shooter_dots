using shooter_game.scripts.DOTS.Navigation.Octree;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PathfindingSystem : ISystem
    {
        private EntityQuery _pathRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            //state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<OctreeReference>();
            state.RequireForUpdate<PathfindingRequest>();
            _pathRequestQuery = state.GetEntityQuery(ComponentType.ReadOnly<PathfindingRequest>());
        }

        public void OnUpdate(ref SystemState state)
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
            var ecb = ecbSingleton.CreateCommandBuffer(state.World.Unmanaged);

            ref var octreeValue = ref octreeRefSingleton.Value.Value;
            var estimatedNodeCount = octreeValue.Nodes.Length / 4;
            if (estimatedNodeCount == 0) estimatedNodeCount = 128;

            var openList = new NativeList<int>(estimatedNodeCount, Allocator.TempJob);
            var nodeDataMap = new NativeHashMap<int, AStarNodeData>(estimatedNodeCount, Allocator.TempJob);
            var neighborCache = new NativeList<int>(32, Allocator.TempJob);

            foreach (var query in SystemAPI.Query<RefRO<PathfindingRequest>>().WithEntityAccess())
            {
                var pathPointsBuffer = new NativeList<float3>(16, Allocator.TempJob);

                var pathfindingJob = new AStarPathfindingJob
                {
                    OctreeRef = octreeRefSingleton.Value,
                    StartPosition = query.Item1.ValueRO.StartPosition,
                    EndPosition = query.Item1.ValueRO.EndPosition,
                    ResultPathPoints = pathPointsBuffer,
                    _openListIndices = openList,
                    _nodeDataMap = nodeDataMap,
                    _neighborCache = neighborCache
                };

                pathfindingJob.Run();

                if (pathPointsBuffer.Length > 0)
                {
                    var pathBuffer = ecb.AddBuffer<CalculatedPathBufferElement>(query.Item1.ValueRO.RequestingEntity);
                    pathBuffer.ResizeUninitialized(pathPointsBuffer.Length);
                    for (var i = 0; i < pathPointsBuffer.Length; i++)
                        pathBuffer[i] = new CalculatedPathBufferElement { Waypoint = pathPointsBuffer[i] };
                }

                pathPointsBuffer.Dispose();
                ecb.RemoveComponent<PathfindingRequest>(query.Item2);
            }

            openList.Dispose();
            nodeDataMap.Dispose();
            neighborCache.Dispose();

            // Entities
            //     .WithStoreEntityQueryInField(ref _pathRequestQuery)
            //     .ForEach((Entity entity, in PathfindingRequest request) =>
            // {
            //     var pathPointsBuffer = new NativeList<float3>(16, Allocator.TempJob);
            //     
            //     var pathfindingJob = new AStarPathfindingJob
            //     {
            //         OctreeRef = octreeRefSingleton.Value,
            //         StartPosition = request.StartPosition,
            //         EndPosition = request.EndPosition,
            //         ResultPathPoints = pathPointsBuffer,
            //         _openListIndices = openList,
            //         _nodeDataMap = nodeDataMap,
            //         _neighborCache = neighborCache
            //     };
            //     
            //     pathfindingJob.Run();
            //
            //     if (pathPointsBuffer.Length > 0)
            //     {
            //         DynamicBuffer<CalculatedPathBufferElement> pathBuffer = ecb.AddBuffer<CalculatedPathBufferElement>(request.RequestingEntity);
            //         pathBuffer.ResizeUninitialized(pathPointsBuffer.Length);
            //         for (int i = 0; i < pathPointsBuffer.Length; i++)
            //         {
            //             pathBuffer[i] = new CalculatedPathBufferElement { Waypoint = pathPointsBuffer[i] };
            //         }
            //     }
            //     else
            //     {
            //         
            //     }
            //     
            //     pathPointsBuffer.Dispose();
            //     ecb.RemoveComponent<PathfindingRequest>(entity); 
            //
            // }).WithoutBurst().Run();
        }
    }
}