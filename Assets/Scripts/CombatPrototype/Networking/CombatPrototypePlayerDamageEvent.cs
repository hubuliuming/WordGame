using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypePlayerDamageEvent : IBufferElementData
    {
        public Entity AttackerEnemy;
        public uint AttackSequence;
        public float Damage;
    }
}
