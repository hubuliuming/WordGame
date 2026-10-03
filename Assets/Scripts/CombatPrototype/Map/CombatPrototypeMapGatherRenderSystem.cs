using Unity.Entities;
using Unity.Rendering;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    public partial struct CombatPrototypeMapGatherRenderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (gather, visibility) in SystemAPI.Query<
                         RefRO<CombatPrototypeMapGatherState>, EnabledRefRW<MaterialMeshInfo>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
                visibility.ValueRW = gather.ValueRO.Phase != CombatPrototypeMapGatherPhase.Depleted;
        }
    }
}
