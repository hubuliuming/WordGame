using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapPickupFeedbackHudConfig
    {
        public bool enabled;
        public float feedbackSeconds;
        public string successColorHex;
        public string failureColorHex;
        public string successLabel;
        public string movingLabel;
        public string attackingLabel;
        public string noTargetLabel;
        public string failedLabel;
    }
}
