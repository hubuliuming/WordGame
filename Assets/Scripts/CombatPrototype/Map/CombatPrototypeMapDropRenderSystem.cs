using Unity.Entities;
using Unity.Rendering;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    public partial struct CombatPrototypeMapDropRenderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (drop, visibility) in SystemAPI.Query<
                         RefRO<CombatPrototypeMapDropState>, EnabledRefRW<MaterialMeshInfo>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
                visibility.ValueRW = drop.ValueRO.Phase != CombatPrototypeMapDropPhase.Consumed &&
                    drop.ValueRO.Phase != CombatPrototypeMapDropPhase.Prepared;
        }
    }
}
