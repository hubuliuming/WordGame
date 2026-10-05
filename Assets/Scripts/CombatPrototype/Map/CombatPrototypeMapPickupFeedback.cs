using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapPickupResult : byte
    {
        None,
        PickedUp,
        PlayerMoving,
        AttackInProgress,
        NoLandedTarget,
        NoSpace,
        Failed
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapPickupFeedback : IComponentData
    {
        [GhostField] public uint Sequence;
        [GhostField] public CombatPrototypeMapPickupResult Result;
        [GhostField] public FixedString64Bytes ItemId;
        [GhostField] public int Quantity;
    }
}
