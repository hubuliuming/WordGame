using Unity.Entities;
using Unity.NetCode.Hybrid;
using Unity.Rendering;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    public partial struct CombatPrototypeEnemyRenderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (health, visibility) in SystemAPI.Query<
                         RefRO<CombatPrototypeEnemyState>, EnabledRefRW<MaterialMeshInfo>>()
                         .WithNone<GhostPresentationGameObjectPrefabReference>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                // Keep hidden enemies in the query so visibility always follows the latest Ghost state.
                visibility.ValueRW = health.ValueRO.IsDead == 0;
            }

            foreach (var visibility in SystemAPI.Query<EnabledRefRW<MaterialMeshInfo>>()
                         .WithAll<CombatPrototypeEnemyState, GhostPresentationGameObjectPrefabReference>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                // Animated views own visibility; retain but never draw their ECS placeholder mesh.
                visibility.ValueRW = false;
            }
        }
    }
}
