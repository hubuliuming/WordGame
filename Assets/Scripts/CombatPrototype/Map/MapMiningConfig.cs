using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapMiningConfig
    {
        public bool enabled;
        public string mineObjectId;
        public string visualResourceKey;
        public float harvestDurationSeconds;
        public string dropItemId;
        public int dropQuantity;
        public string dropVisualResourceKey;
    }
}
