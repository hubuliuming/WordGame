using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapGatherOutcomeHudConfig
    {
        public bool enabled;
        public float feedbackSeconds;
        public string completedColorHex;
        public string interruptedColorHex;
        public string failedColorHex;
        public string gatherCompletedLabel;
        public string treeCompletedLabel;
        public string mineCompletedLabel;
        public string movingLabel;
        public string attackingLabel;
        public string hitLabel;
        public string outOfRangeLabel;
        public string failedLabel;
    }
}
