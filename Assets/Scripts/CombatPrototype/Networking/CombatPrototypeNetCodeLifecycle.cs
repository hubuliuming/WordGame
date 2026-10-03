using Code_01.CombatPrototype.Map;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypeGoInGameRequest : IRpcCommand
    {
        public FixedString64Bytes PlayerId;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
    public partial struct CombatPrototypeGoInGameClientSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatPrototypePlayerSpawner>();
            state.RequireForUpdate<CombatPrototypeMapData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (stream, _, connection) in SystemAPI.Query<RefRO<NetworkStreamConnection>, RefRO<NetworkId>>()
                         .WithEntityAccess().WithNone<NetworkStreamInGame, NetworkStreamRequestDisconnect>())
            {
                if (stream.ValueRO.CurrentState != ConnectionState.State.Connected)
                    continue;
                try
                {
                    var playerId = CombatPrototypeDevelopmentIdentity.ReadPlayerId(state.WorldUnmanaged.Name.ToString());
                    ecb.AddComponent<NetworkStreamInGame>(connection);
                    var request = ecb.CreateEntity();
                    ecb.AddComponent(request, new CombatPrototypeGoInGameRequest { PlayerId = playerId });
                    ecb.AddComponent(request, new SendRpcCommandRequest { TargetConnection = connection });
                    Debug.Log($"[CombatPrototype.NetCode][{state.WorldUnmanaged.Name}] Client sent in-game request; PlayerId={playerId}.");
                }
                catch (global::System.Exception exception)
                {
                    ecb.AddComponent(connection, new NetworkStreamRequestDisconnect { Reason = NetworkStreamDisconnectReason.ConnectionClose });
                    Debug.LogError($"[CombatPrototype.NetCode][{state.WorldUnmanaged.Name}] Client identity configuration failed; connection={connection}; {exception}");
                }
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
            state.RequireForUpdate<CombatPrototypeMapData>();
            state.RequireForUpdate<CombatPrototypeGoInGameRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var spawner = SystemAPI.GetSingleton<CombatPrototypePlayerSpawner>();
            var map = SystemAPI.GetSingleton<CombatPrototypeMapData>();
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            using var handledThisUpdate = new NativeHashSet<Entity>(4, Allocator.Temp);
            using var occupiedPlayerIds = new NativeHashSet<FixedString64Bytes>(4, Allocator.Temp);
            foreach (var (stream, command) in SystemAPI.Query<RefRO<NetworkStreamConnection>, RefRO<CommandTarget>>()
                         .WithAll<NetworkStreamInGame, NetworkId>())
            {
                var player = command.ValueRO.targetEntity;
                if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !SystemAPI.HasComponent<CombatPrototypePlayerNetCode>(player))
                    continue;
                occupiedPlayerIds.Add(SystemAPI.GetComponent<CombatPrototypePlayerIdentity>(player).PlayerId);
            }
            foreach (var (received, admission, request) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>,
                         RefRO<CombatPrototypeGoInGameRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(request);
                var connection = received.ValueRO.SourceConnection;
                // RPCs are external input; stale or repeated requests must not create extra players.
                if (!state.EntityManager.HasComponent<NetworkId>(connection) ||
                    state.EntityManager.HasComponent<NetworkStreamInGame>(connection) ||
                    state.EntityManager.HasComponent<NetworkStreamRequestDisconnect>(connection) ||
                    !handledThisUpdate.Add(connection) ||
                    state.EntityManager.GetComponentData<NetworkStreamConnection>(connection).CurrentState != ConnectionState.State.Connected)
                    continue;

                var networkId = state.EntityManager.GetComponentData<NetworkId>(connection).Value;
                var createdPlayer = Entity.Null;
                var savePath = string.Empty;
                try
                {
                    var playerId = CombatPrototypePlayerIdentity.Parse(admission.ValueRO.PlayerId.ToString());
                    if (occupiedPlayerIds.Contains(playerId))
                    {
                        ecb.AddComponent(connection, new NetworkStreamRequestDisconnect { Reason = NetworkStreamDisconnectReason.ConnectionClose });
                        Debug.LogWarning($"[CombatPrototype.NetCode] Server admission rejected; NetworkId={networkId}, PlayerId={playerId}, reason=PlayerIdAlreadyOnline.");
                        continue;
                    }
                    savePath = CombatPrototypePlayerSaveStore.GetSavePath(playerId.ToString());
                    var data = CombatPrototypePlayerSaveStore.Load(playerId.ToString(), out var restored);
                    var restoredItems = new CombatPrototypeInventoryItem[data.Items.Length];
                    for (var index = 0; index < data.Items.Length; index++)
                        restoredItems[index] = new CombatPrototypeInventoryItem
                        {
                            ItemName = new FixedString64Bytes(data.Items[index].ItemName),
                            Quantity = data.Items[index].Quantity
                        };

                    createdPlayer = ecb.Instantiate(spawner.PlayerPrefab);
                    ecb.AddComponent(createdPlayer, new CombatPrototypePlayerIdentity { PlayerId = playerId });
                    ecb.SetComponent(createdPlayer, new CombatPrototypePlayerReward { Coin = data.Coin, Experience = data.Experience });
                    var inventory = ecb.SetBuffer<CombatPrototypeInventoryItem>(createdPlayer);
                    inventory.EnsureCapacity(restoredItems.Length);
                    foreach (var item in restoredItems)
                        inventory.Add(item);
                    ecb.SetComponent(createdPlayer, new GhostOwner { NetworkId = networkId });
                    ecb.SetComponent(createdPlayer, new AutoCommandTarget { Enabled = true });
                    ecb.SetComponent(createdPlayer, LocalTransform.FromPosition(CombatPrototypeMapSpawnUtility.PlayerPosition(map, networkId)));
                    ecb.SetComponent(connection, new CommandTarget { targetEntity = createdPlayer });
                    ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = createdPlayer });
                    ecb.AddComponent<NetworkStreamInGame>(connection);
                    occupiedPlayerIds.Add(playerId);
                    Debug.Log($"[CombatPrototype.NetCode] Server accepted NetworkId={networkId}, PlayerId={playerId}, restored={restored}, coin={data.Coin}, experience={data.Experience}, inventoryEntries={data.Items.Length}, path={savePath}; player ghost linked to connection lifetime.");
                }
                catch (global::System.Exception exception)
                {
                    if (createdPlayer != Entity.Null)
                        ecb.DestroyEntity(createdPlayer);
                    ecb.AddComponent(connection, new NetworkStreamRequestDisconnect { Reason = NetworkStreamDisconnectReason.ConnectionClose });
                    Debug.LogError($"[CombatPrototype.NetCode] Server admission failed; NetworkId={networkId}, PlayerId={admission.ValueRO.PlayerId}, path={savePath}; {exception}");
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
