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
    [UpdateAfter(typeof(CombatPrototypeMapInteractionHudStateSystem))]
    public partial class CombatPrototypeMapResourceStatusHudStateSystem : SystemBase
    {
        private EntityQuery _points;
        private EntityQuery _trees;
        private EntityQuery _mines;
        private readonly List<CombatPrototypeMapResourceStatusTargetSelector.Target> _targets =
            new List<CombatPrototypeMapResourceStatusTargetSelector.Target>();
        private readonly Dictionary<(byte, int), CombatPrototypeMapResourceStatusTargetSelector.Target> _identities =
            new Dictionary<(byte, int), CombatPrototypeMapResourceStatusTargetSelector.Target>();
        private readonly HashSet<(byte, int)> _failed = new HashSet<(byte, int)>();
        private readonly Dictionary<Entity, CombatPrototypeMapResourceStatusHudState> _frames =
            new Dictionary<Entity, CombatPrototypeMapResourceStatusHudState>();

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _points = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapGatherState>());
            _trees = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapTreeState>());
            _mines = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapMineState>());
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            _targets.Clear();
            _identities.Clear();
            _failed.Clear();
            _frames.Clear();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapResourceStatusHudSettings>(source);
            if (settings.Enabled == 0) { CommitFrames(); return; }
            var tree = EntityManager.GetComponentData<CombatPrototypeMapTreeSettings>(source);
            var mine = EntityManager.GetComponentData<CombatPrototypeMapMineSettings>(source);
            var time = SystemAPI.Time.ElapsedTime;
            using var points = _points.ToEntityArray(Allocator.Temp);
            using var trees = _trees.ToEntityArray(Allocator.Temp);
            using var mines = _mines.ToEntityArray(Allocator.Temp);
            foreach (var point in points)
                Capture(map.MapDefinitionId, point, CombatPrototypeMapInteractionKind.Gather, time, tree, mine);
            if (tree.Enabled != 0)
                foreach (var item in trees)
                    Capture(map.MapDefinitionId, item, CombatPrototypeMapInteractionKind.Tree, time, tree, mine);
            if (mine.Enabled != 0)
                foreach (var item in mines)
                    Capture(map.MapDefinitionId, item, CombatPrototypeMapInteractionKind.Mine, time, tree, mine);

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
                    if (EntityManager.GetComponentData<GhostOwner>(player).NetworkId != id.ValueRO.Value ||
                        EntityManager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0) continue;
                    var position = EntityManager.GetComponentData<LocalTransform>(player).Position.xz;
                    if (!math.all(math.isfinite(position)))
                        throw new InvalidOperationException("Resource status requires a finite player position.");
                    stage = "SelectTarget";
                    var interaction = EntityManager.GetComponentData<CombatPrototypeMapInteractionHudState>(player);
                    var target = CombatPrototypeMapResourceStatusTargetSelector.Select(position, player, interaction, _targets, _identities, _failed);
                    _frames[player] = target.Entity == Entity.Null ? CombatPrototypeMapResourceStatusHudState.Hidden :
                        new CombatPrototypeMapResourceStatusHudState
                        {
                            Mode = target.Mode == CombatPrototypeMapResourceStatusHudMode.Occupied && target.Collector == player ?
                                CombatPrototypeMapResourceStatusHudMode.Working : target.Mode,
                            Kind = target.Kind, PlacementIndex = target.PlacementIndex, RemainingSeconds = target.RemainingSeconds
                        };
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Resource status frame failed; stage=" + stage + ", map=" +
                        map.MapDefinitionId + ", NetworkId=" + id.ValueRO.Value + ", player=" + player + ". " + exception);
                }
            }
            CommitFrames();
        }

        private void Capture(FixedString64Bytes mapId, Entity entity, CombatPrototypeMapInteractionKind kind,
            double time, CombatPrototypeMapTreeSettings tree, CombatPrototypeMapMineSettings mine)
        {
            var target = new CombatPrototypeMapResourceStatusTargetSelector.Target
                { Entity = entity, Kind = (byte)kind, PlacementIndex = -1 };
            try
            {
                byte phase;
                byte regrowEnabled;
                double deadline = 0d;
                switch (kind)
                {
                    case CombatPrototypeMapInteractionKind.Gather:
                        var gatherState = EntityManager.GetComponentData<CombatPrototypeMapGatherState>(entity);
                        target.PlacementIndex = gatherState.PlacementIndex;
                        var gatherConfig = EntityManager.GetComponentData<CombatPrototypeMapGatherConfig>(entity);
                        var gatherProgress = EntityManager.GetComponentData<CombatPrototypeMapGatherProgress>(entity);
                        target.InteractionDistance = gatherConfig.InteractionDistance;
                        target.Collector = gatherProgress.Collector;
                        phase = (byte)gatherState.Phase;
                        regrowEnabled = gatherConfig.RegrowEnabled;
                        deadline = gatherProgress.RegrowAt;
                        break;
                    case CombatPrototypeMapInteractionKind.Tree:
                        var treeState = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(entity);
                        target.PlacementIndex = treeState.PlacementIndex;
                        var treeProgress = EntityManager.GetComponentData<CombatPrototypeMapTreeProgress>(entity);
                        target.InteractionDistance = tree.InteractionDistance;
                        target.Collector = treeProgress.Collector;
                        phase = (byte)treeState.Phase;
                        regrowEnabled = tree.RegrowEnabled;
                        deadline = treeProgress.RegrowAt;
                        break;
                    case CombatPrototypeMapInteractionKind.Mine:
                        var mineState = EntityManager.GetComponentData<CombatPrototypeMapMineState>(entity);
                        target.PlacementIndex = mineState.PlacementIndex;
                        var mineProgress = EntityManager.GetComponentData<CombatPrototypeMapMineProgress>(entity);
                        target.InteractionDistance = mine.InteractionDistance;
                        target.Collector = mineProgress.Collector;
                        phase = (byte)mineState.Phase;
                        regrowEnabled = mine.RegrowEnabled;
                        deadline = mineProgress.RegrowAt;
                        break;
                    default: throw new InvalidOperationException("Unsupported resource status kind: " + kind);
                }
                target.Position = EntityManager.GetComponentData<LocalTransform>(entity).Position.xz;
                if (!math.all(math.isfinite(target.Position)) || target.PlacementIndex < 0)
                    throw new InvalidOperationException("Resource status requires a finite resource position and nonnegative placement.");
                // All three existing phase enums use available=0, working=1, depleted=2.
                if (phase == 0) target.Mode = CombatPrototypeMapResourceStatusHudMode.Available;
                else if (phase == 1)
                {
                    if (target.Collector == Entity.Null)
                        throw new InvalidOperationException("An active resource requires its authoritative collector.");
                    target.Mode = CombatPrototypeMapResourceStatusHudMode.Occupied;
                }
                else if (phase == 2)
                {
                    if (regrowEnabled == 0) target.Mode = CombatPrototypeMapResourceStatusHudMode.Depleted;
                    else
                    {
                        if (double.IsNaN(deadline) || double.IsInfinity(deadline) || deadline <= 0d ||
                            double.IsNaN(time) || double.IsInfinity(time))
                            throw new InvalidOperationException("Regrowing resources require a finite positive deadline and finite server time.");
                        target.Mode = time >= deadline ? CombatPrototypeMapResourceStatusHudMode.Waiting :
                            CombatPrototypeMapResourceStatusHudMode.Regrowing;
                        target.RemainingSeconds = (float)Math.Ceiling(Math.Max(deadline - time, 0d));
                        if (!math.isfinite(target.RemainingSeconds))
                            throw new InvalidOperationException("Regrowth remaining seconds exceed the snapshot capacity.");
                    }
                }
                else throw new InvalidOperationException("Unsupported resource phase: " + phase);
                _identities.Add((target.Kind, target.PlacementIndex), target);
                _targets.Add(target);
            }
            catch (Exception exception)
            {
                if (target.PlacementIndex >= 0) _failed.Add((target.Kind, target.PlacementIndex));
                Debug.LogError("[CombatPrototype.Map] Resource status resource failed; stage=CaptureState, map=" + mapId +
                    ", type=" + kind + ", placement=" + target.PlacementIndex + ", resource=" + entity + ". " + exception);
            }
        }

        private void CommitFrames()
        {
            foreach (var (current, entity) in SystemAPI.Query<RefRW<CombatPrototypeMapResourceStatusHudState>>()
                         .WithAll<CombatPrototypePlayerNetCode>().WithEntityAccess())
            {
                var next = _frames.TryGetValue(entity, out var frame) ? frame : CombatPrototypeMapResourceStatusHudState.Hidden;
                var old = current.ValueRO;
                if (old.Mode != next.Mode || old.Kind != next.Kind || old.PlacementIndex != next.PlacementIndex ||
                    old.RemainingSeconds != next.RemainingSeconds) current.ValueRW = next;
            }
        }

        protected override void OnStopRunning()
        {
            Dependency.Complete();
            _targets.Clear();
            _identities.Clear();
            _failed.Clear();
            _frames.Clear();
            CommitFrames();
        }
    }
}
