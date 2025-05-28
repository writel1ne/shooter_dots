// Файл: OctreeManagerSystem.cs

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
// Для BuildPhysicsWorld
// Для LayerMask, Debug

// Для [BurstCompile]

namespace shooter_game.scripts.DOTS.Navigation.Volume
{
    // Компонент-флаг для сущности, которая будет инициировать построение Octree
    // (остается тем же)
    public struct BuildOctreeRequest : IComponentData { }

    // Компонент для хранения ссылки на Octree (может быть синглтоном)
    // (остается тем же)
    public struct OctreeReference : IComponentData
    {
        public BlobAssetReference<OctreeBlobAsset> Value;
    }
    
    // Компонент с параметрами (остается тем же)
    public struct OctreeBuildParams : IComponentData
    {
        public AABB WorldBounds;
        public float MinNodeSize;
        public int MaxDepth;
        public int ObstacleLayerMaskValue;
    }

    // MonoBehaviour Authoring (остается тем же)

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(BuildPhysicsWorld))] // Явное указание зависимости
    //[BurstCompile] // ISystem может быть Burst-скомпилирована
    public partial struct OctreeManagerSystem : ISystem
    {
        private EntityQuery _buildRequestQuery; // Будет инициализирован в OnCreate
        private bool _octreeBuilt; // Простое состояние, чтобы избежать повторного построения

        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _buildRequestQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildOctreeRequest>(), ComponentType.ReadOnly<OctreeBuildParams>());
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<BuildOctreeRequest>(); // Не обновляться, если нет запросов
            _octreeBuilt = false;
        }

        //[BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Освобождаем BlobAsset, если он был создан и система уничтожается
            if (SystemAPI.HasSingleton<OctreeReference>())
            {
                var octreeRefSingleton = SystemAPI.GetSingleton<OctreeReference>();
                if (octreeRefSingleton.Value.IsCreated)
                {
                    octreeRefSingleton.Value.Dispose();
                }
                 // Также удалим сущность-синглтон
                Entity octreeEntity = SystemAPI.GetSingletonEntity<OctreeReference>();
                if(octreeEntity != Entity.Null)
                {
                    state.EntityManager.DestroyEntity(octreeEntity);
                }
            }
        }

       // [BurstCompile] // Нельзя использовать Debug.Log в Burst, поэтому отключаем для OnUpdate, если он там есть
        public void OnUpdate(ref SystemState state)
        {
            if (_buildRequestQuery.IsEmptyIgnoreFilter || _octreeBuilt)
            {
                state.Enabled = !_octreeBuilt; // Отключаем систему, если дерево построено или нет запросов
                return;
            }
            
            // Завершаем все незавершенные джобы, которые могут писать в PhysicsWorld
            // Это гарантирует, что CollisionWorld актуален.
            state.Dependency.Complete();

            // Для ISystem лучше использовать SystemAPI.GetSingleton<T>() и SystemAPI.GetSingletonEntity<T>()
            if (!SystemAPI.TryGetSingletonEntity<OctreeBuildParams>(out Entity paramsEntity))
            {
                // Этого не должно произойти, если RequireForUpdate работает и запрос есть.
                // Debug.LogWarning("[OctreeManagerSystem] No OctreeBuildParams singleton entity found.");
                return;
            }

            OctreeBuildParams buildParams = SystemAPI.GetComponent<OctreeBuildParams>(paramsEntity);
            LayerMask obstacleLayerMask = buildParams.ObstacleLayerMaskValue; // Implicit conversion

            // Debug.Log($"[OctreeManagerSystem] Received build request. Building Octree...");
            // Debug.Log($"Params: BoundsCenter={buildParams.WorldBounds.Center}, BoundsExtents={buildParams.WorldBounds.Extents}, MinNodeSize={buildParams.MinNodeSize}, MaxDepth={buildParams.MaxDepth}");
            
            CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            if (collisionWorld.NumBodies > 0)
            {
                BlobAssetReference<OctreeBlobAsset> octreeRef = OctreeBuilder.Build(
                    buildParams.WorldBounds,
                    buildParams.MinNodeSize,
                    buildParams.MaxDepth,
                    in collisionWorld,
                    obstacleLayerMask,
                    Allocator.Persistent
                );
                
                if (octreeRef.IsCreated)
                {
                    Debug.Log($"[OctreeManagerSystem] Octree built successfully with {octreeRef.Value.Nodes.Length} nodes.");
                    
                    // ISystem не имеет прямого доступа к EndInitializationEntityCommandBufferSystem.Singleton
                    // CommandBuffer нужно получать через SystemState.
                    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                    
                    var octreeSingletonEntity = ecb.CreateEntity();
                    ecb.AddComponent(octreeSingletonEntity, new OctreeReference { Value = octreeRef });
                    
                    // #if UNITY_EDITOR // Установка имени не работает напрямую в ECB для ISystem без World
                    // state.EntityManager.SetName(octreeSingletonEntity, "OctreeSingleton"); // Нельзя в ECB, только после Playback
                    // #endif

                    // Удаляем компоненты запроса и параметров с сущности, инициировавшей построение
                    ecb.RemoveComponent<BuildOctreeRequest>(paramsEntity);
                    ecb.RemoveComponent<OctreeBuildParams>(paramsEntity);
                    
                    ecb.Playback(state.EntityManager); // Воспроизводим команды
                    ecb.Dispose();

                    _octreeBuilt = true;
                    state.Enabled = false; // Отключаем систему после успешного построения
                }
                else
                {
                    // Debug.LogError("[OctreeManagerSystem] Failed to build Octree.");
                    // Можно оставить систему включенной для повторной попытки или обработать ошибку иначе
                }
            }

        }
    }
}