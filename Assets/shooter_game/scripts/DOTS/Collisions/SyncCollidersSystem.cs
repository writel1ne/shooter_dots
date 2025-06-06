using System.Collections;
using shooter_game.scripts.DOTS.Navigation.Octree;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Collisions
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct SyncCollidersSystem : ISystem
    {
        private bool _isFirstUpdate;
        private int _ticks;
        private EntityQuery _buildRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            _buildRequestQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BuildOctreeRequest>(),
                ComponentType.ReadOnly<OctreeBuildParams>()
            );
            
            _ticks = 250;
            _isFirstUpdate = true;

            if (!SystemAPI.HasSingleton<EntityWorldColliders>())
            {
                var singletonEntity = state.EntityManager.CreateEntity();
                var newCollidersComponent = new EntityWorldColliders
                {
                    Colliders = new NativeHashMap<Entity, OBB>(16, Allocator.Persistent)
                };

                state.EntityManager.AddComponentData(singletonEntity, newCollidersComponent);
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<EntityWorldColliders>())
            {
                var collidersComponent = SystemAPI.GetSingleton<EntityWorldColliders>();
                if (collidersComponent.Colliders.IsCreated) collidersComponent.Colliders.Dispose();
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //var worldCollidersRW = SystemAPI.GetSingletonRW<EntityWorldColliders>();
            SystemAPI.TryGetSingletonRW<EntityWorldColliders>(out var worldCollidersRW);

            if (!worldCollidersRW.IsValid) return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            int i = 0;
            foreach (var (request, entity) in SystemAPI.Query<RefRO<GameObjectUpdateSyncColliderRequest>>()
                         .WithEntityAccess())
            {
                UpdateData(entity, request, worldCollidersRW);

                i++;
                ecb.RemoveComponent<GameObjectUpdateSyncColliderRequest>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            if (_ticks <= 0)
            {
                if (_isFirstUpdate 
                    && _buildRequestQuery.IsEmptyIgnoreFilter)
                {
                    var singletonEntity = state.EntityManager.CreateEntity();

                    var buildParams = new OctreeBuildParams
                    {
                        WorldBounds = new AABB { Center = float3.zero, Extents = new float3(60, 60, 60) },
                        MinNodeSize = 1f,
                        MaxDepth = 8,
                        ObstacleLayerMaskValue = 1
                    };

                    state.EntityManager.AddComponent<BuildOctreeRequest>(singletonEntity);
                    state.EntityManager.AddComponentData(singletonEntity, buildParams);

                    //_isFirstUpdate = false;
                }
                
                _ticks = 5;
            }
            else
            {
                _ticks--;
            }
        }

        private void UpdateData(Entity entity, RefRO<GameObjectUpdateSyncColliderRequest> request,
            RefRW<EntityWorldColliders> collidersComponent)
        {
            if (collidersComponent.ValueRW.Colliders.ContainsKey(entity))
            {
                collidersComponent.ValueRW.Colliders[entity] = new OBB(request.ValueRO.Bounds, request.ValueRO.Quaternion,
                    request.ValueRO.Scale, request.ValueRO.Center);
            }
            else
            {
                collidersComponent.ValueRW.Colliders.Add(entity,
                    new OBB(request.ValueRO.Bounds, request.ValueRO.Quaternion, request.ValueRO.Scale,
                        request.ValueRO.Center));
            }
        }
    }
}