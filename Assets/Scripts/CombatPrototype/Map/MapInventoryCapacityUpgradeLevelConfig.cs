using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapInventoryCapacityUpgradeLevelConfig
    {
        public int level;
        public int maxTotalQuantity;
        public MapInventoryCapacityItemConfig[] items;
        public int woodQuantity;
        public int stoneQuantity;
    }
}
