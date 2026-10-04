using System;

namespace Code_01.CombatPrototype.Networking
{
    [Serializable]
    public sealed class CombatPrototypePlayerSaveData
    {
        public int Version;
        public string PlayerId;
        public int Coin;
        public int Experience;
        public CombatPrototypePlayerSaveItem[] Items;
        public CombatPrototypePlayerSaveTool[] Tools;
    }

    [Serializable]
    public struct CombatPrototypePlayerSaveItem
    {
        public string ItemName;
        public int Quantity;
    }
}
