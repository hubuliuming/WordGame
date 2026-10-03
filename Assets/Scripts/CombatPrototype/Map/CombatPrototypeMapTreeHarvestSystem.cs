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
            public bool WasChopping;
        }

        private EntityQuery _trees;
        private Entity _source;
        private readonly Dictionary<int, int> _obstacles = new Dictionary<int, int>();
        private readonly HashSet<Entity> _gatherCollectors = new HashSet<Entity>();

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
            _gatherCollectors.Clear();
            foreach (var (gather, progress) in SystemAPI.Query<RefRO<CombatPrototypeMapGatherState>,
                         RefRO<CombatPrototypeMapGatherProgress>>())
                if (gather.ValueRO.Phase == CombatPrototypeMapGatherPhase.Collecting)
                    _gatherCollectors.Add(progress.ValueRO.Collector);

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
                    try
                    {
                        var index = FindCollector(players, progress.Collector, current.CollectorNetworkId);
                        if (index < 0)
                        {
                            Cancel(tree, current, map.MapDefinitionId, "CollectorOffline");
                            continue;
                        }
                        var player = players[index];
                        player.WasChopping = true;
                        players[index] = player;
                        var reason = RejectPlayer(player, out _, out var health);
                        if (reason == null && health.HitSequence != progress.StartHitSequence) reason = "PlayerHit";
                        if (reason == null && math.distancesq(
                                EntityManager.GetComponentData<LocalTransform>(player.Entity).Position.xz,
                                EntityManager.GetComponentData<LocalTransform>(tree).Position.xz) >
                            settings.InteractionDistance * settings.InteractionDistance) reason = "OutOfRange";
                        if (reason != null)
                        {
                            Cancel(tree, current, map.MapDefinitionId, reason);
                            continue;
                        }
                        if (time >= progress.FinishAt)
                            Complete(source, tree, current, player, map, settings, tick, dropOwner);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Tree harvest update failed; stage=UpdateReservation, map=" +
                            map.MapDefinitionId + ", placement=" + current.PlacementIndex + ", NetworkId=" +
                            current.CollectorNetworkId + ", tree=" + tree + ", resource=" + settings.ResourceKey + ". " + exception);
                        var latest = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(tree);
                        if (latest.Phase == CombatPrototypeMapTreePhase.Chopping)
                            Cancel(tree, latest, map.MapDefinitionId, "ProcessingFailed");
                    }
                }

                for (var i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    var stage = "ReadInput";
                    try
                    {
                        if (!EntityManager.GetComponentData<CombatPrototypePlayerInput>(player.Entity).HarvestTree.IsSet) continue;
                        stage = "ValidatePlayer";
                        var reason = player.WasChopping ? "AlreadyChopping" : RejectPlayer(player, out _, out _);
                        if (reason != null)
                        {
                            Reject(map.MapDefinitionId, player, reason);
                            continue;
                        }
                        stage = "SelectTree";
                        var target = Entity.Null;
                        var bestDistance = float.PositiveInfinity;
                        var bestPlacement = int.MaxValue;
                        var position = EntityManager.GetComponentData<LocalTransform>(player.Entity).Position.xz;
                        foreach (var tree in trees)
                        {
                            var value = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(tree);
                            if (value.Phase != CombatPrototypeMapTreePhase.Standing) continue;
                            var distance = math.distancesq(position,
                                EntityManager.GetComponentData<LocalTransform>(tree).Position.xz);
                            if (distance > settings.InteractionDistance * settings.InteractionDistance || distance > bestDistance ||
                                (distance == bestDistance && value.PlacementIndex >= bestPlacement)) continue;
                            target = tree;
                            bestDistance = distance;
                            bestPlacement = value.PlacementIndex;
                        }
                        if (target == Entity.Null)
                        {
                            Reject(map.MapDefinitionId, player, "NoStandingTarget");
                            continue;
                        }
                        stage = "ReserveTree";
                        var state = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(target);
                        var health = EntityManager.GetComponentData<CombatPrototypePlayerHealth>(player.Entity);
                        EntityManager.SetComponentData(target, new CombatPrototypeMapTreeProgress
                        {
                            Collector = player.Entity, StartHitSequence = health.HitSequence,
                            FinishAt = time + settings.HarvestDuration
                        });
                        state.Phase = CombatPrototypeMapTreePhase.Chopping;
                        state.CollectorNetworkId = player.NetworkId;
                        EntityManager.SetComponentData(target, state);
                        Debug.Log("[CombatPrototype.Map] Tree harvest started; map=" + map.MapDefinitionId +
                            ", placement=" + state.PlacementIndex + ", NetworkId=" + player.NetworkId +
                            ", duration=" + settings.HarvestDuration + ".");
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Tree request failed; stage=" + stage + ", map=" +
                            map.MapDefinitionId + ", NetworkId=" + player.NetworkId + ", player=" + player.Entity +
                            ", resource=" + settings.ResourceKey + ". " + exception);
                    }
                }
            }
            finally { players.Dispose(); }
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
            if (input.Gather.IsSet || _gatherCollectors.Contains(player.Entity)) return "GatherPriority";
            return null;
        }

        private static int FindCollector(NativeList<OnlinePlayer> players, Entity entity, int networkId)
        {
            for (var i = 0; i < players.Length; i++)
                if (players[i].Entity == entity && players[i].NetworkId == networkId) return i;
            return -1;
        }

        private void Cancel(Entity tree, CombatPrototypeMapTreeState state, FixedString64Bytes mapId, string reason)
        {
            var collector = state.CollectorNetworkId;
            state.Phase = CombatPrototypeMapTreePhase.Standing;
            state.CollectorNetworkId = 0;
            state.FelledTick = 0;
            EntityManager.SetComponentData(tree, default(CombatPrototypeMapTreeProgress));
            EntityManager.SetComponentData(tree, state);
            Debug.Log("[CombatPrototype.Map] Tree harvest cancelled; map=" + mapId + ", placement=" +
                state.PlacementIndex + ", NetworkId=" + collector + ", reason=" + reason + ".");
        }

        private void Complete(Entity source, Entity tree, CombatPrototypeMapTreeState state, OnlinePlayer player,
            CombatPrototypeMapData map, CombatPrototypeMapTreeSettings settings, NetworkTick tick,
            CombatPrototypeMapDropSpawnSystem dropOwner)
        {
            var index = _obstacles[state.PlacementIndex];
            var oldObstacle = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source)[index];
            var position = EntityManager.GetComponentData<LocalTransform>(tree).Position;
            var drop = Entity.Null;
            var dropId = 0;
            try
            {
                drop = dropOwner.SpawnOwnedDrop(source, settings.DropPrefab, settings.DropResourceKey,
                    settings.DropItemId, settings.DropQuantity, position, out dropId);
                // Instantiate invalidated handles; reacquire them before the nonstructural tree commit.
                EntityManager.SetComponentData(tree, default(CombatPrototypeMapTreeProgress));
                var obstacle = oldObstacle;
                obstacle.Disabled = 1;
                var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                obstacles[index] = obstacle;
                state.Phase = CombatPrototypeMapTreePhase.Felled;
                state.CollectorNetworkId = 0;
                state.FelledTick = tick.SerializedData;
                EntityManager.SetComponentData(tree, state);
                Debug.Log("[CombatPrototype.Map] Tree felled and drop spawned; map=" + map.MapDefinitionId +
                    ", placement=" + state.PlacementIndex + ", NetworkId=" + player.NetworkId + ", DropId=" + dropId +
                    ", itemId=" + settings.DropItemId + ", quantity=" + settings.DropQuantity +
                    ", resource=" + settings.DropResourceKey + ".");
            }
            catch (Exception exception)
            {
                if (drop != Entity.Null) dropOwner.ReleaseDrop(drop);
                try
                {
                    var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                    obstacles[index] = oldObstacle;
                    Cancel(tree, state, map.MapDefinitionId, "DropOrCommitFailed");
                }
                catch (Exception rollback)
                {
                    Debug.LogError("[CombatPrototype.Map] Tree rollback failed; stage=RollbackTree, map=" +
                        map.MapDefinitionId + ", placement=" + state.PlacementIndex + ", DropId=" + dropId +
                        ", resource=" + settings.DropResourceKey + ". " + rollback);
                }
                throw new InvalidOperationException("Tree completion failed; placement=" + state.PlacementIndex +
                    ", DropId=" + dropId + ", itemId=" + settings.DropItemId +
                    ", resource=" + settings.DropResourceKey + ".", exception);
            }
        }

        private static void Reject(FixedString64Bytes mapId, OnlinePlayer player, string reason)
        {
            Debug.Log("[CombatPrototype.Map] Tree harvest rejected; map=" + mapId + ", NetworkId=" +
                player.NetworkId + ", player=" + player.Entity + ", reason=" + reason + ".");
        }
    }
}
