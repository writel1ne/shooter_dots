// Файл: UnitPathRequesterAuthoring.cs

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
// Для float3

// Для PathfindingRequest

namespace shooter_game.scripts.DOTS.Navigation
{
    public class UnitPathRequesterAuthoring : MonoBehaviour
    {
        public Transform TargetTransform; // Куда юнит должен лететь
        public bool RequestPathOnStart = true;

        class UnitPathRequesterBaker : Baker<UnitPathRequesterAuthoring>
        {
            public override void Bake(UnitPathRequesterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic); // Юнит будет двигаться

                // Добавляем компонент, который будет хранить ID целевой сущности, если цель - другая сущность.
                // Или просто хранить целевую позицию, если цель статична.
                // Здесь для простоты пока не делаем сложную логику слежения за целью.

                // Если нужно запросить путь сразу при старте
                if (authoring.RequestPathOnStart && authoring.TargetTransform != null)
                {
                    AddComponent(entity, new PathfindingRequest
                    {
                        // StartPosition будет текущей позицией юнита.
                        // Это нужно будет установить динамически системой, которая управляет юнитом,
                        // или при создании запроса. Для простого теста можно взять позицию MonoBehaviour.
                        // В реальной системе юнит уже будет сущностью с LocalToWorld.
                        StartPosition = authoring.transform.position, 
                        EndPosition = authoring.TargetTransform.position,
                        RequestingEntity = entity // Сущность, которой нужен путь
                    });
                    AddComponent(entity, new PathFollowData
                    {
                        MovementSpeed = 5f, // Настройте
                        RotationSpeed = math.radians(180f), // Настройте
                        ArrivalDistanceThresholdSq = 0.5f * 0.5f, // Настройте
                        CurrentWaypointIndex = 0 // Начнется с 0, когда путь будет рассчитан
                    });
                }
                // Добавьте сюда компоненты для самого юнита: скорость, повороты, маркерный компонент "FlyingUnit" и т.д.
                // AddComponent<UnitMovementStats>(entity, ...);
            }
        }
    }
}