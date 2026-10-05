using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapWorldSaveHudConfig
    {
        public bool enabled;
        public float panelWidthPixels;
        public float panelHeightPixels;
        public float bottomMarginPixels;
        public int fontSize;
        public float feedbackSeconds;
        public string disabledLabel;
        public string notSavedLabel;
        public string savedLabel;
        public string captureFailedLabel;
        public string saveFailedLabel;
        public string manualSaveLabel;
        public string manualDisabledLabel;
        public string cooldownLabel;
        public string unavailableLabel;
        public string errorColorHex;
    }
}
