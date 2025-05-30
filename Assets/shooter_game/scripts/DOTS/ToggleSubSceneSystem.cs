namespace shooter_game.scripts.DOTS
{
    // [UpdateInGroup(typeof(SimulationSystemGroup))]
    // public partial class ToggleSubSceneSystem : SystemBase {
    //     protected override void OnUpdate() {
    //         Entities.WithStructuralChanges().ForEach((Entity entity, SubScene scene) => {
    //             if (EntityManager.HasComponent<RequestSceneLoaded>(entity)) {
    //                 //EntityManager.RemoveComponent<RequestSceneLoaded>(entity);
    //             } else {
    //                 EntityManager.AddComponent<RequestSceneLoaded>(entity);
    //             }
    //         }).WithoutBurst().Run();
    //
    //         this.Enabled = false;
    //     }
    // }
}