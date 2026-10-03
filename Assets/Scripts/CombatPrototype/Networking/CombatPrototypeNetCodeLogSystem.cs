using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class CombatPrototypeNetCodeLogSystem : SystemBase
    {
        private double _nextSnapshot;
        private int _lastPlayerCount = -1;
        private int _lastAliveCount = -1;
        private int _lastDeadCount = -1;
        private EntityQuery _players;
        private bool _isServer;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _players = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypePlayerNetCode>());
            _isServer = World.IsServer();
        }

        protected override void OnUpdate()
        {
            var count = _players.CalculateEntityCount();
            if (count != _lastPlayerCount)
            {
                Debug.Log($"[CombatPrototype.NetCode][{World.Name}] Player ghost count {_lastPlayerCount} -> {count}.");
                _lastPlayerCount = count;
            }
            var alive = 0;
            var dead = 0;
            foreach (var health in SystemAPI.Query<RefRO<CombatPrototypeEnemyState>>())
            {
                if (health.ValueRO.IsDead == 0)
                    alive++;
                else
                    dead++;
            }
            if (alive != _lastAliveCount || dead != _lastDeadCount)
            {
                Debug.Log($"[CombatPrototype.NetCode][{World.Name}] Enemy count total={alive + dead}, alive={alive}, dead={dead}.");
                _lastAliveCount = alive;
                _lastDeadCount = dead;
            }
            if (SystemAPI.Time.ElapsedTime < _nextSnapshot)
                return;
            _nextSnapshot = SystemAPI.Time.ElapsedTime + 2d;

            var inventories = SystemAPI.GetBufferLookup<CombatPrototypeInventoryItem>(true);
            var identities = SystemAPI.GetComponentLookup<CombatPrototypePlayerIdentity>(true);
            foreach (var (owner, transform, attack, resource, reward, health, player) in SystemAPI.Query<RefRO<GhostOwner>, RefRO<LocalTransform>, RefRO<CombatPrototypeMeleeState>,
                         RefRO<CombatPrototypePlayerResource>, RefRO<CombatPrototypePlayerReward>, RefRO<CombatPrototypePlayerHealth>>()
                         .WithAll<CombatPrototypePlayerNetCode>().WithEntityAccess())
            {
                var inventory = inventories[player];
                if (_isServer)
                    Debug.Log($"[CombatPrototype.NetCode][{World.Name}] Player={owner.ValueRO.NetworkId}, persistentPlayerId={identities[player].PlayerId}.");
                Debug.Log($"[CombatPrototype.NetCode][{World.Name}] Player={owner.ValueRO.NetworkId}, position={transform.ValueRO.Position}, rotation={transform.ValueRO.Rotation.value}, phase={attack.ValueRO.Phase}, attack={attack.ValueRO.AttackSequence}, power={resource.ValueRO.CurrentPower}/{resource.ValueRO.UpperPower}, HP={health.ValueRO.CurrentHealth}/{health.ValueRO.MaxHealth}, hit={health.ValueRO.HitSequence}, dead={health.ValueRO.IsDead}, coin={reward.ValueRO.Coin}, experience={reward.ValueRO.Experience}, inventoryEntries={inventory.Length}.");
                for (var index = 0; index < inventory.Length; index++)
                {
                    var item = inventory[index];
                    Debug.Log($"[CombatPrototype.NetCode][{World.Name}] Player={owner.ValueRO.NetworkId}, inventoryItem={item.ItemName}, quantity={item.Quantity}.");
                }
            }
            // foreach (var (health, transform, ghost) in SystemAPI.Query<
            //              RefRO<CombatPrototypeEnemyState>, RefRO<LocalTransform>, RefRO<GhostInstance>>())
            // {
            //     Debug.Log($"[CombatPrototype.NetCode][{World.Name}] Enemy ghost={ghost.ValueRO.ghostId}, position={transform.ValueRO.Position}, HP={health.ValueRO.CurrentHealth}, hit={health.ValueRO.HitSequence}, dead={health.ValueRO.IsDead}.");
            //  }
            // foreach (var (target, ghost) in SystemAPI.Query<RefRO<CombatPrototypeEnemyTarget>, RefRO<GhostInstance>>())
            // {
            //     Debug.Log($"[CombatPrototype.NetCode][{World.Name}] Enemy ghost={ghost.ValueRO.ghostId}, target NetworkId={target.ValueRO.NetworkId}.");
            // }
        }
    }
}
