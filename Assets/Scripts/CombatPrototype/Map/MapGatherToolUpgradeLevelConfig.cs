using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapGatherToolUpgradeLevelConfig
    {
        public string toolId;
        public int level;
        public int maxDurability;
        public float durationMultiplier;
        public int woodQuantity;
        public int stoneQuantity;
    }
}
