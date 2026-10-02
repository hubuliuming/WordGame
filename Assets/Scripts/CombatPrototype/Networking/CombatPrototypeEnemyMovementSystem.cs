using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Code_01.CombatPrototype.Networking
{
    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeEnemyMovement : IComponentData
    {
        public float MoveSpeed;
        public float StopDistance;
    }

    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct CombatPrototypeEnemyTarget : IComponentData
    {
        public Entity Player;
        public int NetworkId;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypePlayerMovementSystem))]
    public partial struct CombatPrototypeEnemyMovementSystem : ISystem
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
                // Connections and their command target are network lifecycle data.
                var player = command.ValueRO.targetEntity;
                if (connection.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !SystemAPI.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                    !SystemAPI.HasComponent<LocalTransform>(player))
                    continue;
                players.Add(new OnlinePlayer
                {
                    Entity = player,
                    NetworkId = id.ValueRO.Value,
                    Position = SystemAPI.GetComponent<LocalTransform>(player).Position
                });
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, health, movement, target) in SystemAPI.Query<
                         RefRW<LocalTransform>, RefRO<CombatPrototypeEnemyState>,
                         RefRO<CombatPrototypeEnemyMovement>, RefRW<CombatPrototypeEnemyTarget>>())
            {
                target.ValueRW = default;
                if (health.ValueRO.IsDead != 0)
                    continue;

                var nearestIndex = -1;
                var nearestDistanceSquared = float.MaxValue;
                for (var index = 0; index < players.Length; index++)
                {
                    var distanceSquared = math.distancesq(transform.ValueRO.Position.xz, players[index].Position.xz);
                    if (distanceSquared < nearestDistanceSquared ||
                        (distanceSquared == nearestDistanceSquared &&
                         (nearestIndex < 0 || players[index].NetworkId < players[nearestIndex].NetworkId)))
                    {
                        nearestIndex = index;
                        nearestDistanceSquared = distanceSquared;
                    }
                }
                if (nearestIndex < 0)
                    continue;

                var nearest = players[nearestIndex];
                target.ValueRW = new CombatPrototypeEnemyTarget { Player = nearest.Entity, NetworkId = nearest.NetworkId };
                var direction = nearest.Position - transform.ValueRO.Position;
                direction.y = 0f;
                if (nearestDistanceSquared < 0.0001f)
                    continue;

                transform.ValueRW.Rotation = quaternion.RotateY(math.atan2(direction.x, direction.z));
                var distance = math.sqrt(nearestDistanceSquared);
                if (distance <= movement.ValueRO.StopDistance)
                    continue;
                var step = math.min(movement.ValueRO.MoveSpeed * deltaTime, distance - movement.ValueRO.StopDistance);
                transform.ValueRW.Position += direction * (step / distance);
            }
        }
    }
}