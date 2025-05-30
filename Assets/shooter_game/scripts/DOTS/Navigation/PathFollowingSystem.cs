using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Navigation
{
    public struct PathFollowData : IComponentData
    {
        public int CurrentWaypointIndex;
        public float MovementSpeed;
        public float RotationSpeed;
        public float ArrivalDistanceThresholdSq;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PathFollowingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CalculatedPathBufferElement>();
            state.RequireForUpdate<PathFollowData>();
            // state.RequireForUpdate<PathfindingSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, pathFollow, pathBuffer, entity) in
                     SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<PathFollowData>,
                             DynamicBuffer<CalculatedPathBufferElement>>()
                         .WithEntityAccess())
            {
                if (pathBuffer.IsEmpty || pathFollow.ValueRO.CurrentWaypointIndex >= pathBuffer.Length)
                {
                    // Путь пройден или пуст
                    ecb.RemoveComponent<PathFollowData>(entity);
                    ecb.RemoveComponent<CalculatedPathBufferElement>(entity);
                    continue;
                }

                var currentPosition = transform.ValueRO.Position;
                var targetWaypoint = pathBuffer[pathFollow.ValueRO.CurrentWaypointIndex].Waypoint;

                var directionToWaypoint = targetWaypoint - currentPosition;
                var distanceToWaypoint = math.length(directionToWaypoint);

                //if (distanceToWaypoint < math.sqrt(pathFollow.ValueRO.ArrivalDistanceThresholdSq))
                if (math.distancesq(currentPosition, targetWaypoint) < pathFollow.ValueRO.ArrivalDistanceThresholdSq)
                {
                    pathFollow.ValueRW.CurrentWaypointIndex++;

                    if (pathFollow.ValueRO.CurrentWaypointIndex >= pathBuffer.Length)
                    {
                        ecb.RemoveComponent<PathFollowData>(entity);
                        ecb.RemoveComponent<CalculatedPathBufferElement>(entity);
                        continue;
                    }

                    targetWaypoint = pathBuffer[pathFollow.ValueRO.CurrentWaypointIndex].Waypoint;
                    directionToWaypoint = targetWaypoint - currentPosition;
                    distanceToWaypoint = math.length(directionToWaypoint);
                }

                if (distanceToWaypoint > 0.001f)
                {
                    directionToWaypoint = directionToWaypoint / distanceToWaypoint;

                    var targetRotation = quaternion.LookRotationSafe(directionToWaypoint, math.up());
                    transform.ValueRW.Rotation = math.slerp(transform.ValueRO.Rotation, targetRotation,
                        pathFollow.ValueRO.RotationSpeed * deltaTime);

                    var moveDistance = math.min(pathFollow.ValueRO.MovementSpeed * deltaTime, distanceToWaypoint);
                    transform.ValueRW.Position += directionToWaypoint * moveDistance;
                }

#if UNITY_EDITOR
                if (pathBuffer.Length > 0)
                {
                    if (pathFollow.ValueRO.CurrentWaypointIndex < pathBuffer.Length)
                        Debug.DrawLine(transform.ValueRO.Position,
                            pathBuffer[pathFollow.ValueRO.CurrentWaypointIndex].Waypoint, Color.yellow);

                    for (var i = 0; i < pathBuffer.Length - 1; i++)
                        Debug.DrawLine(pathBuffer[i].Waypoint, pathBuffer[i + 1].Waypoint, Color.cyan);

                    for (var i = 0; i < pathBuffer.Length; i++)
                    {
                        var pointColor = i == pathFollow.ValueRO.CurrentWaypointIndex ? Color.red : Color.green;
                        Debug.DrawRay(pathBuffer[i].Waypoint, Vector3.up * 0.5f, pointColor);
                    }
                }
#endif
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}