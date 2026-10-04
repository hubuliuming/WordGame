using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapToolRepairResult : byte
    {
        None, Success, Disabled, NotOwned, AlreadyFull, InsufficientMaterials,
        Busy, ExistingOperationHasPriority, PlayerUnavailable, Failed
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapToolRepairFeedback : IComponentData
    {
        [GhostField] public uint Sequence;
        [GhostField] public CombatPrototypeMapGatherToolKind Kind;
        [GhostField] public CombatPrototypeMapToolRepairResult Result;
    }
}
