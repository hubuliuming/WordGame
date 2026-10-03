using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapTreeHarvestConfig
    {
        public bool enabled;
        public string treeObjectId;
        public string visualResourceKey;
        public float harvestDurationSeconds;
        public string dropItemId;
        public int dropQuantity;
        public string dropVisualResourceKey;
    }
}
