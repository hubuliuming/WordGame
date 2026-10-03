using Code_01.CombatPrototype.Map;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct CombatPrototypePlayerMovementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatPrototypeMapData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var map = SystemAPI.GetSingleton<CombatPrototypeMapData>();
            var obstacles = SystemAPI.GetSingletonBuffer<CombatPrototypeMapObstacle>(true).AsNativeArray();
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, input, player, health) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<CombatPrototypePlayerInput>,
                         RefRO<CombatPrototypePlayerNetCode>, RefRO<CombatPrototypePlayerHealth>>()
                         .WithAll<Simulate>())
            {
                if (health.ValueRO.IsDead != 0)
                    continue;

                // Commands are external input: reject non-finite axes and cap the vector to unit length.
                var move = input.ValueRO.Move;
                if (!math.all(math.isfinite(move)))
                    continue;
                var lengthSquared = math.lengthsq(move);
                if (lengthSquared > 1f)
                    move *= math.rsqrt(lengthSquared);
                var position = transform.ValueRO.Position;
                var resolved = CombatPrototypeMapMovementUtility.Move(position.xz, move * player.ValueRO.MoveSpeed * deltaTime,
                    map.PlayerRadius, in map, obstacles);
                transform.ValueRW.Position = new float3(resolved.x, position.y, resolved.y);
                if (lengthSquared > 0.0001f)
                    transform.ValueRW.Rotation = quaternion.RotateY(math.atan2(move.x, move.y));
            }
        }
    }
}
