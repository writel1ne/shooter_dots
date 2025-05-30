using Unity.Entities;
using Unity.Physics;
using Unity.Scenes;

// Essential for SceneReference, RequestSceneLoaded etc.

namespace shooter_game.scripts.DOTS
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    //[CreateBefore(typeof(OctreeManagerSystem))]
    public partial class SubSceneLoaderSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            RequireForUpdate<PhysicsWorldSingleton>();
            // Entities.WithStructuralChanges().ForEach((Entity entity, SubScene scene) =>
            // {
            //     if (!scene.IsLoaded)
            //     {
            //         if (EntityManager.HasComponent<RequestSceneLoaded>(entity))
            //         {
            //             //EntityManager.RemoveComponent<RequestSceneLoaded>(entity);
            //         }
            //         else
            //         {
            //             EntityManager.AddComponent<RequestSceneLoaded>(entity);
            //         }
            //     }
            // }).WithoutBurst().Run();


            Enabled = false;
        }

        // protected override void OnUpdate() {
        //     //this.Enabled = false;
        // }
    }

    // public partial struct SubSceneLoaderSystemNew : ISystem
    // {
    //     public void OnCreate(ref SystemState state)
    //     {
    //         foreach (var query in SystemAPI.Query<LocalTransform>().WithEntityAccess())
    //         {
    //         }
    //     }
    // }
}