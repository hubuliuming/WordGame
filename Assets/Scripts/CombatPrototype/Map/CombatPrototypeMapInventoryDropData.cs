using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    // Stable command values; never send an inventory row index or a client quantity.
    public enum CombatPrototypeMapInventoryDropKind : byte { None, Apple, Wood, Stone }
    public enum CombatPrototypeMapInventoryDropMode : byte { None, Single, All }
    public enum CombatPrototypeMapInventoryDropResult : byte { None, Success, Rejected, Failed }

    public struct CombatPrototypeMapInventoryDropSettings : IComponentData
    {
        public byte Enabled;
        public int SingleDropQuantity;
        public byte AllowDropAll;
        public float FeedbackSeconds;
        public FixedString64Bytes DropLabel;
        public FixedString64Bytes DropAllLabel;
        public FixedString64Bytes UnavailableLabel;
        public FixedString64Bytes SuccessLabel;
        public FixedString64Bytes RejectedLabel;
        public FixedString64Bytes FailureLabel;
    }

    [InternalBufferCapacity(3)]
    public struct CombatPrototypeMapInventoryDropDefinition : IBufferElementData
    {
        public CombatPrototypeMapInventoryDropKind Kind;
        public FixedString64Bytes ItemId;
        public FixedString64Bytes ItemName;
        public FixedString64Bytes ResourceKey;
        public Entity Prefab;
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapInventoryDropFeedback : IComponentData
    {
        [GhostField] public uint Sequence;
        [GhostField] public CombatPrototypeMapInventoryDropKind Kind;
        [GhostField] public int Quantity;
        [GhostField] public CombatPrototypeMapInventoryDropResult Result;
    }

    public static class CombatPrototypeMapInventoryDropUtility
    {
        public static CombatPrototypeMapInventoryDropKind ResolveKind(string itemId)
        {
            switch (itemId)
            {
                case CombatPrototypeMapYieldItemResolver.VitalityAppleId: return CombatPrototypeMapInventoryDropKind.Apple;
                case CombatPrototypeMapYieldItemResolver.WoodId: return CombatPrototypeMapInventoryDropKind.Wood;
                case CombatPrototypeMapYieldItemResolver.StoneId: return CombatPrototypeMapInventoryDropKind.Stone;
                default: throw new InvalidOperationException("Unsupported inventory drop itemId: " + itemId);
            }
        }
    }
}
