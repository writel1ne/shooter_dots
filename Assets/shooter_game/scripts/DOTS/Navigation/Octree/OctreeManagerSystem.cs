using shooter_game.scripts.DOTS.Collisions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;

namespace shooter_game.scripts.DOTS.Navigation.Octree
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct OctreeManagerSystem : ISystem
    {
        private EntityQuery _buildRequestQuery;
        private bool _jobScheduled;
        private NativeReference<BlobAssetReference<OctreeBlobAsset>> _jobResultOctreeRef;
        private JobHandle _buildJobHandle;

        public void OnCreate(ref SystemState state)
        {
            _buildRequestQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BuildOctreeRequest>(),
                ComponentType.ReadOnly<OctreeBuildParams>()
            );
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<EntityWorldColliders>();
            state.RequireForUpdate(_buildRequestQuery);

            _jobScheduled = false;
            _jobResultOctreeRef = new NativeReference<BlobAssetReference<OctreeBlobAsset>>(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            _buildJobHandle.Complete();

            if (_jobResultOctreeRef.IsCreated)
            {
                if (_jobResultOctreeRef.Value.IsCreated) _jobResultOctreeRef.Value.Dispose();
                _jobResultOctreeRef.Dispose();
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_jobScheduled)
            {
                if (_buildJobHandle.IsCompleted)
                {
                    //_buildJobHandle.Complete();
                    _jobScheduled = false;

                    var builtOctree = _jobResultOctreeRef.Value;

                    if (builtOctree.IsCreated)
                    {
                        var ecb = new EntityCommandBuffer(Allocator.Temp);
                        bool entityExists = SystemAPI.TryGetSingletonEntity<OctreeReference>(out var octreeEntity);
                        Entity octreeSingletonEntity = entityExists ? octreeEntity : ecb.CreateEntity();

                        if (entityExists) 
                            ecb.SetComponent(octreeSingletonEntity, new OctreeReference { Value = builtOctree });
                        else 
                            ecb.AddComponent(octreeSingletonEntity, new OctreeReference { Value = builtOctree });
                        
                        if (!_buildRequestQuery.IsEmptyIgnoreFilter)
                        {
                            var requestEntity = _buildRequestQuery.GetSingletonEntity();
                            ecb.RemoveComponent<BuildOctreeRequest>(requestEntity);
                            ecb.RemoveComponent<OctreeBuildParams>(requestEntity);
                        }

                        ecb.Playback(state.EntityManager);
                        ecb.Dispose();
                        
                       // state.Enabled = false;
                    }
                }
                else
                {
                    state.Dependency = JobHandle.CombineDependencies(state.Dependency, _buildJobHandle);
                }
            }
            else
            {
                if (_buildRequestQuery.IsEmptyIgnoreFilter) return;

                if (!SystemAPI.TryGetSingletonEntity<OctreeBuildParams>(out var paramsEntity) ||
                    !SystemAPI.TryGetSingleton(out EntityWorldColliders colliders)) return;

                var buildParams = SystemAPI.GetComponent<OctreeBuildParams>(paramsEntity);

                var buildJob = new OctreeBuildJob
                {
                    BuildParams = buildParams,
                    Colliders = colliders,
                    ResultAllocator = Allocator.Persistent,
                    ResultOctreeRef = _jobResultOctreeRef
                };
                
                _buildJobHandle = buildJob.Schedule(state.Dependency);
                //_buildJobHandle.Complete();
                state.Dependency = _buildJobHandle;

                _jobScheduled = true;
            }
        }
    }
}