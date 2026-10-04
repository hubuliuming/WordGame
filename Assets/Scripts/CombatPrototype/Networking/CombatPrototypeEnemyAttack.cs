using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    public enum CombatPrototypeEnemyAttackPhase : byte
    {
        Ready,
        Startup,
        Recovery
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeEnemyAttackConfig : IComponentData
    {
        public float Damage;
        public float Range;
        public float StartupSeconds;
        public float RecoverySeconds;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeEnemyAttackState : IComponentData
    {
        public CombatPrototypeEnemyAttackPhase Phase;
        public float PhaseTimer;
        public uint AttackSequence;
        public byte SwingStarted;
        public Entity TargetPlayer;
        public int TargetNetworkId;
    }
}
