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
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, input, player) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<CombatPrototypePlayerInput>, RefRO<CombatPrototypePlayerNetCode>>()
                         .WithAll<Simulate>())
            {
                // Commands are external input: reject non-finite axes and cap the vector to unit length.
                var move = input.ValueRO.Move;
                if (!math.all(math.isfinite(move)))
                    continue;
                var lengthSquared = math.lengthsq(move);
                if (lengthSquared > 1f)
                    move *= math.rsqrt(lengthSquared);
                transform.ValueRW.Position += new float3(move.x, 0f, move.y) * player.ValueRO.MoveSpeed * deltaTime;
                if (lengthSquared > 0.0001f)
                    transform.ValueRW.Rotation = quaternion.RotateY(math.atan2(move.x, move.y));
            }
        }
    }
}