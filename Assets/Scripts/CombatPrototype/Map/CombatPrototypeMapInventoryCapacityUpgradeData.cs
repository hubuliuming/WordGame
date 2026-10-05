using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapInventoryCapacityUpgradeSettings : IComponentData
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
    }

    public struct CombatPrototypeMapInventoryCapacityUpgradeDefinition : IBufferElementData
    {
        public int Level;
        public int MaxTotalQuantity;
        public int AppleMaxQuantity;
        public int WoodMaxQuantity;
        public int StoneMaxQuantity;
        public int WoodQuantity;
        public int StoneQuantity;
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapInventoryCapacityLevel : IComponentData
    {
        [GhostField] public int Level;
    }

    public enum CombatPrototypeMapInventoryCapacityUpgradeResult : byte
    {
        None, Success, Disabled, MaxLevel, InsufficientMaterials, Busy,
        ExistingOperationHasPriority, PlayerUnavailable, Failed
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapInventoryCapacityUpgradeFeedback : IComponentData
    {
        [GhostField] public uint Sequence;
        [GhostField] public CombatPrototypeMapInventoryCapacityUpgradeResult Result;
    }
}
