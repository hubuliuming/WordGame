using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapDropMergeConfig
    {
        public bool enabled;
        public float mergeDistanceMeters;
        public int maxStackQuantity;
        public float scanIntervalSeconds;
    }
}
