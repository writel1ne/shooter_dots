using Unity.Entities;
using UnityEngine;

namespace shooter_game.scripts.DOTS.Physics.Player
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float MovementSpeed = 3;
        public Transform TargetTransform;

        private class PlayerAuthoringBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring.TargetTransform != null)
                {
                    // AddComponent(entity, new CapsuleCollider());
                }
            }
        }
    }
}