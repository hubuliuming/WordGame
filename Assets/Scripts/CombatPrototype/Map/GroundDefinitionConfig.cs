using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class GroundDefinitionConfig
    {
        public string groundId;
        public string visualResourceKey;
        public bool walkable;
        public float movementMultiplier;
    }
}