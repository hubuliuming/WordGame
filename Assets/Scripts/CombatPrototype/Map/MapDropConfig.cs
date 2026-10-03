using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapDropConfig
    {
        public bool enabled;
        public string itemId;
        public int quantity;
        public string visualResourceKey;
        public float pickupDistanceMeters;
        public float flightDurationSeconds;
        public float scatterRadiusMeters;
        public float arcHeightMeters;
        public float groundOffsetMeters;
        public float visualScale;
        public float lifetimeSeconds;
    }
}
