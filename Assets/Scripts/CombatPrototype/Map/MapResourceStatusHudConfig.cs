using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapResourceStatusHudConfig
    {
        public bool enabled;
        public float panelWidthPixels;
        public float panelHeightPixels;
        public float bottomMarginPixels;
        public int fontSize;
        public string availableLabel;
        public string workingLabel;
        public string occupiedLabel;
        public string regrowingLabel;
        public string waitingLabel;
        public string depletedLabel;
    }
}
