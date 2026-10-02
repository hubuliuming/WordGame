using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypePlayerDamageSystem))]
    public partial struct CombatPrototypePlayerRespawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var inputs = SystemAPI.GetComponentLookup<CombatPrototypePlayerInput>(true);
            var owners = SystemAPI.GetComponentLookup<GhostOwner>(true);
            var healths = SystemAPI.GetComponentLookup<CombatPrototypePlayerHealth>();
            var resources = SystemAPI.GetComponentLookup<CombatPrototypePlayerResource>();
            var meleeStates = SystemAPI.GetComponentLookup<CombatPrototypeMeleeState>();
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>();
            var damageEvents = SystemAPI.GetBufferLookup<CombatPrototypePlayerDamageEvent>();

            foreach (var (stream, command, id, connection) in SystemAPI.Query<
                         RefRO<NetworkStreamConnection>, RefRO<CommandTarget>, RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>()
                         .WithEntityAccess())
            {
                var player = command.ValueRO.targetEntity;
                // Connection targets are lifecycle data; only the current simulated player can respawn.
                if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !SystemAPI.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                    !SystemAPI.HasComponent<Simulate>(player) ||
                    !SystemAPI.IsComponentEnabled<Simulate>(player))
                    continue;

                var networkId = id.ValueRO.Value;
                var stage = "ReadInput";
                try
                {
                    if (!inputs[player].Respawn.IsSet)
                        continue;
                    stage = "ValidatePlayer";
                    if (owners[player].NetworkId != networkId)
                    {
                        Debug.LogWarning($"[CombatPrototype.NetCode] Server respawn rejected; NetworkId={networkId}, player={player}, reason=CommandTargetOwnerMismatch.");
                        continue;
                    }

                    var health = healths.GetRefRW(player);
                    if (health.ValueRO.IsDead == 0)
                    {
                        Debug.Log($"[CombatPrototype.NetCode] Server respawn rejected; NetworkId={networkId}, player={player}, reason=PlayerAlive.");
                        continue;
                    }

                    // Acquire every required player reference before changing any state.
                    var resource = resources.GetRefRW(player);
                    var melee = meleeStates.GetRefRW(player);
                    var transform = transforms.GetRefRW(player);
                    var events = damageEvents[player];
                    var clearedEnemyLocks = 0;
                    var cancelledEnemyAttacks = 0;

                    stage = "ClearEnemyLocks";
                    foreach (var (attack, config, target) in SystemAPI.Query<
                                 RefRW<CombatPrototypeEnemyAttackState>, RefRO<CombatPrototypeEnemyAttackConfig>,
                                 RefRW<CombatPrototypeEnemyTarget>>().WithAll<CombatPrototypeEnemyState>())
                    {
                        if (target.ValueRO.Player == player && target.ValueRO.NetworkId == networkId)
                            target.ValueRW = default;
                        if (attack.ValueRO.TargetPlayer != player || attack.ValueRO.TargetNetworkId != networkId)
                            continue;

                        if (attack.ValueRO.Phase == CombatPrototypeEnemyAttackPhase.Startup)
                        {
                            attack.ValueRW.Phase = CombatPrototypeEnemyAttackPhase.Recovery;
                            attack.ValueRW.PhaseTimer = config.ValueRO.RecoverySeconds;
                            cancelledEnemyAttacks++;
                        }
                        // An existing recovery keeps its remaining timer; no old swing can hit the new life.
                        attack.ValueRW.TargetPlayer = Entity.Null;
                        attack.ValueRW.TargetNetworkId = 0;
                        clearedEnemyLocks++;
                    }

                    stage = "ApplyRespawn";
                    events.Clear();
                    melee.ValueRW.Phase = CombatPrototypeAttackPhase.Ready;
                    melee.ValueRW.PhaseTimer = 0f;
                    resource.ValueRW.CurrentPower = resource.ValueRO.UpperPower;
                    transform.ValueRW.Position = new float3(networkId * 2f, 1f, 0f);
                    health.ValueRW.CurrentHealth = health.ValueRO.MaxHealth;
                    health.ValueRW.IsDead = 0;
                    // HitSequence and AttackSequence are intentionally retained on the same player entity.
                    Debug.Log($"[CombatPrototype.NetCode] Server respawn accepted; NetworkId={networkId}, player={player}, HP={health.ValueRO.CurrentHealth}/{health.ValueRO.MaxHealth}, power={resource.ValueRO.CurrentPower}/{resource.ValueRO.UpperPower}, hit={health.ValueRO.HitSequence}, attack={melee.ValueRO.AttackSequence}, position={transform.ValueRO.Position}, cancelledEnemyAttacks={cancelledEnemyAttacks}, clearedEnemyLocks={clearedEnemyLocks}.");
                }
                catch (global::System.Exception exception)
                {
                    Debug.LogError($"[CombatPrototype.NetCode] Server respawn failed; connection={connection}, NetworkId={networkId}, player={player}, stage={stage}; {exception}");
                }
            }
        }
    }
}
