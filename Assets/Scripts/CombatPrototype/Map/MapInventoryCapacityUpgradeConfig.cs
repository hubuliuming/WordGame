using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapInventoryCapacityUpgradeConfig
    {
        public bool enabled;
        public float feedbackSeconds;
        public string upgradeLabel;
        public string upgradeButtonLabel;
        public string levelLabel;
        public string maxLevelLabel;
        public string successLabel;
        public string rejectedLabel;
        public string failureLabel;
        public MapInventoryCapacityUpgradeLevelConfig[] levels;
    }
}
