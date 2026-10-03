using System;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypePlayerDamageSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapGatherSystem : SystemBase
    {
        private struct OnlinePlayer
        {
            public Entity Entity;
            public int NetworkId;
            public bool WasGathering;
        }

        private struct PlayerLookups
        {
            public ComponentLookup<CombatPrototypePlayerInput> Inputs;
            public ComponentLookup<GhostOwner> Owners;
            public ComponentLookup<CombatPrototypePlayerHealth> Healths;
            public ComponentLookup<CombatPrototypeMeleeState> Melees;
            public ComponentLookup<LocalTransform> Transforms;
            public ComponentLookup<CombatPrototypePlayerIdentity> Identities;
            public ComponentLookup<CombatPrototypePlayerReward> Rewards;
            public BufferLookup<CombatPrototypeInventoryItem> Inventories;
        }

        private EntityQuery _points;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _points = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapGatherState>(),
                ComponentType.ReadOnly<CombatPrototypeMapGatherProgress>(),
                ComponentType.ReadOnly<CombatPrototypeMapGatherConfig>(), ComponentType.ReadOnly<LocalTransform>());
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var map = SystemAPI.GetSingleton<CombatPrototypeMapData>();
            var time = SystemAPI.Time.ElapsedTime;
            using var points = _points.ToEntityArray(Allocator.Temp);
            var players = new NativeList<OnlinePlayer>(Allocator.Temp);
            try
            {
                foreach (var (stream, command, id) in SystemAPI.Query<
                             RefRO<NetworkStreamConnection>, RefRO<CommandTarget>, RefRO<NetworkId>>()
                             .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>())
                {
                    var player = command.ValueRO.targetEntity;
                    if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                        !SystemAPI.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                        !SystemAPI.HasComponent<Simulate>(player) || !SystemAPI.IsComponentEnabled<Simulate>(player))
                        continue;
                    // The small online roster is ordered independently of ECS chunk iteration.
                    var value = new OnlinePlayer { Entity = player, NetworkId = id.ValueRO.Value };
                    var index = players.Length;
                    players.Add(value);
                    while (index > 0 && players[index - 1].NetworkId > value.NetworkId)
                    {
                        players[index] = players[index - 1];
                        index--;
                    }
                    players[index] = value;
                }

                var access = new PlayerLookups
                {
                    Inputs = SystemAPI.GetComponentLookup<CombatPrototypePlayerInput>(true),
                    Owners = SystemAPI.GetComponentLookup<GhostOwner>(true),
                    Healths = SystemAPI.GetComponentLookup<CombatPrototypePlayerHealth>(true),
                    Melees = SystemAPI.GetComponentLookup<CombatPrototypeMeleeState>(true),
                    Transforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                    Identities = SystemAPI.GetComponentLookup<CombatPrototypePlayerIdentity>(true),
                    Rewards = SystemAPI.GetComponentLookup<CombatPrototypePlayerReward>(true),
                    Inventories = SystemAPI.GetBufferLookup<CombatPrototypeInventoryItem>()
                };
                var states = SystemAPI.GetComponentLookup<CombatPrototypeMapGatherState>();
                var progresses = SystemAPI.GetComponentLookup<CombatPrototypeMapGatherProgress>();
                var configs = SystemAPI.GetComponentLookup<CombatPrototypeMapGatherConfig>(true);

                foreach (var point in points)
                {
                    var current = states.GetRefRW(point);
                    if (current.ValueRO.Phase != CombatPrototypeMapGatherPhase.Collecting) continue;
                    var progress = progresses.GetRefRW(point);
                    var networkId = current.ValueRO.CollectorNetworkId;
                    var placement = current.ValueRO.PlacementIndex;
                    try
                    {
                        var playerIndex = FindCollector(players, progress.ValueRO.Collector, networkId);
                        if (playerIndex < 0)
                        {
                            Cancel(current, progress, map.MapDefinitionId, point, "CollectorOffline");
                            continue;
                        }
                        var player = players[playerIndex];
                        player.WasGathering = true;
                        players[playerIndex] = player;
                        var reason = RejectPlayer(player, access);
                        if (reason == null && access.Healths[player.Entity].HitSequence != progress.ValueRO.StartHitSequence)
                            reason = "PlayerHit";
                        var config = configs[point];
                        if (reason == null && math.distancesq(access.Transforms[player.Entity].Position.xz,
                                access.Transforms[point].Position.xz) > config.InteractionDistance * config.InteractionDistance)
                            reason = "OutOfRange";
                        if (reason != null)
                        {
                            Cancel(current, progress, map.MapDefinitionId, point, reason);
                            continue;
                        }
                        if (time >= progress.ValueRO.FinishAt)
                            Complete(current, progress, config, player, access, map.MapDefinitionId, point);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Gather update failed; map=" + map.MapDefinitionId +
                            ", placement=" + placement + ", NetworkId=" + networkId + ", point=" + point + ". " + exception);
                        if (current.ValueRO.Phase == CombatPrototypeMapGatherPhase.Collecting)
                            Cancel(current, progress, map.MapDefinitionId, point, "ProcessingFailed");
                    }
                }

                for (var i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    var stage = "ReadInput";
                    try
                    {
                        if (!access.Inputs[player.Entity].Gather.IsSet) continue;
                        stage = "ValidatePlayer";
                        var reason = player.WasGathering ? "AlreadyGathering" : RejectPlayer(player, access);
                        if (reason != null)
                        {
                            Reject(map.MapDefinitionId, player, reason);
                            continue;
                        }
                        var position = access.Transforms[player.Entity].Position.xz;
                        var target = Entity.Null;
                        var bestDistance = float.PositiveInfinity;
                        var bestPlacement = int.MaxValue;
                        foreach (var point in points)
                        {
                            var gather = states[point];
                            if (gather.Phase != CombatPrototypeMapGatherPhase.Available) continue;
                            var range = configs[point].InteractionDistance;
                            var distance = math.distancesq(position, access.Transforms[point].Position.xz);
                            if (distance > range * range || distance > bestDistance ||
                                (distance == bestDistance && gather.PlacementIndex >= bestPlacement)) continue;
                            target = point;
                            bestDistance = distance;
                            bestPlacement = gather.PlacementIndex;
                        }
                        if (target == Entity.Null)
                        {
                            Reject(map.MapDefinitionId, player, "NoAvailableTarget");
                            continue;
                        }
                        stage = "StartGather";
                        var state = states.GetRefRW(target);
                        var progress = progresses.GetRefRW(target);
                        var config = configs[target];
                        // Obtain both references before committing the temporary reservation.
                        progress.ValueRW = new CombatPrototypeMapGatherProgress
                        {
                            Collector = player.Entity, StartHitSequence = access.Healths[player.Entity].HitSequence,
                            FinishAt = time + config.GatherDuration
                        };
                        state.ValueRW.CollectorNetworkId = player.NetworkId;
                        state.ValueRW.Phase = CombatPrototypeMapGatherPhase.Collecting;
                        Debug.Log("[CombatPrototype.Map] Gather started; map=" + map.MapDefinitionId +
                            ", placement=" + state.ValueRO.PlacementIndex + ", objectId=" + config.ObjectId +
                            ", NetworkId=" + player.NetworkId + ", player=" + player.Entity +
                            ", duration=" + config.GatherDuration + ".");
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Gather request failed; map=" + map.MapDefinitionId +
                            ", NetworkId=" + player.NetworkId + ", player=" + player.Entity +
                            ", stage=" + stage + ". " + exception);
                    }
                }
            }
            finally { players.Dispose(); }
        }

        private static int FindCollector(NativeList<OnlinePlayer> players, Entity entity, int networkId)
        {
            for (var i = 0; i < players.Length; i++)
                if (players[i].Entity == entity && players[i].NetworkId == networkId) return i;
            return -1;
        }

        private static string RejectPlayer(OnlinePlayer player, PlayerLookups access)
        {
            if (access.Owners[player.Entity].NetworkId != player.NetworkId) return "CommandTargetOwnerMismatch";
            if (access.Healths[player.Entity].IsDead != 0) return "PlayerDead";
            var input = access.Inputs[player.Entity];
            if (input.Attack.IsSet || access.Melees[player.Entity].Phase != CombatPrototypeAttackPhase.Ready)
                return "AttackInProgress";
            if (!math.all(math.isfinite(input.Move))) return "InvalidMoveInput";
            if (math.lengthsq(input.Move) != 0f) return "PlayerMoving";
            return null;
        }

        private static void Reject(FixedString64Bytes mapId, OnlinePlayer player, string reason)
        {
            Debug.Log("[CombatPrototype.Map] Gather rejected; map=" + mapId + ", NetworkId=" +
                player.NetworkId + ", player=" + player.Entity + ", reason=" + reason + ".");
        }

        private static void Cancel(RefRW<CombatPrototypeMapGatherState> state,
            RefRW<CombatPrototypeMapGatherProgress> progress, FixedString64Bytes mapId, Entity point, string reason)
        {
            var placement = state.ValueRO.PlacementIndex;
            var networkId = state.ValueRO.CollectorNetworkId;
            progress.ValueRW = default;
            state.ValueRW.CollectorNetworkId = 0;
            state.ValueRW.Phase = CombatPrototypeMapGatherPhase.Available;
            Debug.Log("[CombatPrototype.Map] Gather cancelled; map=" + mapId + ", placement=" + placement +
                ", NetworkId=" + networkId + ", point=" + point + ", reason=" + reason + ".");
        }

        private static void Complete(RefRW<CombatPrototypeMapGatherState> state,
            RefRW<CombatPrototypeMapGatherProgress> progress, CombatPrototypeMapGatherConfig config,
            OnlinePlayer player, PlayerLookups access, FixedString64Bytes mapId, Entity point)
        {
            var stage = "PrepareReward";
            var placement = state.ValueRO.PlacementIndex;
            try
            {
                var playerId = access.Identities[player.Entity].PlayerId;
                var reward = access.Rewards[player.Entity];
                var inventory = access.Inventories[player.Entity];
                var itemIndex = -1;
                for (var i = 0; i < inventory.Length; i++)
                    if (inventory[i].ItemName.Equals(config.YieldItemName)) { itemIndex = i; break; }
                var next = new CombatPrototypeInventoryItem { ItemName = config.YieldItemName, Quantity = config.YieldQuantity };
                if (itemIndex >= 0) next.Quantity = checked(inventory[itemIndex].Quantity + config.YieldQuantity);
                else inventory.EnsureCapacity(checked(inventory.Length + 1));
                var candidate = CombatPrototypePlayerSaveStore.PrepareReward(playerId, reward, inventory, itemIndex, next);

                stage = "SavePrepared";
                CombatPrototypePlayerSaveStore.SavePrepared(candidate);

                // All references and buffer capacity are acquired before saving; no structural change or allocation here.
                stage = "CommitGather";
                if (itemIndex >= 0) inventory[itemIndex] = next;
                else inventory.Add(next);
                progress.ValueRW = default;
                state.ValueRW.CollectorNetworkId = 0;
                state.ValueRW.Phase = CombatPrototypeMapGatherPhase.Depleted;
                Debug.Log("[CombatPrototype.Map] Gather granted and saved; map=" + mapId + ", placement=" +
                    placement + ", objectId=" + config.ObjectId + ", NetworkId=" + player.NetworkId +
                    ", PlayerId=" + playerId + ", item=" + config.YieldItemName + ", quantity=+" +
                    config.YieldQuantity + ", totalItemQuantity=" + next.Quantity + ".");
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Gather settlement or save failed; map=" + mapId +
                    ", placement=" + placement + ", NetworkId=" + player.NetworkId + ", player=" + player.Entity +
                    ", item=" + config.YieldItemName + ", stage=" + stage + ". " + exception);
                if (state.ValueRO.Phase == CombatPrototypeMapGatherPhase.Collecting)
                    Cancel(state, progress, mapId, point, "SettlementFailed");
            }
        }
    }
}
