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
    [UpdateAfter(typeof(CombatPrototypeMapGatherSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapDropSpawnSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapTreeHarvestSystem : SystemBase
    {
        private struct OnlinePlayer
        {
            public Entity Entity;
            public int NetworkId;
        }

        private EntityQuery _trees;
        private Entity _source;
        private readonly Dictionary<int, int> _obstacles = new Dictionary<int, int>();

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypeMapTreeSettings>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            RequireForUpdate<NetworkTime>();
            _trees = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapTreeState>(),
                ComponentType.ReadOnly<CombatPrototypeMapTreeProgress>(), ComponentType.ReadOnly<LocalTransform>());
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapTreeSettings>(source);
            if (settings.Enabled == 0) return;
            var dropOwner = World.GetExistingSystemManaged<CombatPrototypeMapDropSpawnSystem>();
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            if (dropOwner == null || !tick.IsValid)
            {
                Debug.LogError("[CombatPrototype.Map] Tree harvest prerequisites failed; stage=ValidateServices, map=" +
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
                    if (objects[obstacles[i].ObjectIndex].Harvestable != 0)
                        _obstacles.Add(obstacles[i].PlacementIndex, i);
            }

            var time = SystemAPI.Time.ElapsedTime;
            using var trees = _trees.ToEntityArray(Allocator.Temp);
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

                foreach (var tree in trees)
                {
                    var current = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(tree);
                    if (current.Phase != CombatPrototypeMapTreePhase.Chopping) continue;
                    var progress = EntityManager.GetComponentData<CombatPrototypeMapTreeProgress>(tree);
                    var durabilitySaved = false;
                    try
                    {
                        var index = FindCollector(players, progress.Collector, current.CollectorNetworkId);
                        if (index < 0)
                        {
                            Cancel(tree, current, map.MapDefinitionId, "CollectorOffline", progress.Collector);
                            continue;
                        }
                        var player = players[index];
                        var reason = RejectPlayer(player, out _, out var health);
                        if (reason == null && health.HitSequence != progress.StartHitSequence) reason = "PlayerHit";
                        if (reason == null && math.distancesq(
                                EntityManager.GetComponentData<LocalTransform>(player.Entity).Position.xz,
                                EntityManager.GetComponentData<LocalTransform>(tree).Position.xz) >
                            settings.InteractionDistance * settings.InteractionDistance) reason = "OutOfRange";
                        if (reason != null)
                        {
                            Cancel(tree, current, map.MapDefinitionId, reason, progress.Collector);
                            continue;
                        }
                        if (time >= progress.FinishAt)
                        {
                            Complete(source, tree, current, player, map, settings, tick, dropOwner, out durabilitySaved);
                            CombatPrototypeMapGatherOutcomeFeedbackUtility.Write(EntityManager, map.MapDefinitionId,
                                player.Entity, player.NetworkId, CombatPrototypeMapInteractionKind.Tree, CombatPrototypeMapGatherOutcomeResult.Completed);
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Tree harvest update failed; stage=UpdateReservation, map=" +
                            map.MapDefinitionId + ", placement=" + current.PlacementIndex + ", NetworkId=" +
                            current.CollectorNetworkId + ", tree=" + tree + ", resource=" + settings.ResourceKey + ". " + exception);
                        if (!durabilitySaved)
                        {
                            var latest = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(tree);
                            if (latest.Phase == CombatPrototypeMapTreePhase.Chopping)
                                Cancel(tree, latest, map.MapDefinitionId, "ProcessingFailed", progress.Collector);
                        }
                    }
                }

            }
            finally { players.Dispose(); }
        }

        internal bool TryBegin(Entity tree, Entity player, int networkId, uint hitSequence,
            double time, FixedString64Bytes mapId, CombatPrototypeMapTreeSettings settings, Entity source)
        {
            var state = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(tree);
            if (state.Phase != CombatPrototypeMapTreePhase.Standing) return false;
            var toolKind = CombatPrototypeMapGatherToolUtility.SelectForWork(EntityManager, source, player,
                CombatPrototypeMapGatherToolKind.Axe, settings.HarvestDuration, out var duration);
            var progress = new CombatPrototypeMapTreeProgress
            {
                Collector = player, StartHitSequence = hitSequence, FinishAt = time + duration,
                ToolKind = toolKind, ActualDuration = duration
            };
            state.Phase = CombatPrototypeMapTreePhase.Chopping;
            state.CollectorNetworkId = networkId;
            EntityManager.SetComponentData(tree, progress);
            EntityManager.SetComponentData(tree, state);
            Debug.Log("[CombatPrototype.Map] Tree harvest started; map=" + mapId + ", placement=" +
                state.PlacementIndex + ", NetworkId=" + networkId + ", tool=" + toolKind + ", duration=" + duration + ".");
            return true;
        }

        internal void CancelBegin(Entity tree, Entity player, FixedString64Bytes mapId)
        {
            var progress = EntityManager.GetComponentData<CombatPrototypeMapTreeProgress>(tree);
            if (progress.Collector == player)
                Cancel(tree, EntityManager.GetComponentData<CombatPrototypeMapTreeState>(tree),
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

        private void Cancel(Entity tree, CombatPrototypeMapTreeState state, FixedString64Bytes mapId, string reason, Entity player)
        {
            var collector = state.CollectorNetworkId;
            state.Phase = CombatPrototypeMapTreePhase.Standing;
            state.CollectorNetworkId = 0;
            state.FelledTick = 0;
            EntityManager.SetComponentData(tree, default(CombatPrototypeMapTreeProgress));
            EntityManager.SetComponentData(tree, state);
            Debug.Log("[CombatPrototype.Map] Tree harvest cancelled; map=" + mapId + ", placement=" +
                state.PlacementIndex + ", NetworkId=" + collector + ", reason=" + reason + ".");
            CombatPrototypeMapGatherOutcomeFeedbackUtility.WriteCancellation(EntityManager, mapId, player,
                collector, CombatPrototypeMapInteractionKind.Tree, reason);
        }

        private void Complete(Entity source, Entity tree, CombatPrototypeMapTreeState state, OnlinePlayer player,
            CombatPrototypeMapData map, CombatPrototypeMapTreeSettings settings, NetworkTick tick,
            CombatPrototypeMapDropSpawnSystem dropOwner, out bool durabilitySaved)
        {
            durabilitySaved = false;
            var index = _obstacles[state.PlacementIndex];
            var oldObstacle = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source)[index];
            var position = EntityManager.GetComponentData<LocalTransform>(tree).Position;
            var toolKind = EntityManager.GetComponentData<CombatPrototypeMapTreeProgress>(tree).ToolKind;
            var drop = Entity.Null;
            var dropId = 0;
            var historyLength = EntityManager.GetBuffer<CombatPrototypeMapTreeBlockingEvent>(tree).Length;
            var stage = "PrepareDrop";
            try
            {
                drop = dropOwner.SpawnOwnedDrop(source, settings.DropPrefab, settings.DropResourceKey,
                    settings.DropItemId, settings.DropQuantity, position, out dropId);
                // Instantiate invalidated handles. Acquire every commit reference and capacity before saving.
                stage = "PrepareCommit";
                var history = EntityManager.GetBuffer<CombatPrototypeMapTreeBlockingEvent>(tree);
                history.EnsureCapacity(checked(historyLength + 1));
                var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                var stateAccess = SystemAPI.GetComponentLookup<CombatPrototypeMapTreeState>().GetRefRW(tree);
                var progressAccess = SystemAPI.GetComponentLookup<CombatPrototypeMapTreeProgress>().GetRefRW(tree);
                var tools = EntityManager.GetBuffer<CombatPrototypeMapGatherTool>(player.Entity);
                var nextProgress = new CombatPrototypeMapTreeProgress
                {
                    RegrowAt = settings.RegrowEnabled != 0 ? SystemAPI.Time.ElapsedTime + settings.RegrowSeconds : 0d
                };
                var obstacle = oldObstacle;
                obstacle.Disabled = 1;
                var nextState = state;
                nextState.Phase = CombatPrototypeMapTreePhase.Felled;
                nextState.CollectorNetworkId = 0;
                nextState.FelledTick = tick.SerializedData;
                var transition = new CombatPrototypeMapTreeBlockingEvent { TransitionTick = tick.SerializedData, Disabled = 1 };
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
                Debug.Log("[CombatPrototype.Map] Tree completed and drop spawned; map=" + map.MapDefinitionId +
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
                            Debug.LogError("[CombatPrototype.Map] Tree drop cleanup failed; stage=ReleaseCurrentDrop, map=" +
                                map.MapDefinitionId + ", placement=" + state.PlacementIndex + ", DropId=" + dropId + ". " + cleanup);
                        }
                    }
                    try
                    {
                        var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                        obstacles[index] = oldObstacle;
                        EntityManager.GetBuffer<CombatPrototypeMapTreeBlockingEvent>(tree).ResizeUninitialized(historyLength);
                        Cancel(tree, state, map.MapDefinitionId, "DropOrSaveOrCommitFailed", player.Entity);
                    }
                    catch (Exception rollback)
                    {
                        Debug.LogError("[CombatPrototype.Map] Tree rollback failed; stage=RollbackTree, map=" +
                            map.MapDefinitionId + ", placement=" + state.PlacementIndex + ", DropId=" + dropId +
                            ", resource=" + settings.DropResourceKey + ". " + rollback);
                    }
                }
                throw new InvalidOperationException("Tree completion failed; stage=" + stage + ", durabilitySaved=" + durabilitySaved +
                    ", placement=" + state.PlacementIndex + ", DropId=" + dropId + ", itemId=" + settings.DropItemId +
                    ", resource=" + settings.DropResourceKey + ".", exception);
            }
        }

    }
}
