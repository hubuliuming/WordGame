using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class CombatPrototypeMapResourceSaveData
    {
        public int Version;
        public string SaveSlotId;
        public string MapDefinitionId;
        public uint Seed;
        public int ConfigRevision;
        public string LayoutSignature;
        public CombatPrototypeMapResourceSaveEntry[] Resources;
        public int LastDropId;
        public CombatPrototypeMapDropSaveEntry[] Drops;
    }

    [Serializable]
    public sealed class CombatPrototypeMapResourceSaveEntry
    {
        public string Kind;
        public int PlacementIndex;
        public string ObjectId;
        public double RemainingSeconds;
    }
}
