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
    [UpdateAfter(typeof(CombatPrototypePlayerRespawnSystem))]
    [CreateAfter(typeof(CombatPrototypeMapInteractionSystem))]
    public partial class CombatPrototypeMapInteractionHudStateSystem : SystemBase
    {
        private EntityQuery _points;
        private EntityQuery _trees;
        private EntityQuery _mines;
        private CombatPrototypeMapInteractionSystem _interaction;
        private readonly Dictionary<Entity, CombatPrototypeMapInteractionHudState> _active =
            new Dictionary<Entity, CombatPrototypeMapInteractionHudState>();
        private readonly Dictionary<Entity, CombatPrototypeMapInteractionHudState> _frames =
            new Dictionary<Entity, CombatPrototypeMapInteractionHudState>();

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _points = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapGatherState>(),
                ComponentType.ReadOnly<CombatPrototypeMapGatherProgress>(),
                ComponentType.ReadOnly<CombatPrototypeMapGatherConfig>(), ComponentType.ReadOnly<LocalTransform>());
            _trees = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapTreeState>(),
                ComponentType.ReadOnly<CombatPrototypeMapTreeProgress>(), ComponentType.ReadOnly<LocalTransform>());
            _mines = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapMineState>(),
                ComponentType.ReadOnly<CombatPrototypeMapMineProgress>(), ComponentType.ReadOnly<LocalTransform>());
            _interaction = World.GetExistingSystemManaged<CombatPrototypeMapInteractionSystem>();
            if (_interaction == null)
                throw new InvalidOperationException("[CombatPrototype.Map] HUD requires the existing unified interaction system.");
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            _active.Clear();
            _frames.Clear();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapInteractionHudSettings>(source);
            if (settings.Enabled == 0)
            {
                CommitFrames();
                return;
            }

            var tree = EntityManager.GetComponentData<CombatPrototypeMapTreeSettings>(source);
            var mine = EntityManager.GetComponentData<CombatPrototypeMapMineSettings>(source);
            var time = SystemAPI.Time.ElapsedTime;
            using var points = _points.ToEntityArray(Allocator.Temp);
            using var trees = _trees.ToEntityArray(Allocator.Temp);
            using var mines = _mines.ToEntityArray(Allocator.Temp);
            foreach (var point in points)
                CaptureActive(map.MapDefinitionId, point, CombatPrototypeMapInteractionKind.Gather, time, tree, mine);
            if (tree.Enabled != 0)
                foreach (var item in trees)
                    CaptureActive(map.MapDefinitionId, item, CombatPrototypeMapInteractionKind.Tree, time, tree, mine);
            if (mine.Enabled != 0)
                foreach (var item in mines)
                    CaptureActive(map.MapDefinitionId, item, CombatPrototypeMapInteractionKind.Mine, time, tree, mine);

            foreach (var (stream, command, id) in SystemAPI.Query<RefRO<NetworkStreamConnection>,
                         RefRO<CommandTarget>, RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>())
            {
                var player = command.ValueRO.targetEntity;
                if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !EntityManager.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                    !EntityManager.HasComponent<Simulate>(player) || !EntityManager.IsComponentEnabled<Simulate>(player))
                    continue;
                var stage = "ReadPlayer";
                try
                {
                    var input = EntityManager.GetComponentData<CombatPrototypePlayerInput>(player);
                    var rejection = _interaction.GetInteractionHintRejection(player, id.ValueRO.Value, input);
                    // A foreign connection must not replace the actual owner's display state.
                    if (rejection == "CommandTargetOwnerMismatch") continue;
                    var frame = CombatPrototypeMapInteractionHudState.Hidden;
                    if (rejection == null)
                    {
                        stage = "SelectTarget";
                        if (!_active.TryGetValue(player, out frame))
                        {
                            var position = EntityManager.GetComponentData<LocalTransform>(player).Position.xz;
                            var target = CombatPrototypeMapInteractionTargetSelector.Select(EntityManager, position,
                                points, trees, mines, tree, mine);
                            frame = target.Entity == Entity.Null ? CombatPrototypeMapInteractionHudState.Hidden :
                                new CombatPrototypeMapInteractionHudState
                                {
                                    Mode = CombatPrototypeMapInteractionHudMode.Ready, Kind = (byte)target.Kind,
                                    PlacementIndex = target.PlacementIndex
                                };
                        }
                    }
                    _frames[player] = frame;
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] HUD frame failed; stage=" + stage + ", map=" +
                        map.MapDefinitionId + ", NetworkId=" + id.ValueRO.Value + ", player=" + player + ". " + exception);
                }
            }
            CommitFrames();
        }

        private void CaptureActive(FixedString64Bytes mapId, Entity entity, CombatPrototypeMapInteractionKind kind,
            double time, CombatPrototypeMapTreeSettings tree, CombatPrototypeMapMineSettings mine)
        {
            var placement = -1;
            try
            {
                Entity collector;
                double finishAt;
                float duration;
                switch (kind)
                {
                    case CombatPrototypeMapInteractionKind.Gather:
                        var gatherState = EntityManager.GetComponentData<CombatPrototypeMapGatherState>(entity);
                        if (gatherState.Phase != CombatPrototypeMapGatherPhase.Collecting) return;
                        placement = gatherState.PlacementIndex;
                        var gatherProgress = EntityManager.GetComponentData<CombatPrototypeMapGatherProgress>(entity);
                        collector = gatherProgress.Collector;
                        finishAt = gatherProgress.FinishAt;
                        duration = EntityManager.GetComponentData<CombatPrototypeMapGatherConfig>(entity).GatherDuration;
                        break;
                    case CombatPrototypeMapInteractionKind.Tree:
                        var treeState = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(entity);
                        if (treeState.Phase != CombatPrototypeMapTreePhase.Chopping) return;
                        placement = treeState.PlacementIndex;
                        var treeProgress = EntityManager.GetComponentData<CombatPrototypeMapTreeProgress>(entity);
                        collector = treeProgress.Collector;
                        finishAt = treeProgress.FinishAt;
                        duration = tree.HarvestDuration;
                        break;
                    case CombatPrototypeMapInteractionKind.Mine:
                        var mineState = EntityManager.GetComponentData<CombatPrototypeMapMineState>(entity);
                        if (mineState.Phase != CombatPrototypeMapMinePhase.Mining) return;
                        placement = mineState.PlacementIndex;
                        var mineProgress = EntityManager.GetComponentData<CombatPrototypeMapMineProgress>(entity);
                        collector = mineProgress.Collector;
                        finishAt = mineProgress.FinishAt;
                        duration = mine.HarvestDuration;
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported HUD interaction kind: " + kind);
                }
                var fraction = (float)(1d - (finishAt - time) / duration);
                if (!math.isfinite(fraction))
                    throw new InvalidOperationException("Active interaction requires finite authoritative progress.");
                _active.Add(collector, new CombatPrototypeMapInteractionHudState
                {
                    Mode = CombatPrototypeMapInteractionHudMode.Working, Kind = (byte)kind, PlacementIndex = placement,
                    ProgressPermille = (ushort)math.round(math.saturate(fraction) * 1000f)
                });
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] HUD resource failed; stage=CaptureProgress, map=" +
                    mapId + ", type=" + kind + ", placement=" + placement + ", resource=" + entity + ". " + exception);
            }
        }

        private void CommitFrames()
        {
            foreach (var (current, entity) in SystemAPI.Query<RefRW<CombatPrototypeMapInteractionHudState>>()
                         .WithAll<CombatPrototypePlayerNetCode>().WithEntityAccess())
            {
                var next = _frames.TryGetValue(entity, out var frame) ? frame : CombatPrototypeMapInteractionHudState.Hidden;
                var old = current.ValueRO;
                if (old.Mode != next.Mode || old.Kind != next.Kind || old.PlacementIndex != next.PlacementIndex ||
                    old.ProgressPermille != next.ProgressPermille) current.ValueRW = next;
            }
        }

        protected override void OnStopRunning()
        {
            Dependency.Complete();
            _active.Clear();
            _frames.Clear();
            CommitFrames();
        }
    }
}
