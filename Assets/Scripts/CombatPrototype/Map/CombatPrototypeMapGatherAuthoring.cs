using Unity.Entities;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public sealed class CombatPrototypeMapGatherAuthoring : MonoBehaviour
    {
        private sealed class Baker : Baker<CombatPrototypeMapGatherAuthoring>
        {
            public override void Bake(CombatPrototypeMapGatherAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CombatPrototypeMapGatherState { PlacementIndex = -1 });
                // Map placement supplies the validated configuration before the Ghost is used.
                AddComponent<CombatPrototypeMapGatherConfig>(entity);
                AddComponent<CombatPrototypeMapGatherProgress>(entity);
            }
        }
    }
}
