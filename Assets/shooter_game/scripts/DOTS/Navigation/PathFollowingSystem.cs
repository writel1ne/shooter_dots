// Файл: PathFollowingSystem.cs

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation
{
    // Для Time.deltaTime, Debug.DrawLine

// Компонент с данными для следования по пути
    public struct PathFollowData : IComponentData
    {
        public int CurrentWaypointIndex;
        public float MovementSpeed;
        public float RotationSpeed; // В радианах/сек
        public float ArrivalDistanceThresholdSq; // Квадрат дистанции для смены вейпоинта
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PathfindingSystem))] // После того как путь может быть рассчитан
    public partial struct PathFollowingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CalculatedPathBufferElement>();
            state.RequireForUpdate<PathFollowData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp); // Для удаления буфера и PathFollowData

            foreach (var (transform, pathFollow, pathBuffer, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<PathFollowData>, DynamicBuffer<CalculatedPathBufferElement>>()
                         .WithEntityAccess())
            {
                if (pathBuffer.IsEmpty || pathFollow.ValueRO.CurrentWaypointIndex >= pathBuffer.Length)
                {
                    // Путь пройден или пуст
                    ecb.RemoveComponent<PathFollowData>(entity);
                    ecb.RemoveComponent<CalculatedPathBufferElement>(entity); 
                    // Можно добавить компонент "PathCompleteTag"
                    continue;
                }

                float3 currentPosition = transform.ValueRO.Position;
                float3 targetWaypoint = pathBuffer[pathFollow.ValueRO.CurrentWaypointIndex].Waypoint;

                // Движение к текущему вейпоинту
                float3 directionToWaypoint = math.normalize(targetWaypoint - currentPosition);
            
                // Поворот
                if (math.lengthsq(directionToWaypoint) > 0.001f) // Избегаем NaN если направление нулевое
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(directionToWaypoint, math.up());
                    transform.ValueRW.Rotation = math.slerp(transform.ValueRO.Rotation, targetRotation, pathFollow.ValueRO.RotationSpeed * deltaTime);
                }
            
                // Перемещение
                transform.ValueRW.Position += math.mul(transform.ValueRO.Rotation, new float3(0,0,1)) * pathFollow.ValueRO.MovementSpeed * deltaTime;
                // Или проще: transform.ValueRW.Position += directionToWaypoint * pathFollow.ValueRO.MovementSpeed * deltaTime;
                // (если не хотите чтобы поворот влиял на направление движения немедленно)


                // Проверка достижения вейпоинта
                if (math.distancesq(currentPosition, targetWaypoint) < pathFollow.ValueRO.ArrivalDistanceThresholdSq)
                {
                    pathFollow.ValueRW.CurrentWaypointIndex++;
                    if (pathFollow.ValueRO.CurrentWaypointIndex >= pathBuffer.Length)
                    {
                        // Достигли конца пути
                        // ecb.RemoveComponent<PathFollowData>(entity); // Уже обработано выше
                        // ecb.RemoveComponent<CalculatedPathBufferElement>(entity);
                        // Debug.Log($"Entity {entity.Index} reached end of path.");
                    }
                }
                
#if UNITY_EDITOR
                if (pathBuffer.Length > 0)
                {
                    if (pathFollow.ValueRO.CurrentWaypointIndex < pathBuffer.Length)
                    {
                        UnityEngine.Debug.DrawLine(transform.ValueRO.Position, pathBuffer[pathFollow.ValueRO.CurrentWaypointIndex].Waypoint, Color.yellow);
                    }
                    for (int i = 0; i < pathBuffer.Length - 1; i++)
                    {
                        UnityEngine.Debug.DrawLine(pathBuffer[i].Waypoint, pathBuffer[i + 1].Waypoint, Color.cyan);
                    }
                }
#endif
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}