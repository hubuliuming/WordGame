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
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapResourceSaveSystem : SystemBase
    {
        private Entity _source;
        private CombatPrototypeMapData _map;
        private CombatPrototypeMapResourcePersistenceSettings _settings;
        private CombatPrototypeMapResourceBinding[] _bindings;
        private bool[] _depleted, _nextDepleted;
        private double[] _deadlines, _nextDeadlines;
        private double _observedTime, _nextSaveAt, _nextCaptureAt;
        private bool _hasSnapshot, _dirty;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypeMapResourcePersistenceSettings>();
            RequireForUpdate<CombatPrototypeMapResourceRestoreState>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            if (_source != source) { FlushAndReset("SourceChanged"); }
            var settings = EntityManager.GetComponentData<CombatPrototypeMapResourcePersistenceSettings>(source);
            if (settings.Enabled == 0 || EntityManager.GetComponentData<CombatPrototypeMapResourceRestoreState>(source).Phase !=
                CombatPrototypeMapResourceRestorePhase.Ready) return;
            if (_source == Entity.Null)
            {
                _source = source;
                _map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
                _settings = settings;
                _bindings = World.GetExistingSystemManaged<CombatPrototypeMapResourceRestoreSystem>().Bindings;
                _depleted = new bool[_bindings.Length]; _nextDepleted = new bool[_bindings.Length];
                _deadlines = new double[_bindings.Length]; _nextDeadlines = new double[_bindings.Length];
                _nextSaveAt = SystemAPI.Time.ElapsedTime + settings.SaveIntervalSeconds;
            }
            var time = SystemAPI.Time.ElapsedTime;
            if (time < _nextCaptureAt) return;
            var placement = -1;
            try
            {
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
                var depletedSwap = _depleted; _depleted = _nextDepleted; _nextDepleted = depletedSwap;
                var deadlineSwap = _deadlines; _deadlines = _nextDeadlines; _nextDeadlines = deadlineSwap;
                _observedTime = time;
                _hasSnapshot = true;
                _dirty |= changed;
                if ((_dirty && (changed || time >= _nextSaveAt)) || time >= _nextSaveAt)
                    TrySave(changed ? "StateChanged" : "Checkpoint");
            }
            catch (Exception exception)
            {
                _nextCaptureAt = time + _settings.SaveIntervalSeconds;
                Debug.LogError("[CombatPrototype.Map] Resource capture failed; stage=CaptureCompleteSnapshot, map=" + _map.MapDefinitionId +
                    ", slot=" + _settings.SaveSlotId + ", placement=" + placement + ". " + exception);
            }
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

        private void TrySave(string reason)
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
                CombatPrototypeMapResourceSaveStore.SavePrepared(data);
                _dirty = false;
                Debug.Log("[CombatPrototype.Map] Resource snapshot saved; map=" + _map.MapDefinitionId + ", slot=" +
                    _settings.SaveSlotId + ", reason=" + reason + ", depleted=" + count + ", path=" + path + ".");
            }
            catch (Exception exception)
            {
                _dirty = true;
                Debug.LogError("[CombatPrototype.Map] Resource snapshot save failed; map=" + _map.MapDefinitionId + ", slot=" +
                    _settings.SaveSlotId + ", reason=" + reason + ", path=" + path + ". " + exception);
            }
        }

        private void FlushAndReset(string reason)
        {
            // Owned caches survive resource destruction; shutdown never reads released ECS entities.
            if (_hasSnapshot) TrySave(reason);
            _source = Entity.Null; _hasSnapshot = false; _dirty = false;
            _bindings = null; _depleted = _nextDepleted = null; _deadlines = _nextDeadlines = null;
            _nextSaveAt = _nextCaptureAt = 0;
        }

        protected override void OnStopRunning() { FlushAndReset("StopRunning"); }
        protected override void OnDestroy() { FlushAndReset("WorldDestroy"); }
    }
}
