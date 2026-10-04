using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapGatherToolsConfig
    {
        public bool enabled;
        public float craftFeedbackSeconds;
        public bool repairEnabled;
        public float repairFeedbackSeconds;
        public MapGatherToolDefinitionConfig[] tools;
    }

    [Serializable]
    public sealed class MapGatherToolDefinitionConfig
    {
        public string toolId;
        public string displayName;
        public string targetKind;
        public int maxDurability;
        public int durabilityCostPerCompletion;
        public float durationMultiplier;
        public int craftWoodQuantity;
        public int craftStoneQuantity;
        public int repairDurability;
        public int repairWoodQuantity;
        public int repairStoneQuantity;
    }
}
