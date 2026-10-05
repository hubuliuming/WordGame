using System;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMapGatherRegrowSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapTreeRegrowSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapMineRegrowSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapDropCleanupSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapInventoryDropSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapResourceSaveSystem : SystemBase
    {
        private Entity _source;
        private Entity _statusSource;
        private EntityQuery _connections;
        private CombatPrototypeMapWorldSaveHudMode _status;
        private readonly CombatPrototypeMapWorldSaveManualRequests _manual = new CombatPrototypeMapWorldSaveManualRequests();
        private CombatPrototypeMapData _map;
        private CombatPrototypeMapResourcePersistenceSettings _settings;
        private CombatPrototypeMapResourceBinding[] _bindings;
        private bool[] _depleted, _nextDepleted;
        private double[] _deadlines, _nextDeadlines;
        private double _observedTime, _nextSaveAt, _nextCaptureAt;
        private bool _hasSnapshot, _dirty;
        private CombatPrototypeMapDropSpawnSystem _dropOwner;
        private CombatPrototypeMapInventoryDropDefinition[] _dropBindings;
        private CombatPrototypeMapDropPersistenceSnapshot _drops, _nextDrops;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypeMapResourcePersistenceSettings>();
            RequireForUpdate<CombatPrototypeMapResourceRestoreState>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _connections = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<NetworkStreamConnection>(), ComponentType.ReadOnly<CommandTarget>(),
                    ComponentType.ReadOnly<NetworkId>(), ComponentType.ReadOnly<NetworkStreamInGame>() },
                None = new[] { ComponentType.ReadOnly<NetworkStreamRequestDisconnect>() }
            });
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            if (_statusSource != source)
            {
                FlushAndReset("SourceChanged");
                _statusSource = source;
                _status = CombatPrototypeMapWorldSaveHudMode.NotSaved;
            }
            var settings = EntityManager.GetComponentData<CombatPrototypeMapResourcePersistenceSettings>(source);
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var phase = EntityManager.GetComponentData<CombatPrototypeMapResourceRestoreState>(source).Phase;
            var time = SystemAPI.Time.ElapsedTime;
            var manual = _manual.Collect(EntityManager, _connections, map.MapDefinitionId, settings, phase, time);
            if (settings.Enabled == 0 || phase != CombatPrototypeMapResourceRestorePhase.Ready)
            {
                _status = settings.Enabled == 0 ? CombatPrototypeMapWorldSaveHudMode.Disabled : CombatPrototypeMapWorldSaveHudMode.Hidden;
                return;
            }
            if (!manual && time < _nextCaptureAt) return;
            var placement = -1;
            var stage = "BindSnapshot";
            var saved = false;
            try
            {
                if (_source == Entity.Null)
                {
                    _map = map;
                    _settings = settings;
                    var restore = World.GetExistingSystemManaged<CombatPrototypeMapResourceRestoreSystem>();
                    _bindings = restore.Bindings;
                    if (settings.SaveGroundDrops != 0)
                    {
                        _dropOwner = World.GetExistingSystemManaged<CombatPrototypeMapDropSpawnSystem>();
                        _dropBindings = restore.DropBindings;
                        _drops = new CombatPrototypeMapDropPersistenceSnapshot();
                        _nextDrops = new CombatPrototypeMapDropPersistenceSnapshot();
                    }
                    _depleted = new bool[_bindings.Length]; _nextDepleted = new bool[_bindings.Length];
                    _deadlines = new double[_bindings.Length]; _nextDeadlines = new double[_bindings.Length];
                    _nextSaveAt = time + settings.SaveIntervalSeconds;
                    _source = source;
                    _status = CombatPrototypeMapWorldSaveHudMode.NotSaved;
                }
                stage = "CaptureCompleteSnapshot";
                var changed = false;
                for (var index = 0; index < _bindings.Length; index++)
                {
                    var binding = _bindings[index]; placement = binding.PlacementIndex;
                    Read(binding, out var depleted, out var deadline);
                    if (double.IsNaN(deadline) || double.IsInfinity(deadline) || deadline < 0 ||
                        (depleted && binding.RegrowEnabled != 0 && deadline <= 0))
                        throw new InvalidOperationException("Invalid resource regeneration deadline.");
                    _nextDepleted[index] = depleted;
                    _nextDeadlines[index] = depleted && binding.RegrowEnabled != 0 ? deadline : 0;
                    changed |= depleted != _depleted[index] || _nextDeadlines[index] != _deadlines[index];
                }
                if (_settings.SaveGroundDrops != 0)
                {
                    _nextDrops.Capture(EntityManager, _dropOwner, source, _dropBindings, time);
                    changed |= !_nextDrops.SameState(_drops);
                    var dropSwap = _drops; _drops = _nextDrops; _nextDrops = dropSwap;
                }
                var depletedSwap = _depleted; _depleted = _nextDepleted; _nextDepleted = depletedSwap;
                var deadlineSwap = _deadlines; _deadlines = _nextDeadlines; _nextDeadlines = deadlineSwap;
                _observedTime = time;
                _hasSnapshot = true;
                _nextCaptureAt = 0d;
                _dirty |= changed;
                if (manual || (_dirty && (changed || time >= _nextSaveAt)) || time >= _nextSaveAt)
                    saved = TrySave(manual ? "ManualRequest" : changed ? "StateChanged" : "Checkpoint");
            }
            catch (Exception exception)
            {
                _status = CombatPrototypeMapWorldSaveHudMode.CaptureFailed;
                _nextCaptureAt = time + settings.SaveIntervalSeconds;
                Debug.LogError("[CombatPrototype.Map] World snapshot capture failed; stage=" + stage + ", map=" + map.MapDefinitionId +
                    ", slot=" + settings.SaveSlotId + ", placement=" + placement + ". " + exception);
            }
            finally { if (manual) _manual.Complete(saved); }
        }

        internal CombatPrototypeMapWorldSaveHudState ReadHud(Entity source, Entity player)
        {
            if (_statusSource != source || _status == CombatPrototypeMapWorldSaveHudMode.Hidden)
                return CombatPrototypeMapWorldSaveHudState.Hidden;
            var frame = new CombatPrototypeMapWorldSaveHudState { Mode = _status };
            _manual.ApplyFeedback(player, ref frame);
            return frame;
        }

        private void Read(CombatPrototypeMapResourceBinding binding, out bool depleted, out double deadline)
        {
            if (binding.Kind == CombatPrototypeMapResourceSaveKind.Gather)
            {
                var state = EntityManager.GetComponentData<CombatPrototypeMapGatherState>(binding.Entity);
                if (state.PlacementIndex != binding.PlacementIndex || state.Phase > CombatPrototypeMapGatherPhase.Depleted)
                    throw new InvalidOperationException("Invalid gather state identity/phase.");
                depleted = state.Phase == CombatPrototypeMapGatherPhase.Depleted;
                deadline = EntityManager.GetComponentData<CombatPrototypeMapGatherProgress>(binding.Entity).RegrowAt;
            }
            else if (binding.Kind == CombatPrototypeMapResourceSaveKind.Tree)
            {
                var state = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(binding.Entity);
                if (state.PlacementIndex != binding.PlacementIndex || state.Phase > CombatPrototypeMapTreePhase.Felled)
                    throw new InvalidOperationException("Invalid tree state identity/phase.");
                depleted = state.Phase == CombatPrototypeMapTreePhase.Felled;
                deadline = EntityManager.GetComponentData<CombatPrototypeMapTreeProgress>(binding.Entity).RegrowAt;
            }
            else
            {
                var state = EntityManager.GetComponentData<CombatPrototypeMapMineState>(binding.Entity);
                if (state.PlacementIndex != binding.PlacementIndex || state.Phase > CombatPrototypeMapMinePhase.Depleted)
                    throw new InvalidOperationException("Invalid mine state identity/phase.");
                depleted = state.Phase == CombatPrototypeMapMinePhase.Depleted;
                deadline = EntityManager.GetComponentData<CombatPrototypeMapMineProgress>(binding.Entity).RegrowAt;
            }
        }

        private bool TrySave(string reason)
        {
            var path = CombatPrototypeMapResourceSaveStore.GetSavePath(_settings.SaveSlotId.ToString(), _map.MapDefinitionId.ToString());
            _nextSaveAt = _observedTime + _settings.SaveIntervalSeconds;
            try
            {
                var count = 0;
                for (var index = 0; index < _depleted.Length; index++) if (_depleted[index]) count++;
                var data = CombatPrototypeMapResourceSaveStore.CreateInitial(_settings, _map);
                data.Resources = new CombatPrototypeMapResourceSaveEntry[count];
                var entryIndex = 0;
                for (var index = 0; index < _bindings.Length; index++)
                {
                    if (!_depleted[index]) continue;
                    var binding = _bindings[index];
                    data.Resources[entryIndex++] = new CombatPrototypeMapResourceSaveEntry
                    {
                        Kind = binding.SaveKind, PlacementIndex = binding.PlacementIndex, ObjectId = binding.ObjectId,
                        RemainingSeconds = binding.RegrowEnabled != 0 ?
                            Math.Min(binding.RegrowSeconds, Math.Max(_deadlines[index] - _observedTime, 0d)) : 0d
                    };
                }
                if (_settings.SaveGroundDrops != 0)
                {
                    data.LastDropId = _drops.LastDropId;
                    data.Drops = _drops.CreateEntries(_observedTime);
                }
                CombatPrototypeMapResourceSaveStore.SavePrepared(data);
                _dirty = false;
                _status = CombatPrototypeMapWorldSaveHudMode.Saved;
                Debug.Log("[CombatPrototype.Map] World snapshot saved; map=" + _map.MapDefinitionId + ", slot=" +
                    _settings.SaveSlotId + ", reason=" + reason + ", depleted=" + count + ", drops=" + data.Drops.Length + ", lastDropId=" + data.LastDropId + ", path=" + path + ".");
                return true;
            }
            catch (Exception exception)
            {
                _dirty = true;
                _status = CombatPrototypeMapWorldSaveHudMode.SaveFailed;
                Debug.LogError("[CombatPrototype.Map] World snapshot save failed; map=" + _map.MapDefinitionId + ", slot=" +
                    _settings.SaveSlotId + ", reason=" + reason + ", path=" + path + ". " + exception);
                return false;
            }
        }

        private void FlushAndReset(string reason)
        {
            // Owned caches survive resource destruction; shutdown never reads released ECS entities.
            if (_hasSnapshot) TrySave(reason);
            _source = Entity.Null; _hasSnapshot = false; _dirty = false;
            _statusSource = Entity.Null;
            _status = CombatPrototypeMapWorldSaveHudMode.Hidden;
            _manual.Reset();
            _bindings = null; _depleted = _nextDepleted = null; _deadlines = _nextDeadlines = null;
            _dropOwner = null; _dropBindings = null; _drops = _nextDrops = null;
            _nextSaveAt = _nextCaptureAt = 0;
        }

        protected override void OnStopRunning() { FlushAndReset("StopRunning"); }
        protected override void OnDestroy() { FlushAndReset("WorldDestroy"); }
    }
}
