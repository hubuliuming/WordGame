using Unity.Entities;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public sealed class CombatPrototypeMapMineAuthoring : MonoBehaviour
    {
        private sealed class Baker : Baker<CombatPrototypeMapMineAuthoring>
        {
            public override void Bake(CombatPrototypeMapMineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CombatPrototypeMapMineState { PlacementIndex = -1 });
                AddComponent<CombatPrototypeMapMineProgress>(entity);
            }
        }
    }
}
