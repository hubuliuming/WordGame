using Unity.Entities;
using Unity.Rendering;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    public partial struct CombatPrototypeMapMineRenderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (mine, visibility) in SystemAPI.Query<
                         RefRO<CombatPrototypeMapMineState>, EnabledRefRW<MaterialMeshInfo>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
                visibility.ValueRW = mine.ValueRO.Phase != CombatPrototypeMapMinePhase.Depleted;
        }
    }
}
