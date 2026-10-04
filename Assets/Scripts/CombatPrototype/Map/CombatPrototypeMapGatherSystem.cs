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
    [UpdateAfter(typeof(CombatPrototypeMapInteractionSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapGatherSystem : SystemBase
    {
        private struct OnlinePlayer
        {
            public Entity Entity;
            public int NetworkId;
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
                            Complete(current, progress, config, player, access, map.MapDefinitionId, point, time);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Gather update failed; map=" + map.MapDefinitionId +
                            ", placement=" + placement + ", NetworkId=" + networkId + ", point=" + point + ". " + exception);
                        if (current.ValueRO.Phase == CombatPrototypeMapGatherPhase.Collecting)
                            Cancel(current, progress, map.MapDefinitionId, point, "ProcessingFailed");
                    }
                }

            }
            finally { players.Dispose(); }
        }

        internal bool TryBegin(Entity point, Entity player, int networkId, uint hitSequence,
            double time, FixedString64Bytes mapId)
        {
            var state = EntityManager.GetComponentData<CombatPrototypeMapGatherState>(point);
            if (state.Phase != CombatPrototypeMapGatherPhase.Available) return false;
            var progress = EntityManager.GetComponentData<CombatPrototypeMapGatherProgress>(point);
            var config = EntityManager.GetComponentData<CombatPrototypeMapGatherConfig>(point);
            progress = new CombatPrototypeMapGatherProgress
            {
                Collector = player, StartHitSequence = hitSequence, FinishAt = time + config.GatherDuration
            };
            state.CollectorNetworkId = networkId;
            state.Phase = CombatPrototypeMapGatherPhase.Collecting;
            EntityManager.SetComponentData(point, progress);
            EntityManager.SetComponentData(point, state);
            Debug.Log("[CombatPrototype.Map] Gather started; map=" + mapId + ", placement=" +
                state.PlacementIndex + ", objectId=" + config.ObjectId + ", NetworkId=" + networkId +
                ", player=" + player + ", duration=" + config.GatherDuration + ".");
            return true;
        }

        internal void CancelBegin(Entity point, Entity player, FixedString64Bytes mapId)
        {
            var progress = EntityManager.GetComponentData<CombatPrototypeMapGatherProgress>(point);
            if (progress.Collector != player) return;
            var state = EntityManager.GetComponentData<CombatPrototypeMapGatherState>(point);
            progress = default;
            state.CollectorNetworkId = 0;
            state.Phase = CombatPrototypeMapGatherPhase.Available;
            EntityManager.SetComponentData(point, progress);
            EntityManager.SetComponentData(point, state);
            Debug.Log("[CombatPrototype.Map] Gather reservation cancelled; map=" + mapId +
                ", placement=" + state.PlacementIndex + ", point=" + point + ", reason=ReservationFailed.");
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
            OnlinePlayer player, PlayerLookups access, FixedString64Bytes mapId, Entity point, double time)
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
                var regrowAt = config.RegrowEnabled != 0 ? time + config.RegrowSeconds : 0d;

                stage = "SavePrepared";
                CombatPrototypePlayerSaveStore.SavePrepared(candidate);

                // All references and buffer capacity are acquired before saving; no structural change or allocation here.
                stage = "CommitGather";
                if (itemIndex >= 0) inventory[itemIndex] = next;
                else inventory.Add(next);
                progress.ValueRW = new CombatPrototypeMapGatherProgress { RegrowAt = regrowAt };
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
