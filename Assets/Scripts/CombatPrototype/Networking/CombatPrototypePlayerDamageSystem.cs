using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeEnemyAttackSystem))]
    public partial struct CombatPrototypePlayerDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (health, events, owner, player) in SystemAPI.Query<
                         RefRW<CombatPrototypePlayerHealth>, DynamicBuffer<CombatPrototypePlayerDamageEvent>,
                         RefRO<GhostOwner>>().WithAll<CombatPrototypePlayerNetCode>().WithEntityAccess())
            {
                for (var index = 0; index < events.Length; index++)
                {
                    if (health.ValueRO.IsDead != 0)
                        break;
                    var damage = events[index];
                    health.ValueRW.CurrentHealth = math.max(0f, health.ValueRO.CurrentHealth - damage.Damage);
                    health.ValueRW.HitSequence++;
                    health.ValueRW.IsDead = (byte)(health.ValueRO.CurrentHealth <= 0f ? 1 : 0);
                    Debug.Log($"[CombatPrototype.NetCode] Server player={player}, NetworkId={owner.ValueRO.NetworkId} hit by enemy={damage.AttackerEnemy}, attack={damage.AttackSequence}; HP={health.ValueRO.CurrentHealth}/{health.ValueRO.MaxHealth}, hit={health.ValueRO.HitSequence}, dead={health.ValueRO.IsDead}.");
                }
                // Consume events for dead players too; no damage is replayed on a later tick.
                events.Clear();
            }
        }
    }
}
