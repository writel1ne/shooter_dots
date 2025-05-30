// using shooter_game.scripts.DOTS.Navigation.Volume;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Physics.Systems;
// using UnityEngine;
//
// namespace shooter_game.scripts.DOTS.Navigation.Octree
// {
//     public struct BuildOctreeRequest : IComponentData { }
//     
//     public struct OctreeReference : IComponentData
//     {
//         public BlobAssetReference<OctreeBlobAsset> Value;
//     }
//
//     public struct OctreeBuildParams : IComponentData
//     {
//         public AABB WorldBounds;
//         public float MinNodeSize;
//         public int MaxDepth;
//         public int ObstacleLayerMaskValue;
//     }
//
//     [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
//     [BurstCompile] 
//     public partial struct OctreeManagerSystem : ISystem
//     {
//         private EntityQuery _buildRequestQuery;
//         private bool _octreeBuilt;
//
//         //[BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<PhysicsWorldSingleton>();
//             state.RequireForUpdate<BuildOctreeRequest>();
//             _buildRequestQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildOctreeRequest>(), ComponentType.ReadOnly<OctreeBuildParams>());
//             _octreeBuilt = false;
//         }
//
//         //[BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//             if (SystemAPI.HasSingleton<OctreeReference>())
//             {
//                 var octreeRefSingleton = SystemAPI.GetSingleton<OctreeReference>();
//                 if (octreeRefSingleton.Value.IsCreated)
//                 {
//                     octreeRefSingleton.Value.Dispose();
//                 }
//                 
//                 Entity octreeEntity = SystemAPI.GetSingletonEntity<OctreeReference>();
//                 if(octreeEntity != Entity.Null)
//                 {
//                     state.EntityManager.DestroyEntity(octreeEntity);
//                 }
//             }
//         }
//
//        // [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             if (_buildRequestQuery.IsEmptyIgnoreFilter || _octreeBuilt)
//             {
//                 state.Enabled = !_octreeBuilt;
//                 return;
//             }
//             
//             if (!SystemAPI.TryGetSingletonEntity<OctreeBuildParams>(out Entity paramsEntity))
//             {
//                 return;
//             }
//
//             OctreeBuildParams buildParams = SystemAPI.GetComponent<OctreeBuildParams>(paramsEntity);
//             LayerMask obstacleLayerMask = buildParams.ObstacleLayerMaskValue;
//             
//             CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
//
//             if (true)
//             {
//                 BlobAssetReference<OctreeBlobAsset> octreeRef = OctreeBuilder.Build(
//                     buildParams.WorldBounds,
//                     buildParams.MinNodeSize,
//                     buildParams.MaxDepth,
//                     in collisionWorld,
//                     obstacleLayerMask,
//                     Allocator.Persistent
//                 );
//                 
//                 if (octreeRef.IsCreated)
//                 {
//                     EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
//                     
//                     var octreeSingletonEntity = ecb.CreateEntity();
//                     ecb.AddComponent(octreeSingletonEntity, new OctreeReference { Value = octreeRef });
//
//                     ecb.RemoveComponent<BuildOctreeRequest>(paramsEntity);
//                     ecb.RemoveComponent<OctreeBuildParams>(paramsEntity);
//                     
//                     ecb.Playback(state.EntityManager);
//                     ecb.Dispose();
//
//                     _octreeBuilt = true;
//                     state.Enabled = false;
//                 }
//                 else
//                 {
//                 }
//             }
//
//         }
//     }
// }

