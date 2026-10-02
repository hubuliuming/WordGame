using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypePlayerNetCode : IComponentData
    {
        public float MoveSpeed;
    }

    public struct CombatPrototypeMeleeConfig : IComponentData
    {
        public int AttackPowerCost;
        public float Damage;
        public float Range;
        public float Angle;
        public float StartupSeconds;
        public float ActiveSeconds;
        public float RecoverySeconds;
    }

    public enum CombatPrototypeAttackPhase : byte
    {
        Ready,
        Startup,
        Active,
        Recovery
    }

    public struct CombatPrototypeMeleeState : IComponentData
    {
        [GhostField] public CombatPrototypeAttackPhase Phase;
        [GhostField] public uint AttackSequence;
        public float PhaseTimer;
    }
}