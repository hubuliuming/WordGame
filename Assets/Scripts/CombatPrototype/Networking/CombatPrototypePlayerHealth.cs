using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypePlayerHealth : IComponentData
    {
        [GhostField] public float CurrentHealth;
        [GhostField] public float MaxHealth;
        [GhostField] public uint HitSequence;
        [GhostField] public byte IsDead;
    }
}
