using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeRewardSystem))]
    public partial struct CombatPrototypeEnemyAttackSystem : ISystem
    {
        private struct OnlinePlayer
        {
            public Entity Entity;
            public int NetworkId;
            public float3 Position;
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        public void OnUpdate(ref SystemState state)
        {
            using var players = new NativeList<OnlinePlayer>(Allocator.Temp);
            foreach (var (connection, command, id) in SystemAPI.Query<
                         RefRO<NetworkStreamConnection>, RefRO<CommandTarget>, RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>())
            {
                var player = command.ValueRO.targetEntity;
                if (connection.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !SystemAPI.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                    !SystemAPI.HasComponent<LocalTransform>(player))
                    continue;
                if (!SystemAPI.HasComponent<CombatPrototypePlayerHealth>(player))
                {
                    Debug.LogError($"[CombatPrototype.NetCode] Server enemy attack target collection failed; NetworkId={id.ValueRO.Value}, player={player}, reason=MissingPlayerHealth.");
                    continue;
                }
                if (SystemAPI.GetComponent<CombatPrototypePlayerHealth>(player).IsDead != 0)
                    continue;
                players.Add(new OnlinePlayer
                {
                    Entity = player,
                    NetworkId = id.ValueRO.Value,
                    Position = SystemAPI.GetComponent<LocalTransform>(player).Position
                });
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            var damageEvents = SystemAPI.GetBufferLookup<CombatPrototypePlayerDamageEvent>();
            foreach (var (attack, config, health, transform, target, enemy) in SystemAPI.Query<
                         RefRW<CombatPrototypeEnemyAttackState>, RefRO<CombatPrototypeEnemyAttackConfig>,
                         RefRO<CombatPrototypeEnemyState>, RefRO<LocalTransform>,
                         RefRO<CombatPrototypeEnemyTarget>>().WithEntityAccess())
            {
                if (health.ValueRO.IsDead != 0)
                {
                    attack.ValueRW.Phase = CombatPrototypeEnemyAttackPhase.Ready;
                    attack.ValueRW.PhaseTimer = 0f;
                    attack.ValueRW.SwingStarted = 0;
                    attack.ValueRW.TargetPlayer = Entity.Null;
                    attack.ValueRW.TargetNetworkId = 0;
                    continue;
                }

                if (attack.ValueRO.Phase == CombatPrototypeEnemyAttackPhase.Ready)
                {
                    var playerIndex = FindPlayer(players, target.ValueRO.Player, target.ValueRO.NetworkId);
                    if (playerIndex < 0 || math.distancesq(transform.ValueRO.Position.xz,
                            players[playerIndex].Position.xz) > config.ValueRO.Range * config.ValueRO.Range)
                        continue;

                    attack.ValueRW.TargetPlayer = target.ValueRO.Player;
                    attack.ValueRW.TargetNetworkId = target.ValueRO.NetworkId;
                    attack.ValueRW.AttackSequence++;
                    attack.ValueRW.SwingStarted = 0;
                    attack.ValueRW.Phase = CombatPrototypeEnemyAttackPhase.Startup;
                    attack.ValueRW.PhaseTimer = config.ValueRO.StartupSeconds;
                    Debug.Log($"[CombatPrototype.NetCode] Server enemy attack started; enemy={enemy}, NetworkId={attack.ValueRO.TargetNetworkId}, player={attack.ValueRO.TargetPlayer}, attack={attack.ValueRO.AttackSequence}, phase=Startup.");
                    continue;
                }

                attack.ValueRW.PhaseTimer -= deltaTime;
                if (attack.ValueRO.PhaseTimer > 0f)
                    continue;

                if (attack.ValueRO.Phase == CombatPrototypeEnemyAttackPhase.Startup)
                {
                    // Advance before queuing: a hit, miss or failed event cannot replay this swing.
                    attack.ValueRW.Phase = CombatPrototypeEnemyAttackPhase.Recovery;
                    attack.ValueRW.PhaseTimer = config.ValueRO.RecoverySeconds;
                    // A respawn-cancelled startup never reaches this actual swing marker.
                    attack.ValueRW.SwingStarted = 1;
                    var playerIndex = FindPlayer(players, attack.ValueRO.TargetPlayer, attack.ValueRO.TargetNetworkId);
                    if (playerIndex < 0)
                    {
                        Debug.Log($"[CombatPrototype.NetCode] Server enemy attack missed; enemy={enemy}, NetworkId={attack.ValueRO.TargetNetworkId}, player={attack.ValueRO.TargetPlayer}, attack={attack.ValueRO.AttackSequence}, reason=TargetOfflineOrDead, phase=Recovery.");
                        continue;
                    }
                    if (math.distancesq(transform.ValueRO.Position.xz, players[playerIndex].Position.xz) >
                        config.ValueRO.Range * config.ValueRO.Range)
                    {
                        Debug.Log($"[CombatPrototype.NetCode] Server enemy attack missed; enemy={enemy}, NetworkId={attack.ValueRO.TargetNetworkId}, player={attack.ValueRO.TargetPlayer}, attack={attack.ValueRO.AttackSequence}, reason=TargetOutOfRange, phase=Recovery.");
                        continue;
                    }
                    QueueDamage(enemy, attack.ValueRO, config.ValueRO.Damage, damageEvents);
                }
                else if (attack.ValueRO.Phase == CombatPrototypeEnemyAttackPhase.Recovery)
                {
                    attack.ValueRW.Phase = CombatPrototypeEnemyAttackPhase.Ready;
                    attack.ValueRW.TargetPlayer = Entity.Null;
                    attack.ValueRW.TargetNetworkId = 0;
                }
            }
        }

        private static int FindPlayer(NativeList<OnlinePlayer> players, Entity player, int networkId)
        {
            for (var index = 0; index < players.Length; index++)
            {
                if (players[index].Entity == player && players[index].NetworkId == networkId)
                    return index;
            }
            return -1;
        }

        private static void QueueDamage(Entity enemy, CombatPrototypeEnemyAttackState attack, float damage,
            BufferLookup<CombatPrototypePlayerDamageEvent> events)
        {
            if (!events.HasBuffer(attack.TargetPlayer))
            {
                Debug.LogError($"[CombatPrototype.NetCode] Server player damage event failed; enemy={enemy}, NetworkId={attack.TargetNetworkId}, player={attack.TargetPlayer}, attack={attack.AttackSequence}, reason=MissingPlayerDamageBuffer.");
                return;
            }
            try
            {
                events[attack.TargetPlayer].Add(new CombatPrototypePlayerDamageEvent
                {
                    AttackerEnemy = enemy,
                    AttackSequence = attack.AttackSequence,
                    Damage = damage
                });
                Debug.Log($"[CombatPrototype.NetCode] Server player damage queued; enemy={enemy}, NetworkId={attack.TargetNetworkId}, player={attack.TargetPlayer}, attack={attack.AttackSequence}, damage={damage}, phase=Recovery.");
            }
            catch (global::System.Exception exception)
            {
                Debug.LogError($"[CombatPrototype.NetCode] Server player damage event failed; enemy={enemy}, NetworkId={attack.TargetNetworkId}, player={attack.TargetPlayer}, attack={attack.AttackSequence}; {exception}");
            }
        }
    }
}
