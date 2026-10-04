using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapTreePhase : byte { Standing, Chopping, Felled }

    public struct CombatPrototypeMapTreeState : IComponentData
    {
        [GhostField] public int PlacementIndex;
        [GhostField] public CombatPrototypeMapTreePhase Phase;
        [GhostField] public int CollectorNetworkId;
        [GhostField] public uint FelledTick;
    }

    [InternalBufferCapacity(4)]
    public struct CombatPrototypeMapTreeBlockingEvent : IBufferElementData
    {
        [GhostField] public uint TransitionTick;
        [GhostField] public byte Disabled;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeMapTreeSettings : IComponentData
    {
        public byte Enabled;
        public FixedString64Bytes ObjectId;
        public FixedString64Bytes ResourceKey;
        public float InteractionDistance;
        public float HarvestDuration;
        public Entity DropPrefab;
        public FixedString64Bytes DropResourceKey;
        public FixedString64Bytes DropItemId;
        public int DropQuantity;
        public byte RegrowEnabled;
        public float RegrowSeconds;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeMapTreeProgress : IComponentData
    {
        public Entity Collector;
        public uint StartHitSequence;
        public double FinishAt;
        public double RegrowAt;
    }
}
