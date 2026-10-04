using Unity.Entities;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public sealed class CombatPrototypeMapDropAuthoring : MonoBehaviour
    {
        private sealed class Baker : Baker<CombatPrototypeMapDropAuthoring>
        {
            public override void Bake(CombatPrototypeMapDropAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CombatPrototypeMapDropState { Phase = CombatPrototypeMapDropPhase.Prepared });
                AddComponent<CombatPrototypeMapDropProgress>(entity);
            }
        }
    }
}
