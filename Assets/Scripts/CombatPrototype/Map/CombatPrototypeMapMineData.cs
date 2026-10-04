using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapMinePhase : byte { Available, Mining, Depleted }

    public struct CombatPrototypeMapMineState : IComponentData
    {
        [GhostField] public int PlacementIndex;
        [GhostField] public CombatPrototypeMapMinePhase Phase;
        [GhostField] public int CollectorNetworkId;
        [GhostField] public uint MinedTick;
    }

    [InternalBufferCapacity(4)]
    public struct CombatPrototypeMapMineBlockingEvent : IBufferElementData
    {
        [GhostField] public uint TransitionTick;
        [GhostField] public byte Disabled;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeMapMineSettings : IComponentData
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
    public struct CombatPrototypeMapMineProgress : IComponentData
    {
        public Entity Collector;
        public uint StartHitSequence;
        public double FinishAt;
        public CombatPrototypeMapGatherToolKind ToolKind;
        public float ActualDuration;
        public double RegrowAt;
    }
}
