using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapInteractionHudConfig
    {
        public bool enabled;
        public float panelWidthPixels;
        public float panelHeightPixels;
        public float bottomMarginPixels;
        public int fontSize;
        public float progressBarHeightPixels;
        public string gatherLabel;
        public string treeLabel;
        public string mineLabel;
    }
}
