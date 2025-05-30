using shooter_game.scripts.DOTS.Navigation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace shooter_game.scripts.DOTS
{
    public class UnitPathRequesterAuthoring : MonoBehaviour
    {
        public float MovementSpeed = 3;
        public Transform TargetTransform;
        public bool RequestPathOnStart = true;

        private class UnitPathRequesterBaker : Baker<UnitPathRequesterAuthoring>
        {
            public override void Bake(UnitPathRequesterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring.RequestPathOnStart && authoring.TargetTransform != null)
                {
                    AddComponent(entity, new PathfindingRequest
                    {
                        StartPosition = authoring.transform.position,
                        EndPosition = authoring.TargetTransform.position,
                        RequestingEntity = entity
                    });
                    AddComponent(entity, new PathFollowData
                    {
                        MovementSpeed = authoring.MovementSpeed,
                        RotationSpeed = math.radians(180f),
                        ArrivalDistanceThresholdSq = 0.5f * 0.5f,
                        CurrentWaypointIndex = 0
                    });
                }
            }
        }
    }
}