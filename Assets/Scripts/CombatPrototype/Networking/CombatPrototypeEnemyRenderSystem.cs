using Unity.Entities;
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
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                // Keep hidden enemies in the query so visibility always follows the latest Ghost state.
                visibility.ValueRW = health.ValueRO.IsDead == 0;
            }
        }
    }
}
