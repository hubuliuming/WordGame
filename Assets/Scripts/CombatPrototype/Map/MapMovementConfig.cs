using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapMovementConfig
    {
        public float playerRadiusMeters;
        public float enemyRadiusMeters;
        public float collisionSkinMeters;
        public int maxSlideIterations;
    }
}
