using System;

namespace Code_01.CombatPrototype.Networking
{
    [Serializable]
    public struct CombatPrototypePlayerSaveTool
    {
        public string ToolId;
        public int Durability;
        public int Level;
    }
}
