using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeDamageEvent : IBufferElementData
    {
        public int AttackerNetworkId;
        public uint AttackSequence;
        public float Damage;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMeleeServerSystem))]
    public partial struct CombatPrototypeDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (health, events, entity) in SystemAPI.Query<
                         RefRW<CombatPrototypeEnemyState>, DynamicBuffer<CombatPrototypeDamageEvent>>().WithEntityAccess())
            {
                for (var index = 0; index < events.Length; index++)
                {
                    if (health.ValueRO.IsDead != 0)
                        break;
                    var damage = events[index];
                    health.ValueRW.CurrentHealth = math.max(0f, health.ValueRO.CurrentHealth - damage.Damage);
                    health.ValueRW.HitSequence++;
                    health.ValueRW.IsDead = (byte)(health.ValueRO.CurrentHealth <= 0f ? 1 : 0);
                    Debug.Log($"[CombatPrototype.NetCode] Server enemy={entity} hit by NetworkId={damage.AttackerNetworkId}, attack={damage.AttackSequence}; HP={health.ValueRO.CurrentHealth}, hit={health.ValueRO.HitSequence}, dead={health.ValueRO.IsDead}.");
                }
                // Dead-target events are consumed too; nothing can replay on a later tick.
                events.Clear();
            }
        }
    }
}