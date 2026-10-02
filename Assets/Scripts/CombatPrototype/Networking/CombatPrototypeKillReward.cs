using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeKillRewardConfig : IComponentData
    {
        public int Coin;
        public int Experience;
        public FixedString64Bytes ItemName;
        public int ItemQuantity;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeKillRewardEvent : IBufferElementData
    {
        public int AttackerNetworkId;
        public uint AttackSequence;
        public int Coin;
        public int Experience;
        public FixedString64Bytes ItemName;
        public int ItemQuantity;
    }
}
