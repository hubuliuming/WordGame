using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapInventoryCapacityConfig
    {
        public bool enabled;
        public int maxTotalQuantity;
        public MapInventoryCapacityItemConfig[] items;
    }
}
