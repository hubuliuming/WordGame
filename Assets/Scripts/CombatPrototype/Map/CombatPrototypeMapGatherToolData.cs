using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapGatherToolKind : byte { None, Axe, Pickaxe }
    public enum CombatPrototypeMapToolCraftResult : byte
    {
        None, Success, Disabled, AlreadyUsable, InsufficientMaterials, Busy, FHasPriority, PlayerUnavailable, Failed
    }

    [InternalBufferCapacity(2)]
    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapGatherTool : IBufferElementData
    {
        [GhostField] public FixedString64Bytes ToolId;
        [GhostField] public int Durability;
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapToolCraftFeedback : IComponentData
    {
        [GhostField] public uint Sequence;
        [GhostField] public CombatPrototypeMapGatherToolKind Kind;
        [GhostField] public CombatPrototypeMapToolCraftResult Result;
    }

    public struct CombatPrototypeMapGatherToolSettings : IComponentData
    {
        public byte Enabled;
        public float CraftFeedbackSeconds;
    }

    [InternalBufferCapacity(2)]
    public struct CombatPrototypeMapGatherToolDefinition : IBufferElementData
    {
        public FixedString64Bytes ToolId;
        public FixedString64Bytes DisplayName;
        public CombatPrototypeMapGatherToolKind Kind;
        public int MaxDurability;
        public int DurabilityCostPerCompletion;
        public float DurationMultiplier;
        public int CraftWoodQuantity;
        public int CraftStoneQuantity;
    }
}
