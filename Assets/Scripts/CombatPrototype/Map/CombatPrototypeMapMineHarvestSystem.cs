using System;
using System.Collections.Generic;
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
    [UpdateAfter(typeof(CombatPrototypeMapTreeHarvestSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapMineHarvestSystem : SystemBase
    {
        private struct OnlinePlayer
        {
            public Entity Entity;
            public int NetworkId;
        }

        private EntityQuery _mines;
        private Entity _source;
        private readonly Dictionary<int, int> _obstacles = new Dictionary<int, int>();

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypeMapMineSettings>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            RequireForUpdate<NetworkTime>();
            _mines = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapMineState>(),
                ComponentType.ReadOnly<CombatPrototypeMapMineProgress>(), ComponentType.ReadOnly<LocalTransform>());
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapMineSettings>(source);
            if (settings.Enabled == 0) return;
            var dropOwner = World.GetExistingSystemManaged<CombatPrototypeMapDropSpawnSystem>();
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            if (dropOwner == null || !tick.IsValid)
            {
                Debug.LogError("[CombatPrototype.Map] Mine harvest prerequisites failed; stage=ValidateServices, map=" +
                    map.MapDefinitionId + ", resource=" + settings.ResourceKey + ", dropOwner=" +
                    (dropOwner != null) + ", validTick=" + tick.IsValid + ".");
                return;
            }
            if (source != _source)
            {
                _source = source;
                _obstacles.Clear();
                var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source, true);
                var objects = EntityManager.GetBuffer<CombatPrototypeMapObject>(source, true);
                for (var i = 0; i < obstacles.Length; i++)
                    if (objects[obstacles[i].ObjectIndex].Mineable != 0)
                        _obstacles.Add(obstacles[i].PlacementIndex, i);
            }


            var time = SystemAPI.Time.ElapsedTime;
            using var mines = _mines.ToEntityArray(Allocator.Temp);
            var players = new NativeList<OnlinePlayer>(Allocator.Temp);
            try
            {
                foreach (var (stream, command, id) in SystemAPI.Query<RefRO<NetworkStreamConnection>,
                             RefRO<CommandTarget>, RefRO<NetworkId>>()
                             .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>())
                {
                    var player = command.ValueRO.targetEntity;
                    if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                        !EntityManager.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                        !EntityManager.HasComponent<Simulate>(player) || !EntityManager.IsComponentEnabled<Simulate>(player))
                        continue;
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

                foreach (var mine in mines)
                {
                    var current = EntityManager.GetComponentData<CombatPrototypeMapMineState>(mine);
                    if (current.Phase != CombatPrototypeMapMinePhase.Mining) continue;
                    var progress = EntityManager.GetComponentData<CombatPrototypeMapMineProgress>(mine);
                    var durabilitySaved = false;
                    try
                    {
                        var index = FindCollector(players, progress.Collector, current.CollectorNetworkId);
                        if (index < 0)
                        {
                            Cancel(mine, current, map.MapDefinitionId, "CollectorOffline", progress.Collector);
                            continue;
                        }
                        var player = players[index];
                        var reason = RejectPlayer(player, out _, out var health);
                        if (reason == null && health.HitSequence != progress.StartHitSequence) reason = "PlayerHit";
                        if (reason == null && math.distancesq(
                                EntityManager.GetComponentData<LocalTransform>(player.Entity).Position.xz,
                                EntityManager.GetComponentData<LocalTransform>(mine).Position.xz) >
                            settings.InteractionDistance * settings.InteractionDistance) reason = "OutOfRange";
                        if (reason != null)
                        {
                            Cancel(mine, current, map.MapDefinitionId, reason, progress.Collector);
                            continue;
                        }
                        if (time >= progress.FinishAt)
                        {
                            Complete(source, mine, current, player, map, settings, tick, dropOwner, out durabilitySaved);
                            CombatPrototypeMapGatherOutcomeFeedbackUtility.Write(EntityManager, map.MapDefinitionId,
                                player.Entity, player.NetworkId, CombatPrototypeMapInteractionKind.Mine, CombatPrototypeMapGatherOutcomeResult.Completed);
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Mine harvest update failed; stage=UpdateReservation, map=" +
                            map.MapDefinitionId + ", placement=" + current.PlacementIndex + ", NetworkId=" +
                            current.CollectorNetworkId + ", mine=" + mine + ", resource=" + settings.ResourceKey + ". " + exception);
                        try
                        {
                        if (!durabilitySaved)
                        {
                            var latest = EntityManager.GetComponentData<CombatPrototypeMapMineState>(mine);
                            if (latest.Phase == CombatPrototypeMapMinePhase.Mining)
                                Cancel(mine, latest, map.MapDefinitionId, "ProcessingFailed", progress.Collector);
                        }
                        }
                        catch (Exception cleanup)
                        {
                            Debug.LogError("[CombatPrototype.Map] Mine reservation cleanup failed; stage=CancelReservation, map=" +
                                map.MapDefinitionId + ", placement=" + current.PlacementIndex + ", mine=" + mine + ". " + cleanup);
                        }
                    }
                }

            }
            finally { players.Dispose(); }
        }

        internal bool TryBegin(Entity mine, Entity player, int networkId, uint hitSequence,
            double time, FixedString64Bytes mapId, CombatPrototypeMapMineSettings settings, Entity source)
        {
            var state = EntityManager.GetComponentData<CombatPrototypeMapMineState>(mine);
            if (state.Phase != CombatPrototypeMapMinePhase.Available) return false;
            var toolKind = CombatPrototypeMapGatherToolUtility.SelectForWork(EntityManager, source, player,
                CombatPrototypeMapGatherToolKind.Pickaxe, settings.HarvestDuration, out var duration);
            var progress = new CombatPrototypeMapMineProgress
            {
                Collector = player, StartHitSequence = hitSequence, FinishAt = time + duration,
                ToolKind = toolKind, ActualDuration = duration
            };
            state.Phase = CombatPrototypeMapMinePhase.Mining;
            state.CollectorNetworkId = networkId;
            EntityManager.SetComponentData(mine, progress);
            EntityManager.SetComponentData(mine, state);
            Debug.Log("[CombatPrototype.Map] Mine harvest started; map=" + mapId + ", placement=" +
                state.PlacementIndex + ", NetworkId=" + networkId + ", tool=" + toolKind + ", duration=" + duration + ".");
            return true;
        }

        internal void CancelBegin(Entity mine, Entity player, FixedString64Bytes mapId)
        {
            var progress = EntityManager.GetComponentData<CombatPrototypeMapMineProgress>(mine);
            if (progress.Collector == player)
                Cancel(mine, EntityManager.GetComponentData<CombatPrototypeMapMineState>(mine),
                    mapId, "ReservationFailed", player);
        }

        private string RejectPlayer(OnlinePlayer player, out CombatPrototypePlayerInput input,
            out CombatPrototypePlayerHealth health)
        {
            input = EntityManager.GetComponentData<CombatPrototypePlayerInput>(player.Entity);
            health = EntityManager.GetComponentData<CombatPrototypePlayerHealth>(player.Entity);
            if (EntityManager.GetComponentData<GhostOwner>(player.Entity).NetworkId != player.NetworkId) return "CommandTargetOwnerMismatch";
            if (health.IsDead != 0) return "PlayerDead";
            if (input.Attack.IsSet || EntityManager.GetComponentData<CombatPrototypeMeleeState>(player.Entity).Phase !=
                CombatPrototypeAttackPhase.Ready) return "AttackInProgress";
            if (!math.all(math.isfinite(input.Move))) return "InvalidMoveInput";
            if (math.lengthsq(input.Move) != 0f) return "PlayerMoving";
            return null;
        }

        private static int FindCollector(NativeList<OnlinePlayer> players, Entity entity, int networkId)
        {
            for (var i = 0; i < players.Length; i++)
                if (players[i].Entity == entity && players[i].NetworkId == networkId) return i;
            return -1;
        }

        private void Cancel(Entity mine, CombatPrototypeMapMineState state, FixedString64Bytes mapId, string reason, Entity player)
        {
            var collector = state.CollectorNetworkId;
            state.Phase = CombatPrototypeMapMinePhase.Available;
            state.CollectorNetworkId = 0;
            state.MinedTick = 0;
            EntityManager.SetComponentData(mine, default(CombatPrototypeMapMineProgress));
            EntityManager.SetComponentData(mine, state);
            Debug.Log("[CombatPrototype.Map] Mine harvest cancelled; map=" + mapId + ", placement=" +
                state.PlacementIndex + ", NetworkId=" + collector + ", reason=" + reason + ".");
            CombatPrototypeMapGatherOutcomeFeedbackUtility.WriteCancellation(EntityManager, mapId, player,
                collector, CombatPrototypeMapInteractionKind.Mine, reason);
        }

        private void Complete(Entity source, Entity mine, CombatPrototypeMapMineState state, OnlinePlayer player,
            CombatPrototypeMapData map, CombatPrototypeMapMineSettings settings, NetworkTick tick,
            CombatPrototypeMapDropSpawnSystem dropOwner, out bool durabilitySaved)
        {
            durabilitySaved = false;
            var index = _obstacles[state.PlacementIndex];
            var oldObstacle = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source)[index];
            var position = EntityManager.GetComponentData<LocalTransform>(mine).Position;
            var toolKind = EntityManager.GetComponentData<CombatPrototypeMapMineProgress>(mine).ToolKind;
            var drop = Entity.Null;
            var dropId = 0;
            var historyLength = EntityManager.GetBuffer<CombatPrototypeMapMineBlockingEvent>(mine).Length;
            var stage = "PrepareDrop";
            try
            {
                drop = dropOwner.SpawnOwnedDrop(source, settings.DropPrefab, settings.DropResourceKey,
                    settings.DropItemId, settings.DropQuantity, position, out dropId);
                // Instantiate invalidated handles. Acquire every commit reference and capacity before saving.
                stage = "PrepareCommit";
                var history = EntityManager.GetBuffer<CombatPrototypeMapMineBlockingEvent>(mine);
                history.EnsureCapacity(checked(historyLength + 1));
                var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                var stateAccess = SystemAPI.GetComponentLookup<CombatPrototypeMapMineState>().GetRefRW(mine);
                var progressAccess = SystemAPI.GetComponentLookup<CombatPrototypeMapMineProgress>().GetRefRW(mine);
                var tools = EntityManager.GetBuffer<CombatPrototypeMapGatherTool>(player.Entity);
                var nextProgress = new CombatPrototypeMapMineProgress
                {
                    RegrowAt = settings.RegrowEnabled != 0 ? SystemAPI.Time.ElapsedTime + settings.RegrowSeconds : 0d
                };
                var obstacle = oldObstacle;
                obstacle.Disabled = 1;
                var nextState = state;
                nextState.Phase = CombatPrototypeMapMinePhase.Depleted;
                nextState.CollectorNetworkId = 0;
                nextState.MinedTick = tick.SerializedData;
                var transition = new CombatPrototypeMapMineBlockingEvent { TransitionTick = tick.SerializedData, Disabled = 1 };
                var toolIndex = -1;
                var nextTool = default(CombatPrototypeMapGatherTool);
                if (toolKind != CombatPrototypeMapGatherToolKind.None)
                {
                    var definitions = EntityManager.GetBuffer<CombatPrototypeMapGatherToolDefinition>(source, true);
                    nextTool = CombatPrototypeMapGatherToolUtility.PrepareConsumption(definitions, tools, toolKind, out toolIndex);
                    var identity = EntityManager.GetComponentData<CombatPrototypePlayerIdentity>(player.Entity).PlayerId;
                    var reward = EntityManager.GetComponentData<CombatPrototypePlayerReward>(player.Entity);
                    var inventory = EntityManager.GetBuffer<CombatPrototypeInventoryItem>(player.Entity, true);
                    var candidate = CombatPrototypePlayerSaveStore.PrepareToolUse(identity, reward, inventory, tools,
                        EntityManager.GetComponentData<CombatPrototypeMapInventoryCapacityLevel>(player.Entity).Level, toolIndex, nextTool);
                    stage = "SaveDurability";
                    CombatPrototypePlayerSaveStore.SavePrepared(candidate);
                    durabilitySaved = true;
                }
                stage = "CommitCompletion";
                // No structural changes, allocation or reference acquisition after successful SavePrepared.
                if (toolIndex >= 0) tools[toolIndex] = nextTool;
                progressAccess.ValueRW = nextProgress;
                history.Add(transition);
                obstacles[index] = obstacle;
                stateAccess.ValueRW = nextState;
                Debug.Log("[CombatPrototype.Map] Mine completed and drop spawned; map=" + map.MapDefinitionId +
                    ", placement=" + state.PlacementIndex + ", NetworkId=" + player.NetworkId + ", DropId=" + dropId +
                    ", itemId=" + settings.DropItemId + ", quantity=" + settings.DropQuantity +
                    ", tool=" + toolKind + ", durability=" + nextTool.Durability + ", resource=" + settings.DropResourceKey + ".");
            }
            catch (Exception exception)
            {
                // A persisted cost is final. Unexpected faults beyond this boundary cannot be compensated with an old save.
                if (!durabilitySaved)
                {
                    if (drop != Entity.Null)
                    {
                        try { dropOwner.ReleaseDrop(drop); }
                        catch (Exception cleanup)
                        {
                            Debug.LogError("[CombatPrototype.Map] Mine drop cleanup failed; stage=ReleaseCurrentDrop, map=" +
                                map.MapDefinitionId + ", placement=" + state.PlacementIndex + ", DropId=" + dropId + ". " + cleanup);
                        }
                    }
                    try
                    {
                        var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                        obstacles[index] = oldObstacle;
                        EntityManager.GetBuffer<CombatPrototypeMapMineBlockingEvent>(mine).ResizeUninitialized(historyLength);
                        Cancel(mine, state, map.MapDefinitionId, "DropOrSaveOrCommitFailed", player.Entity);
                    }
                    catch (Exception rollback)
                    {
                        Debug.LogError("[CombatPrototype.Map] Mine rollback failed; stage=RollbackMine, map=" +
                            map.MapDefinitionId + ", placement=" + state.PlacementIndex + ", DropId=" + dropId +
                            ", resource=" + settings.DropResourceKey + ". " + rollback);
                    }
                }
                throw new InvalidOperationException("Mine completion failed; stage=" + stage + ", durabilitySaved=" + durabilitySaved +
                    ", placement=" + state.PlacementIndex + ", DropId=" + dropId + ", itemId=" + settings.DropItemId +
                    ", resource=" + settings.DropResourceKey + ".", exception);
            }
        }

    }
}
