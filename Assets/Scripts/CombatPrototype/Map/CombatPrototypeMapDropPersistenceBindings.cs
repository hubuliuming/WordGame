using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapDropPersistenceBindings
    {
        internal static CombatPrototypeMapInventoryDropDefinition[] Read(EntityManager manager, Entity source)
        {
            var buffer = manager.GetBuffer<CombatPrototypeMapInventoryDropDefinition>(source, true);
            var definitions = new CombatPrototypeMapInventoryDropDefinition[buffer.Length];
            for (var index = 0; index < buffer.Length; index++)
            {
                var definition = buffer[index];
                CombatPrototypeMapYieldItemResolver.Resolve(definition.ItemId.ToString());
                for (var previous = 0; previous < index; previous++)
                    if (definitions[previous].ItemId == definition.ItemId)
                        throw new InvalidOperationException("Duplicate drop restore binding; itemId=" + definition.ItemId);
                var prefab = definition.Prefab;
                if (!manager.HasComponent<Prefab>(prefab) || !manager.HasComponent<GhostType>(prefab) ||
                    !manager.HasComponent<LocalTransform>(prefab) || !manager.HasComponent<CombatPrototypeMapDropState>(prefab) ||
                    !manager.HasComponent<CombatPrototypeMapDropProgress>(prefab) ||
                    manager.GetComponentData<CombatPrototypeMapDropState>(prefab).Phase != CombatPrototypeMapDropPhase.Prepared)
                    throw new InvalidOperationException("Required drop restore prefab is invalid; itemId=" + definition.ItemId +
                        ", resource=" + definition.ResourceKey + ", prefab=" + prefab);
                definitions[index] = definition;
            }
            return definitions;
        }

        internal static CombatPrototypeMapInventoryDropDefinition Resolve(
            CombatPrototypeMapInventoryDropDefinition[] definitions, FixedString64Bytes itemId)
        {
            foreach (var definition in definitions) if (definition.ItemId == itemId) return definition;
            throw new InvalidOperationException("Required explicit drop restore binding is missing; itemId=" + itemId);
        }
    }
}
