using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    public sealed class CombatPrototypeEnemyNetCodeAuthoring : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float MoveSpeed = 2f;
        public float StopDistance = 1.5f;

        private sealed class Baker : Baker<CombatPrototypeEnemyNetCodeAuthoring>
        {
            public override void Bake(CombatPrototypeEnemyNetCodeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CombatPrototypeEnemyMovement
                {
                    MoveSpeed = authoring.MoveSpeed,
                    StopDistance = authoring.StopDistance
                });
                AddComponent<CombatPrototypeEnemyTarget>(entity);
                AddBuffer<CombatPrototypeDamageEvent>(entity);
                AddComponent(entity, new CombatPrototypeEnemyState
                {
                    CurrentHealth = authoring.MaxHealth
                });
            }
        }
    }

    public struct CombatPrototypeEnemyState : IComponentData
    {
        [GhostField] public float CurrentHealth;
        [GhostField] public uint HitSequence;
        [GhostField] public byte IsDead;
    }
}