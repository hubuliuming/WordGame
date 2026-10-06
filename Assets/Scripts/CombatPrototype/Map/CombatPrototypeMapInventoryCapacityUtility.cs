using System;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapInventoryCapacityUtility
    {
        internal const int MaximumLevel = 3;
        private static readonly FixedString64Bytes AppleId = new FixedString64Bytes(CombatPrototypeMapYieldItemResolver.VitalityAppleId);
        private static readonly FixedString64Bytes WoodId = new FixedString64Bytes(CombatPrototypeMapYieldItemResolver.WoodId);
        private static readonly FixedString64Bytes StoneId = new FixedString64Bytes(CombatPrototypeMapYieldItemResolver.StoneId);

        internal static void ValidateLevel(int level)
        {
            if (level < 1 || level > MaximumLevel) throw new InvalidOperationException("Invalid material capacity level: " + level);
        }

        internal static CombatPrototypeMapInventoryCapacityUpgradeDefinition RequireUpgradeDefinition(
            DynamicBuffer<CombatPrototypeMapInventoryCapacityUpgradeDefinition> definitions, int level)
        {
            foreach (var definition in definitions) if (definition.Level == level) return definition;
            throw new InvalidOperationException("Missing material capacity upgrade level: " + level);
        }

        private static int Maximum(CombatPrototypeMapInventoryCapacityUpgradeDefinition upgrade, FixedString64Bytes itemId)
        {
            if (itemId.Equals(AppleId)) return upgrade.AppleMaxQuantity;
            if (itemId.Equals(WoodId)) return upgrade.WoodMaxQuantity;
            if (itemId.Equals(StoneId)) return upgrade.StoneMaxQuantity;
            throw new InvalidOperationException("Unknown material capacity upgrade item: " + itemId);
        }

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
            DynamicBuffer<CombatPrototypeMapInventoryCapacityUpgradeDefinition> upgradeDefinitions, int capacityLevel,
            DynamicBuffer<CombatPrototypeInventoryItem> inventory, FixedString64Bytes itemName, int quantity)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Material receipt requires positive quantity: " + itemName);
            ValidateLevel(capacityLevel);
            var incoming = RequireDefinition(definitions, itemName);
            if (settings.Enabled == 0) return null;
            var upgrade = capacityLevel == 1 ? default : RequireUpgradeDefinition(upgradeDefinitions, capacityLevel);
            var totalMaximum = capacityLevel == 1 ? settings.MaxTotalQuantity : upgrade.MaxTotalQuantity;
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
                    var maximum = capacityLevel == 1 ? definition.MaxQuantity : Maximum(upgrade, definition.ItemId);
                    alreadyOverLimit |= item.Quantity > maximum;
                    if (item.ItemName.Equals(itemName)) currentQuantity = item.Quantity;
                    break;
                }
            }
            if (alreadyOverLimit || total > totalMaximum) return "InventoryAlreadyOverCapacity";
            if (total + quantity > totalMaximum) return "MaterialTotalCapacityExceeded";
            var incomingMaximum = capacityLevel == 1 ? incoming.MaxQuantity : Maximum(upgrade, incoming.ItemId);
            if (currentQuantity + quantity > incomingMaximum) return "MaterialItemCapacityExceeded";
            return null;
        }

        // G may receive the available part; the original whole-batch decision remains the authority for F.
        internal static int GetPickupQuantity(CombatPrototypeMapInventoryCapacitySettings settings,
            DynamicBuffer<CombatPrototypeMapInventoryCapacityDefinition> definitions,
            DynamicBuffer<CombatPrototypeMapInventoryCapacityUpgradeDefinition> upgradeDefinitions, int capacityLevel,
            DynamicBuffer<CombatPrototypeInventoryItem> inventory, FixedString64Bytes itemName, int quantity,
            bool partialPickupEnabled, out string rejection)
        {
            rejection = GetRejection(settings, definitions, upgradeDefinitions, capacityLevel, inventory, itemName, quantity);
            if (rejection == null) return quantity;
            if (!partialPickupEnabled || rejection == "InventoryAlreadyOverCapacity") return 0;

            // Only a whole-stack capacity rejection reaches here; the first pass has validated this inventory.
            var incoming = RequireDefinition(definitions, itemName);
            var upgrade = capacityLevel == 1 ? default : RequireUpgradeDefinition(upgradeDefinitions, capacityLevel);
            var totalMaximum = capacityLevel == 1 ? settings.MaxTotalQuantity : upgrade.MaxTotalQuantity;
            var incomingMaximum = capacityLevel == 1 ? incoming.MaxQuantity : Maximum(upgrade, incoming.ItemId);
            long total = 0;
            long currentQuantity = 0;
            foreach (var item in inventory)
            {
                foreach (var definition in definitions)
                {
                    if (!definition.ItemName.Equals(item.ItemName)) continue;
                    total += item.Quantity;
                    if (item.ItemName.Equals(itemName)) currentQuantity = item.Quantity;
                    break;
                }
            }
            var remaining = Math.Min((long)totalMaximum - total, (long)incomingMaximum - currentQuantity);
            var received = (int)Math.Min((long)quantity, remaining);
            if (received > 0) rejection = null;
            return received;
        }
    }
}
