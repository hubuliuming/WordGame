using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapGatherOutcomeResult : byte
    {
        None,
        Completed,
        PlayerMoving,
        AttackInProgress,
        PlayerHit,
        OutOfRange,
        NoSpace,
        Failed
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapGatherOutcomeFeedback : IComponentData
    {
        [GhostField] public uint Sequence;
        [GhostField] public byte Kind;
        [GhostField] public CombatPrototypeMapGatherOutcomeResult Result;
    }
}
