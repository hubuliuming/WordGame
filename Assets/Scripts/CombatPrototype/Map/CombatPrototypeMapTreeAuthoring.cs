using Unity.Entities;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public sealed class CombatPrototypeMapTreeAuthoring : MonoBehaviour
    {
        private sealed class Baker : Baker<CombatPrototypeMapTreeAuthoring>
        {
            public override void Bake(CombatPrototypeMapTreeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CombatPrototypeMapTreeState { PlacementIndex = -1 });
                AddComponent<CombatPrototypeMapTreeProgress>(entity);
            }
        }
    }
}
