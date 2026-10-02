using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypeGoInGameRequest : IRpcCommand { }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
    public partial struct CombatPrototypeGoInGameClientSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, connection) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess().WithNone<NetworkStreamInGame>())
            {
                ecb.AddComponent<NetworkStreamInGame>(connection);
                var request = ecb.CreateEntity();
                ecb.AddComponent<CombatPrototypeGoInGameRequest>(request);
                ecb.AddComponent(request, new SendRpcCommandRequest { TargetConnection = connection });
                Debug.Log("[CombatPrototype.NetCode] Client sent in-game request after scene data loaded.");
            }
            ecb.Playback(state.EntityManager);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
    public partial struct CombatPrototypeGoInGameServerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var spawner = SystemAPI.GetSingleton<CombatPrototypePlayerSpawner>();
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            using var acceptedThisUpdate = new NativeHashSet<Entity>(4, Allocator.Temp);
            foreach (var (received, request) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>()
                         .WithAll<CombatPrototypeGoInGameRequest>().WithEntityAccess())
            {
                ecb.DestroyEntity(request);
                var connection = received.ValueRO.SourceConnection;
                // RPCs are external input; stale or repeated requests must not create extra players.
                if (!state.EntityManager.HasComponent<NetworkId>(connection) ||
                    state.EntityManager.HasComponent<NetworkStreamInGame>(connection) ||
                    !acceptedThisUpdate.Add(connection))
                    continue;

                var networkId = state.EntityManager.GetComponentData<NetworkId>(connection).Value;
                var player = ecb.Instantiate(spawner.PlayerPrefab);
                ecb.SetComponent(player, new GhostOwner { NetworkId = networkId });
                ecb.SetComponent(player, new AutoCommandTarget { Enabled = true });
                ecb.SetComponent(player, LocalTransform.FromPosition(new float3(networkId * 2f, 1f, 0f)));
                ecb.SetComponent(connection, new CommandTarget { targetEntity = player });
                ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = player });
                ecb.AddComponent<NetworkStreamInGame>(connection);
                Debug.Log($"[CombatPrototype.NetCode] Server accepted NetworkId={networkId}; player ghost linked to connection lifetime.");
            }
            ecb.Playback(state.EntityManager);
        }
    }
}