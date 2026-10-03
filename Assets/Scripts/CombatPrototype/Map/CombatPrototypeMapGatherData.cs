using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapGatherPhase : byte { Available, Collecting, Depleted }

    public struct CombatPrototypeMapGatherState : IComponentData
    {
        [GhostField] public int PlacementIndex;
        [GhostField] public CombatPrototypeMapGatherPhase Phase;
        [GhostField] public int CollectorNetworkId;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeMapGatherConfig : IComponentData
    {
        public FixedString64Bytes ObjectId;
        public float InteractionDistance;
        public float GatherDuration;
        public FixedString64Bytes YieldItemName;
        public int YieldQuantity;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeMapGatherProgress : IComponentData
    {
        public Entity Collector;
        public uint StartHitSequence;
        public double FinishAt;
    }
}
