using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapGatherToolDurabilityHudConfig
    {
        public bool enabled;
        public float warningRatio;
        public float criticalRatio;
        public string warningColorHex;
        public string criticalColorHex;
        public string brokenColorHex;
        public string warningLabel;
        public string criticalLabel;
        public string brokenLabel;
        public string remainingUsesLabel;
        public string repairHintLabel;
    }
}
