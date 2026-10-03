using Unity.Entities;
using Unity.Rendering;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    public partial struct CombatPrototypeMapTreeRenderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (tree, visibility) in SystemAPI.Query<
                         RefRO<CombatPrototypeMapTreeState>, EnabledRefRW<MaterialMeshInfo>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
                visibility.ValueRW = tree.ValueRO.Phase != CombatPrototypeMapTreePhase.Felled;
        }
    }
}
