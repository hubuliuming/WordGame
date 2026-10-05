using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapInteractionFailureHudConfig
    {
        public bool enabled;
        public float feedbackSeconds;
        public string errorColorHex;
        public string alreadyInteractingLabel;
        public string movingLabel;
        public string attackingLabel;
        public string noTargetLabel;
        public string targetUnavailableLabel;
        public string failedLabel;
    }
}
