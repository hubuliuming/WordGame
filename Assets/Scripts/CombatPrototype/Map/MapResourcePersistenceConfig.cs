using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapResourcePersistenceConfig
    {
        public bool enabled;
        public string saveSlotId;
        public float saveIntervalSeconds;
    }
}
