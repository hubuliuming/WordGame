using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapGatherToolUpgradeSettings : IComponentData
    {
        public byte Enabled;
        public float FeedbackSeconds;
        public FixedString64Bytes UpgradeLabel;
        public FixedString64Bytes UpgradeButtonLabel;
        public FixedString64Bytes LevelLabel;
        public FixedString64Bytes MaxLevelLabel;
        public FixedString64Bytes SuccessLabel;
        public FixedString64Bytes RejectedLabel;
        public FixedString64Bytes FailureLabel;
        public FixedString64Bytes RecraftLabel;
    }

    public struct CombatPrototypeMapGatherToolUpgradeDefinition : IBufferElementData
    {
        public FixedString64Bytes ToolId;
        public int Level;
        public int MaxDurability;
        public float DurationMultiplier;
        public int WoodQuantity;
        public int StoneQuantity;
    }

    public enum CombatPrototypeMapToolUpgradeResult : byte
    {
        None, Success, Disabled, NotOwned, MaxLevel, InsufficientMaterials, Busy,
        ExistingOperationHasPriority, PlayerUnavailable, Failed
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapToolUpgradeFeedback : IComponentData
    {
        [GhostField] public uint Sequence;
        [GhostField] public CombatPrototypeMapGatherToolKind Kind;
        [GhostField] public CombatPrototypeMapToolUpgradeResult Result;
    }
}
