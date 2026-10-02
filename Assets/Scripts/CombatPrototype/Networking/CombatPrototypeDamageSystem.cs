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
            var rewardConfigs = SystemAPI.GetComponentLookup<CombatPrototypeKillRewardConfig>(true);
            var rewardEvents = SystemAPI.GetBufferLookup<CombatPrototypeKillRewardEvent>();
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
                    if (health.ValueRO.IsDead != 0)
                        QueueReward(entity, damage, rewardConfigs, rewardEvents);
                }
                // Dead-target events are consumed too; nothing can replay on a later tick.
                events.Clear();
            }
        }

        private static void QueueReward(Entity enemy, CombatPrototypeDamageEvent damage,
            ComponentLookup<CombatPrototypeKillRewardConfig> configs,
            BufferLookup<CombatPrototypeKillRewardEvent> events)
        {
            if (!configs.HasComponent(enemy) || !events.HasBuffer(enemy))
            {
                Debug.LogError($"[CombatPrototype.NetCode] Server reward event failed; enemy={enemy}, NetworkId={damage.AttackerNetworkId}, attack={damage.AttackSequence}, reason=MissingEnemyRewardData.");
                return;
            }
            try
            {
                var config = configs[enemy];
                events[enemy].Add(new CombatPrototypeKillRewardEvent
                {
                    AttackerNetworkId = damage.AttackerNetworkId,
                    AttackSequence = damage.AttackSequence,
                    Coin = config.Coin,
                    Experience = config.Experience,
                    ItemName = config.ItemName,
                    ItemQuantity = config.ItemQuantity
                });
                Debug.Log($"[CombatPrototype.NetCode] Server reward queued; enemy={enemy}, NetworkId={damage.AttackerNetworkId}, attack={damage.AttackSequence}, coin={config.Coin}, experience={config.Experience}, item={config.ItemName}, quantity={config.ItemQuantity}.");
            }
            catch (global::System.Exception exception)
            {
                Debug.LogError($"[CombatPrototype.NetCode] Server reward event failed; enemy={enemy}, NetworkId={damage.AttackerNetworkId}, attack={damage.AttackSequence}; {exception}");
            }
        }
    }
}
