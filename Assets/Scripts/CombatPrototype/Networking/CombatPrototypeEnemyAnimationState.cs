using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypeEnemyAnimationState : IComponentData
    {
        [GhostField] public CombatPrototypeEnemyAttackPhase Phase;
        [GhostField] public uint AttackSequence;
        [GhostField] public byte SwingStarted;
        [GhostField(Quantization = 1000)] public float PhaseRemaining;
        [GhostField(Quantization = 1000)] public float StartupSeconds;
        [GhostField(Quantization = 1000)] public float RecoverySeconds;
    }
}
