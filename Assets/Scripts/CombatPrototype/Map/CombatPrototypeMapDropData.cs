using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapDropPhase : byte { Airborne, Landed, Consumed }

    public struct CombatPrototypeMapDropState : IComponentData
    {
        [GhostField] public int DropId;
        [GhostField] public FixedString64Bytes ItemId;
        [GhostField] public int Quantity;
        [GhostField] public CombatPrototypeMapDropPhase Phase;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeMapDropSettings : IComponentData
    {
        public byte Enabled;
        public Entity Prefab;
        public FixedString64Bytes ResourceKey;
        public FixedString64Bytes ItemId;
        public FixedString64Bytes ItemName;
        public int Quantity;
        public float PickupDistance;
        public float FlightDuration;
        public float ScatterRadius;
        public float ArcHeight;
        public float GroundOffset;
        public float VisualScale;
        public float Lifetime;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeMapDropProgress : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public double StartedAt;
        public double ExpiresAt;
        public byte CleanupQueued;
    }
}
