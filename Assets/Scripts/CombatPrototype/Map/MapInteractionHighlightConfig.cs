using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapInteractionHighlightConfig
    {
        public bool enabled;
        public bool fTargetsEnabled;
        public bool gTargetsEnabled;
        public float gatherRadiusMeters;
        public float treeRadiusMeters;
        public float mineRadiusMeters;
        public float dropRadiusMeters;
        public float lineWidthPixels;
        public int segmentCount;
        public string readyColorHex;
        public string workingColorHex;
        public string pickupColorHex;
        public float opacity;
        public float heightOffsetMeters;
    }
}
