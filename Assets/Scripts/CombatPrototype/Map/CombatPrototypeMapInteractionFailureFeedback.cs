using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapInteractionFailureResult : byte
    {
        None,
        AlreadyInteracting,
        AttackInProgress,
        PlayerMoving,
        NoAvailableTarget,
        NoSpace,
        TargetUnavailable,
        Failed
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapInteractionFailureFeedback : IComponentData
    {
        [GhostField] public uint Sequence;
        [GhostField] public CombatPrototypeMapInteractionFailureResult Result;
    }
}
