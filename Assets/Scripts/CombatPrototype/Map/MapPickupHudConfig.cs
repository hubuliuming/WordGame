using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapPickupHudConfig
    {
        public bool enabled;
        public float panelWidthPixels;
        public float panelHeightPixels;
        public float bottomMarginPixels;
        public int fontSize;
        public string pickupLabel;
        public string appleLabel;
        public string woodLabel;
        public string stoneLabel;
        public bool lifetimeEnabled;
        public bool expiryWarningEnabled;
        public float expiryWarningSeconds;
        public string expiresInLabel;
        public string permanentLabel;
        public string expiringSoonLabel;
        public string secondsLabel;
        public string expiryWarningColorHex;
    }
}
