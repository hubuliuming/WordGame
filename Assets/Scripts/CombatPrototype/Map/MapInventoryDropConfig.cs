using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapInventoryDropConfig
    {
        public bool enabled;
        public int singleDropQuantity;
        public bool allowDropAll;
        public float feedbackSeconds;
        public string dropLabel;
        public string dropAllLabel;
        public string unavailableLabel;
        public string successLabel;
        public string rejectedLabel;
        public string failureLabel;
        public MapInventoryDropItemConfig[] items;
    }

    [Serializable]
    public sealed class MapInventoryDropItemConfig
    {
        public string itemId;
        public string visualResourceKey;
    }
}
