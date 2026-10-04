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
    [UpdateAfter(typeof(CombatPrototypePlayerDamageSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapDropCleanupSystem))]
    [UpdateBefore(typeof(CombatPrototypeMapGatherSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapInteractionSystem : SystemBase
    {
        private struct Request
        {
            public Entity Player;
            public int NetworkId;
            public CombatPrototypePlayerInput Input;
        }

        private EntityQuery _points;
        private EntityQuery _trees;
        private EntityQuery _mines;
        private readonly HashSet<Entity> _activeCollectors = new HashSet<Entity>();

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            RequireForUpdate<NetworkTime>();
            _points = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapGatherState>(),
                ComponentType.ReadOnly<CombatPrototypeMapGatherProgress>(),
                ComponentType.ReadOnly<CombatPrototypeMapGatherConfig>(), ComponentType.ReadOnly<LocalTransform>());
            _trees = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapTreeState>(),
                ComponentType.ReadOnly<CombatPrototypeMapTreeProgress>(), ComponentType.ReadOnly<LocalTransform>());
            _mines = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapMineState>(),
                ComponentType.ReadOnly<CombatPrototypeMapMineProgress>(), ComponentType.ReadOnly<LocalTransform>());
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var requests = new NativeList<Request>(Allocator.Temp);
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
                    try
                    {
                        var input = EntityManager.GetComponentData<CombatPrototypePlayerInput>(player);
                        if (!input.Gather.IsSet) continue;
                        var request = new Request { Player = player, NetworkId = id.ValueRO.Value, Input = input };
                        var index = requests.Length;
                        requests.Add(request);
                        while (index > 0 && requests[index - 1].NetworkId > request.NetworkId)
                        {
                            requests[index] = requests[index - 1];
                            index--;
                        }
                        requests[index] = request;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Interaction request failed; stage=ReadInput, map=" +
                            map.MapDefinitionId + ", NetworkId=" + id.ValueRO.Value + ", player=" + player + ". " + exception);
                    }
                }
                if (requests.Length == 0) return;

                var gather = World.GetExistingSystemManaged<CombatPrototypeMapGatherSystem>();
                var treeHarvest = World.GetExistingSystemManaged<CombatPrototypeMapTreeHarvestSystem>();
                var mineHarvest = World.GetExistingSystemManaged<CombatPrototypeMapMineHarvestSystem>();
                if (gather == null || treeHarvest == null || mineHarvest == null)
                {
                    Debug.LogError("[CombatPrototype.Map] Interaction prerequisites failed; stage=ValidateServices, map=" +
                        map.MapDefinitionId + ", gather=" + (gather != null) + ", tree=" + (treeHarvest != null) +
                        ", mine=" + (mineHarvest != null) + ".");
                    return;
                }
                var treeSettings = EntityManager.GetComponentData<CombatPrototypeMapTreeSettings>(source);
                var mineSettings = EntityManager.GetComponentData<CombatPrototypeMapMineSettings>(source);
                if ((treeSettings.Enabled != 0 || mineSettings.Enabled != 0) &&
                    (World.GetExistingSystemManaged<CombatPrototypeMapDropSpawnSystem>() == null ||
                     !SystemAPI.GetSingleton<NetworkTime>().ServerTick.IsValid))
                {
                    Debug.LogError("[CombatPrototype.Map] Interaction prerequisites failed; stage=ValidateDropServices, map=" +
                        map.MapDefinitionId + ", treeResource=" + treeSettings.ResourceKey +
                        ", mineResource=" + mineSettings.ResourceKey + ".");
                    return;
                }

                // Snapshot busy players before any resource system completes or cancels this tick.
                _activeCollectors.Clear();
                foreach (var (state, progress) in SystemAPI.Query<RefRO<CombatPrototypeMapGatherState>,
                             RefRO<CombatPrototypeMapGatherProgress>>())
                    if (state.ValueRO.Phase == CombatPrototypeMapGatherPhase.Collecting)
                        _activeCollectors.Add(progress.ValueRO.Collector);
                foreach (var (state, progress) in SystemAPI.Query<RefRO<CombatPrototypeMapTreeState>,
                             RefRO<CombatPrototypeMapTreeProgress>>())
                    if (state.ValueRO.Phase == CombatPrototypeMapTreePhase.Chopping)
                        _activeCollectors.Add(progress.ValueRO.Collector);
                foreach (var (state, progress) in SystemAPI.Query<RefRO<CombatPrototypeMapMineState>,
                             RefRO<CombatPrototypeMapMineProgress>>())
                    if (state.ValueRO.Phase == CombatPrototypeMapMinePhase.Mining)
                        _activeCollectors.Add(progress.ValueRO.Collector);

                using var points = _points.ToEntityArray(Allocator.Temp);
                using var trees = _trees.ToEntityArray(Allocator.Temp);
                using var mines = _mines.ToEntityArray(Allocator.Temp);
                var time = SystemAPI.Time.ElapsedTime;
                foreach (var request in requests)
                {
                    var stage = "ValidatePlayer";
                    var target = default(CombatPrototypeMapInteractionTarget);
                    try
                    {
                        if (_activeCollectors.Contains(request.Player))
                        {
                            Reject(map.MapDefinitionId, request, "AlreadyInteracting");
                            continue;
                        }
                        var reason = RejectPlayer(request, out var health);
                        if (reason != null)
                        {
                            Reject(map.MapDefinitionId, request, reason);
                            continue;
                        }
                        stage = "SelectTarget";
                        var position = EntityManager.GetComponentData<LocalTransform>(request.Player).Position.xz;
                        target = CombatPrototypeMapInteractionTargetSelector.Select(EntityManager, position,
                            points, trees, mines, treeSettings, mineSettings);
                        if (target.Entity == Entity.Null)
                        {
                            Reject(map.MapDefinitionId, request, "NoAvailableTarget");
                            continue;
                        }
                        stage = "ReserveTarget";
                        bool started;
                        switch (target.Kind)
                        {
                            case CombatPrototypeMapInteractionKind.Gather:
                                started = gather.TryBegin(target.Entity, request.Player, request.NetworkId,
                                    health.HitSequence, time, map.MapDefinitionId);
                                break;
                            case CombatPrototypeMapInteractionKind.Tree:
                                started = treeHarvest.TryBegin(target.Entity, request.Player, request.NetworkId,
                                    health.HitSequence, time, map.MapDefinitionId, treeSettings);
                                break;
                            case CombatPrototypeMapInteractionKind.Mine:
                                started = mineHarvest.TryBegin(target.Entity, request.Player, request.NetworkId,
                                    health.HitSequence, time, map.MapDefinitionId, mineSettings);
                                break;
                            default:
                                throw new InvalidOperationException("Unsupported resource interaction kind: " + target.Kind);
                        }
                        if (!started)
                        {
                            Reject(map.MapDefinitionId, request, "TargetUnavailable");
                            continue;
                        }
                        // Commit each reservation before selecting for the next NetworkId.
                        _activeCollectors.Add(request.Player);
                        Debug.Log("[CombatPrototype.Map] Interaction selected; map=" + map.MapDefinitionId +
                            ", NetworkId=" + request.NetworkId + ", player=" + request.Player + ", type=" + target.Kind +
                            ", placement=" + target.PlacementIndex + ", distance=" + math.sqrt(target.DistanceSquared) + ".");
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Interaction request failed; stage=" + stage + ", map=" +
                            map.MapDefinitionId + ", NetworkId=" + request.NetworkId + ", player=" + request.Player +
                            ", type=" + target.Kind + ", placement=" + target.PlacementIndex +
                            ", target=" + target.Entity + ". " + exception);
                        if (target.Entity == Entity.Null) continue;
                        try
                        {
                            switch (target.Kind)
                            {
                                case CombatPrototypeMapInteractionKind.Gather:
                                    gather.CancelBegin(target.Entity, request.Player, map.MapDefinitionId);
                                    break;
                                case CombatPrototypeMapInteractionKind.Tree:
                                    treeHarvest.CancelBegin(target.Entity, request.Player, map.MapDefinitionId);
                                    break;
                                case CombatPrototypeMapInteractionKind.Mine:
                                    mineHarvest.CancelBegin(target.Entity, request.Player, map.MapDefinitionId);
                                    break;
                            }
                        }
                        catch (Exception cleanup)
                        {
                            Debug.LogError("[CombatPrototype.Map] Interaction reservation cleanup failed; stage=CancelPartialReservation, map=" +
                                map.MapDefinitionId + ", NetworkId=" + request.NetworkId + ", type=" + target.Kind +
                                ", placement=" + target.PlacementIndex + ", target=" + target.Entity + ". " + cleanup);
                        }
                    }
                }
            }
            finally { requests.Dispose(); }
        }

        internal string GetInteractionHintRejection(Entity player, int networkId, CombatPrototypePlayerInput input)
        {
            return RejectPlayer(new Request { Player = player, NetworkId = networkId, Input = input }, out _);
        }

        private string RejectPlayer(Request request, out CombatPrototypePlayerHealth health)
        {
            health = EntityManager.GetComponentData<CombatPrototypePlayerHealth>(request.Player);
            if (EntityManager.GetComponentData<GhostOwner>(request.Player).NetworkId != request.NetworkId)
                return "CommandTargetOwnerMismatch";
            if (health.IsDead != 0) return "PlayerDead";
            if (request.Input.Attack.IsSet || EntityManager.GetComponentData<CombatPrototypeMeleeState>(request.Player).Phase !=
                CombatPrototypeAttackPhase.Ready) return "AttackInProgress";
            if (!math.all(math.isfinite(request.Input.Move))) return "InvalidMoveInput";
            if (math.lengthsq(request.Input.Move) != 0f) return "PlayerMoving";
            return null;
        }

        private static void Reject(FixedString64Bytes mapId, Request request, string reason)
        {
            Debug.Log("[CombatPrototype.Map] Interaction rejected; map=" + mapId + ", NetworkId=" +
                request.NetworkId + ", player=" + request.Player + ", reason=" + reason + ".");
        }
    }
}
