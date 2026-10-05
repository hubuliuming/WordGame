using System;
using Unity.Collections;
using Unity.Entities;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapGatherToolUtility
    {
        internal const string AxeId = "stone_axe";
        internal const string PickaxeId = "stone_pickaxe";
        internal const int MaximumLevel = 3;

        internal static CombatPrototypeMapGatherToolKind ResolveKind(string toolId)
        {
            switch (toolId)
            {
                case AxeId: return CombatPrototypeMapGatherToolKind.Axe;
                case PickaxeId: return CombatPrototypeMapGatherToolKind.Pickaxe;
                default: throw new InvalidOperationException("Unsupported gathering tool: " + toolId);
            }
        }

        internal static CombatPrototypeMapGatherToolDefinition RequireDefinition(
            DynamicBuffer<CombatPrototypeMapGatherToolDefinition> definitions, CombatPrototypeMapGatherToolKind kind)
        {
            foreach (var definition in definitions)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing gathering tool definition: " + kind);
        }

        internal static void ValidateLevel(int level)
        {
            if (level < 1 || level > MaximumLevel)
                throw new InvalidOperationException("Unsupported gathering tool level: " + level);
        }

        internal static CombatPrototypeMapGatherToolUpgradeDefinition RequireUpgradeDefinition(
            DynamicBuffer<CombatPrototypeMapGatherToolUpgradeDefinition> definitions, FixedString64Bytes toolId, int level)
        {
            foreach (var definition in definitions)
                if (definition.ToolId.Equals(toolId) && definition.Level == level) return definition;
            throw new InvalidOperationException("Missing tool upgrade definition: " + toolId + "/" + level);
        }

        internal static CombatPrototypeMapGatherToolDefinition ApplyUpgrade(CombatPrototypeMapGatherToolDefinition definition,
            CombatPrototypeMapGatherToolUpgradeDefinition upgrade)
        {
            definition.MaxDurability = upgrade.MaxDurability;
            definition.DurationMultiplier = upgrade.DurationMultiplier;
            return definition;
        }

        internal static CombatPrototypeMapGatherToolDefinition ForLevel(CombatPrototypeMapGatherToolDefinition definition,
            DynamicBuffer<CombatPrototypeMapGatherToolUpgradeDefinition> upgrades, int level)
        {
            ValidateLevel(level);
            return level == 1 ? definition : ApplyUpgrade(definition, RequireUpgradeDefinition(upgrades, definition.ToolId, level));
        }

        internal static int FindOwned(DynamicBuffer<CombatPrototypeMapGatherTool> tools, FixedString64Bytes toolId)
        {
            for (var index = 0; index < tools.Length; index++)
                if (tools[index].ToolId.Equals(toolId)) return index;
            return -1;
        }

        internal static CombatPrototypeMapGatherToolKind SelectForWork(EntityManager manager, Entity source,
            Entity player, CombatPrototypeMapGatherToolKind kind, float baseDuration, out float duration)
        {
            duration = baseDuration;
            if (manager.GetComponentData<CombatPrototypeMapGatherToolSettings>(source).Enabled == 0)
                return CombatPrototypeMapGatherToolKind.None;
            var definition = RequireDefinition(manager.GetBuffer<CombatPrototypeMapGatherToolDefinition>(source, true), kind);
            var tools = manager.GetBuffer<CombatPrototypeMapGatherTool>(player, true);
            var index = FindOwned(tools, definition.ToolId);
            if (index < 0 || tools[index].Durability < definition.DurabilityCostPerCompletion)
                return CombatPrototypeMapGatherToolKind.None;
            definition = ForLevel(definition, manager.GetBuffer<CombatPrototypeMapGatherToolUpgradeDefinition>(source, true), tools[index].Level);
            duration = baseDuration * definition.DurationMultiplier;
            return kind;
        }

        internal static CombatPrototypeMapGatherTool PrepareConsumption(
            DynamicBuffer<CombatPrototypeMapGatherToolDefinition> definitions,
            DynamicBuffer<CombatPrototypeMapGatherTool> tools, CombatPrototypeMapGatherToolKind kind, out int index)
        {
            var definition = RequireDefinition(definitions, kind);
            index = FindOwned(tools, definition.ToolId);
            if (index < 0 || tools[index].Durability < definition.DurabilityCostPerCompletion)
                throw new InvalidOperationException("Reserved gathering tool is no longer usable: " + definition.ToolId);
            var next = tools[index];
            next.Durability -= definition.DurabilityCostPerCompletion;
            return next;
        }
    }
}
