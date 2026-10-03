using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapObjectDefinitionConfig
    {
        public string objectId;
        public string visualResourceKey;
        public float footprintRadiusMeters;
        public float minimumSameTypeSpacingMeters;
        public float interactionDistanceMeters;
        public bool blocksMovement;
        public bool blocksMelee;
        public bool blocksProjectile;
        public bool gatherable;
        public float gatherDurationSeconds;
        public string yieldItemId;
        public int yieldQuantity;
        public bool regrowEnabled;
        public float regrowSeconds;
    }
}