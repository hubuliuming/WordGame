using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class CombatPrototypeMapDropSaveEntry
    {
        public int DropId;
        public string ItemId;
        public int Quantity;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public bool HasExpiry;
        public double RemainingLifetimeSeconds;
    }
}
