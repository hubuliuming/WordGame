using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeMapDropMergeSettings : IComponentData
    {
        public byte Enabled;
        public float MergeDistance;
        public int MaxStackQuantity;
        public float ScanInterval;
    }
}
