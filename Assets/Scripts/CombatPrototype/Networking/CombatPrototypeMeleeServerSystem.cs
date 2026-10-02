using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeEnemySpatialSystem))]
    public partial class CombatPrototypeMeleeServerSystem : SystemBase
    {
        private CombatPrototypeEnemySpatialSystem _spatial;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        protected override void OnStartRunning()
        {
            _spatial = World.GetExistingSystemManaged<CombatPrototypeEnemySpatialSystem>();
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var damageEvents = SystemAPI.GetBufferLookup<CombatPrototypeDamageEvent>();
            foreach (var (attack, config, input, transform, owner, resource) in SystemAPI.Query<
                         RefRW<CombatPrototypeMeleeState>, RefRO<CombatPrototypeMeleeConfig>,
                         RefRO<CombatPrototypePlayerInput>, RefRO<LocalTransform>, RefRO<GhostOwner>,
                         RefRW<CombatPrototypePlayerResource>>()
                         .WithAll<CombatPrototypePlayerNetCode, Simulate>())
            {
                if (attack.ValueRO.Phase == CombatPrototypeAttackPhase.Ready)
                {
                    if (input.ValueRO.Attack.IsSet)
                    {
                        if (resource.ValueRO.CurrentPower < config.ValueRO.AttackPowerCost)
                        {
                            Debug.Log($"[CombatPrototype.NetCode] Server attack rejected; NetworkId={owner.ValueRO.NetworkId}, reason=InsufficientPower, power={resource.ValueRO.CurrentPower}/{resource.ValueRO.UpperPower}, cost={config.ValueRO.AttackPowerCost}, sequence={attack.ValueRO.AttackSequence}, phase=Ready.");
                            continue;
                        }

                        resource.ValueRW.CurrentPower -= config.ValueRO.AttackPowerCost;
                        attack.ValueRW.Phase = CombatPrototypeAttackPhase.Startup;
                        attack.ValueRW.PhaseTimer = config.ValueRO.StartupSeconds;
                        attack.ValueRW.AttackSequence++;
                        Debug.Log($"[CombatPrototype.NetCode] Server attack accepted; NetworkId={owner.ValueRO.NetworkId}, reason=ReadyAndPowerAvailable, power={resource.ValueRO.CurrentPower}/{resource.ValueRO.UpperPower}, cost={config.ValueRO.AttackPowerCost}, sequence={attack.ValueRO.AttackSequence}, phase=Startup.");
                    }
                    continue;
                }

                if (input.ValueRO.Attack.IsSet)
                    Debug.Log($"[CombatPrototype.NetCode] Server attack rejected; NetworkId={owner.ValueRO.NetworkId}, reason=AttackInProgress, power={resource.ValueRO.CurrentPower}/{resource.ValueRO.UpperPower}, sequence={attack.ValueRO.AttackSequence}, phase={attack.ValueRO.Phase}.");

                attack.ValueRW.PhaseTimer -= deltaTime;
                if (attack.ValueRO.PhaseTimer > 0f)
                    continue;

                switch (attack.ValueRO.Phase)
                {
                    case CombatPrototypeAttackPhase.Startup:
                        attack.ValueRW.Phase = CombatPrototypeAttackPhase.Active;
                        attack.ValueRW.PhaseTimer = config.ValueRO.ActiveSeconds;
                        var hits = QueueHits(transform.ValueRO, config.ValueRO, owner.ValueRO.NetworkId,
                            attack.ValueRO.AttackSequence, damageEvents);
                        Debug.Log($"[CombatPrototype.NetCode] Server attack queried; NetworkId={owner.ValueRO.NetworkId}, sequence={attack.ValueRO.AttackSequence}, targets={hits}.");
                        break;
                    case CombatPrototypeAttackPhase.Active:
                        attack.ValueRW.Phase = CombatPrototypeAttackPhase.Recovery;
                        attack.ValueRW.PhaseTimer = config.ValueRO.RecoverySeconds;
                        break;
                    case CombatPrototypeAttackPhase.Recovery:
                        attack.ValueRW.Phase = CombatPrototypeAttackPhase.Ready;
                        break;
                }
            }
        }

        private int QueueHits(LocalTransform transform, CombatPrototypeMeleeConfig config, int networkId,
            uint attackSequence, BufferLookup<CombatPrototypeDamageEvent> damageEvents)
        {
            var extent = new float3(config.Range, 0f, config.Range);
            var minimum = CombatPrototypeEnemySpatialSystem.GetCell(transform.Position - extent);
            var maximum = CombatPrototypeEnemySpatialSystem.GetCell(transform.Position + extent);
            var cells = _spatial.Cells;
            var rangeSquared = config.Range * config.Range;
            var angleCosine = math.cos(math.radians(config.Angle * 0.5f));
            var forward = math.forward(transform.Rotation);
            var hits = 0;

            // Startup ends once per attack. Each cell is visited once and each enemy occupies one cell.
            for (var x = minimum.x; x <= maximum.x; x++)
            {
                for (var z = minimum.y; z <= maximum.y; z++)
                {
                    if (!cells.TryGetFirstValue(new int2(x, z), out var enemy, out var iterator))
                        continue;
                    do
                    {
                        var direction = enemy.Position - transform.Position;
                        direction.y = 0f;
                        var distanceSquared = math.lengthsq(direction);
                        if (distanceSquared > rangeSquared ||
                            (distanceSquared >= 0.0001f &&
                             math.dot(forward, math.normalizesafe(direction)) < angleCosine))
                            continue;
                        damageEvents[enemy.Entity].Add(new CombatPrototypeDamageEvent
                        {
                            AttackerNetworkId = networkId,
                            AttackSequence = attackSequence,
                            Damage = config.Damage
                        });
                        hits++;
                    } while (cells.TryGetNextValue(out enemy, ref iterator));
                }
            }
            return hits;
        }
    }
}