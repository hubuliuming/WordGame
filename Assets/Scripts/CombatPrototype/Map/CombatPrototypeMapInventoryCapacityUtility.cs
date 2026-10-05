using System;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapInventoryCapacityUtility
    {
        internal static CombatPrototypeMapInventoryCapacityDefinition RequireItemDefinition(
            DynamicBuffer<CombatPrototypeMapInventoryCapacityDefinition> definitions, FixedString64Bytes itemId)
        {
            foreach (var definition in definitions)
                if (definition.ItemId.Equals(itemId)) return definition;
            throw new InvalidOperationException("Missing material capacity item: " + itemId);
        }

        internal static CombatPrototypeMapInventoryCapacityDefinition RequireDefinition(
            DynamicBuffer<CombatPrototypeMapInventoryCapacityDefinition> definitions, FixedString64Bytes itemName)
        {
            foreach (var definition in definitions)
                if (definition.ItemName.Equals(itemName)) return definition;
            throw new InvalidOperationException("Missing material capacity definition: " + itemName);
        }

        // Read-only whole-batch decision, shared by settlement and authoritative owner hints.
        internal static string GetRejection(CombatPrototypeMapInventoryCapacitySettings settings,
            DynamicBuffer<CombatPrototypeMapInventoryCapacityDefinition> definitions,
            DynamicBuffer<CombatPrototypeInventoryItem> inventory, FixedString64Bytes itemName, int quantity)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Material receipt requires positive quantity: " + itemName);
            var incoming = RequireDefinition(definitions, itemName);
            if (settings.Enabled == 0) return null;
            long total = 0;
            long currentQuantity = 0;
            var seen = 0;
            var alreadyOverLimit = false;
            foreach (var item in inventory)
            {
                for (var index = 0; index < definitions.Length; index++)
                {
                    var definition = definitions[index];
                    if (!definition.ItemName.Equals(item.ItemName)) continue;
                    var flag = 1 << index;
                    if ((seen & flag) != 0 || item.Quantity < 0)
                        throw new InvalidOperationException("Material inventory requires unique nonnegative quantities: " +
                            item.ItemName + ", quantity=" + item.Quantity);
                    seen |= flag;
                    total += item.Quantity;
                    alreadyOverLimit |= item.Quantity > definition.MaxQuantity;
                    if (item.ItemName.Equals(itemName)) currentQuantity = item.Quantity;
                    break;
                }
            }
            if (alreadyOverLimit || total > settings.MaxTotalQuantity) return "InventoryAlreadyOverCapacity";
            if (total + quantity > settings.MaxTotalQuantity) return "MaterialTotalCapacityExceeded";
            if (currentQuantity + quantity > incoming.MaxQuantity) return "MaterialItemCapacityExceeded";
            return null;
        }
    }
}
