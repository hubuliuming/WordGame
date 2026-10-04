using System;
using System.Collections.Generic;
using System.IO;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMapGatherSpawnSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapTreeSpawnSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapMineSpawnSystem))]
    [UpdateBefore(typeof(CombatPrototypeGoInGameServerSystem))]
    public partial class CombatPrototypeMapResourceRestoreSystem : SystemBase
    {
        private Entity _source;
        internal CombatPrototypeMapResourceBinding[] Bindings { get; private set; } = Array.Empty<CombatPrototypeMapResourceBinding>();

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypeMapResourcePersistenceSettings>();
            RequireForUpdate<CombatPrototypeMapResourceRestoreState>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            RequireForUpdate<NetworkTime>();
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            if (source == _source) return;
            var settings = EntityManager.GetComponentData<CombatPrototypeMapResourcePersistenceSettings>(source);
            if (settings.Enabled == 0)
            {
                _source = source;
                Bindings = Array.Empty<CombatPrototypeMapResourceBinding>();
                SetPhase(source, CombatPrototypeMapResourceRestorePhase.Ready);
                return;
            }
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            if (!tick.IsValid) return;
            _source = source;
            Bindings = Array.Empty<CombatPrototypeMapResourceBinding>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var path = CombatPrototypeMapResourceSaveStore.GetSavePath(settings.SaveSlotId.ToString(), map.MapDefinitionId.ToString());
            var stage = "BindResources";
            try
            {
                var bindings = BindResources(source);
                stage = "LoadAndValidate";
                var data = CombatPrototypeMapResourceSaveStore.Load(settings, map, bindings, out var restored);
                var byPlacement = new Dictionary<int, CombatPrototypeMapResourceBinding>(bindings.Length);
                foreach (var binding in bindings) byPlacement.Add(binding.PlacementIndex, binding);
                // Prepare every buffer before applying the fully validated snapshot.
                foreach (var entry in data.Resources)
                {
                    var binding = byPlacement[entry.PlacementIndex];
                    if (binding.Kind == CombatPrototypeMapResourceSaveKind.Tree)
                        EntityManager.GetBuffer<CombatPrototypeMapTreeBlockingEvent>(binding.Entity).EnsureCapacity(1);
                    else if (binding.Kind == CombatPrototypeMapResourceSaveKind.Mine)
                        EntityManager.GetBuffer<CombatPrototypeMapMineBlockingEvent>(binding.Entity).EnsureCapacity(1);
                }
                if (!restored)
                {
                    stage = "SaveInitial";
                    CombatPrototypeMapResourceSaveStore.SavePrepared(data);
                }
                stage = "ApplySnapshot";
                var baselineTick = tick;
                baselineTick.Decrement();
                var time = SystemAPI.Time.ElapsedTime;
                foreach (var entry in data.Resources) Restore(byPlacement[entry.PlacementIndex], entry, source, time, baselineTick.SerializedData);
                Bindings = bindings;
                SetPhase(source, CombatPrototypeMapResourceRestorePhase.Ready);
                Debug.Log("[CombatPrototype.Map] Resource restore ready; map=" + map.MapDefinitionId +
                    ", slot=" + settings.SaveSlotId + ", restored=" + restored + ", resources=" + bindings.Length +
                    ", depleted=" + data.Resources.Length + ", path=" + path + ".");
            }
            catch (Exception exception)
            {
                SetPhase(source, CombatPrototypeMapResourceRestorePhase.Failed);
                Debug.LogError("[CombatPrototype.Map] Resource restore failed; stage=" + stage + ", map=" + map.MapDefinitionId +
                    ", slot=" + settings.SaveSlotId + ", path=" + path + ". " + exception);
            }
        }

        private CombatPrototypeMapResourceBinding[] BindResources(Entity source)
        {
            var entities = new Dictionary<int, Entity>();
            var kinds = new Dictionary<int, CombatPrototypeMapResourceSaveKind>();
            foreach (var (state, entity) in SystemAPI.Query<RefRO<CombatPrototypeMapGatherState>>().WithEntityAccess())
            { entities.Add(state.ValueRO.PlacementIndex, entity); kinds.Add(state.ValueRO.PlacementIndex, CombatPrototypeMapResourceSaveKind.Gather); }
            foreach (var (state, entity) in SystemAPI.Query<RefRO<CombatPrototypeMapTreeState>>().WithEntityAccess())
            { entities.Add(state.ValueRO.PlacementIndex, entity); kinds.Add(state.ValueRO.PlacementIndex, CombatPrototypeMapResourceSaveKind.Tree); }
            foreach (var (state, entity) in SystemAPI.Query<RefRO<CombatPrototypeMapMineState>>().WithEntityAccess())
            { entities.Add(state.ValueRO.PlacementIndex, entity); kinds.Add(state.ValueRO.PlacementIndex, CombatPrototypeMapResourceSaveKind.Mine); }
            var objects = EntityManager.GetBuffer<CombatPrototypeMapObject>(source, true);
            var placements = EntityManager.GetBuffer<CombatPrototypeMapDecoration>(source, true);
            var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source, true);
            var treeSettings = EntityManager.GetComponentData<CombatPrototypeMapTreeSettings>(source);
            var mineSettings = EntityManager.GetComponentData<CombatPrototypeMapMineSettings>(source);
            var obstacleIndices = new Dictionary<int, int>();
            for (var index = 0; index < obstacles.Length; index++) obstacleIndices.Add(obstacles[index].PlacementIndex, index);
            var bindings = new List<CombatPrototypeMapResourceBinding>(entities.Count);
            for (var index = 0; index < placements.Length; index++)
            {
                var definition = objects[placements[index].ObjectIndex];
                var kind = definition.Gatherable != 0 ? CombatPrototypeMapResourceSaveKind.Gather :
                    treeSettings.Enabled != 0 && definition.Harvestable != 0 ? CombatPrototypeMapResourceSaveKind.Tree :
                    mineSettings.Enabled != 0 && definition.Mineable != 0 ? CombatPrototypeMapResourceSaveKind.Mine : 0;
                if (kind == 0) continue;
                if (!entities.TryGetValue(index, out var entity) || kinds[index] != kind)
                    throw new InvalidOperationException("Required resource instance missing or kind mismatch; placement=" + index + ", objectId=" + definition.ObjectId);
                var binding = new CombatPrototypeMapResourceBinding
                {
                    Entity = entity, Kind = kind, PlacementIndex = index, ObjectId = definition.ObjectId.ToString(),
                    RegrowEnabled = definition.RegrowEnabled, RegrowSeconds = definition.RegrowSeconds, ObstacleIndex = -1
                };
                if (kind == CombatPrototypeMapResourceSaveKind.Gather)
                {
                    EntityManager.GetComponentData<CombatPrototypeMapGatherProgress>(entity);
                    var config = EntityManager.GetComponentData<CombatPrototypeMapGatherConfig>(entity);
                    if (config.ObjectId != definition.ObjectId) throw new InvalidOperationException("Gather identity mismatch; placement=" + index);
                }
                else
                {
                    if (!obstacleIndices.TryGetValue(index, out binding.ObstacleIndex))
                        throw new InvalidOperationException("Required resource obstacle missing; placement=" + index);
                    if (kind == CombatPrototypeMapResourceSaveKind.Tree)
                    {
                        EntityManager.GetComponentData<CombatPrototypeMapTreeProgress>(entity);
                        EntityManager.GetBuffer<CombatPrototypeMapTreeBlockingEvent>(entity, true);
                    }
                    else
                    {
                        EntityManager.GetComponentData<CombatPrototypeMapMineProgress>(entity);
                        EntityManager.GetBuffer<CombatPrototypeMapMineBlockingEvent>(entity, true);
                    }
                }
                bindings.Add(binding);
            }
            if (bindings.Count != entities.Count) throw new InvalidOperationException("Unexpected resource instances outside current layout.");
            return bindings.ToArray();
        }

        private void Restore(CombatPrototypeMapResourceBinding binding, CombatPrototypeMapResourceSaveEntry entry,
            Entity source, double time, uint baselineTick)
        {
            // A due tree/mine needs a positive deadline to enter the existing occupancy/regrowth path.
            var regrowAt = binding.RegrowEnabled != 0 ? Math.Max(time + entry.RemainingSeconds, double.Epsilon) : 0d;
            if (binding.Kind == CombatPrototypeMapResourceSaveKind.Gather)
            {
                EntityManager.SetComponentData(binding.Entity, new CombatPrototypeMapGatherProgress { RegrowAt = regrowAt });
                EntityManager.SetComponentData(binding.Entity, new CombatPrototypeMapGatherState
                { PlacementIndex = binding.PlacementIndex, Phase = CombatPrototypeMapGatherPhase.Depleted });
                return;
            }
            if (binding.Kind == CombatPrototypeMapResourceSaveKind.Tree)
            {
                EntityManager.SetComponentData(binding.Entity, new CombatPrototypeMapTreeProgress { RegrowAt = regrowAt });
                var history = EntityManager.GetBuffer<CombatPrototypeMapTreeBlockingEvent>(binding.Entity);
                history.Clear(); history.Add(new CombatPrototypeMapTreeBlockingEvent { TransitionTick = baselineTick, Disabled = 1 });
                EntityManager.SetComponentData(binding.Entity, new CombatPrototypeMapTreeState
                { PlacementIndex = binding.PlacementIndex, Phase = CombatPrototypeMapTreePhase.Felled, FelledTick = baselineTick });
            }
            else
            {
                EntityManager.SetComponentData(binding.Entity, new CombatPrototypeMapMineProgress { RegrowAt = regrowAt });
                var history = EntityManager.GetBuffer<CombatPrototypeMapMineBlockingEvent>(binding.Entity);
                history.Clear(); history.Add(new CombatPrototypeMapMineBlockingEvent { TransitionTick = baselineTick, Disabled = 1 });
                EntityManager.SetComponentData(binding.Entity, new CombatPrototypeMapMineState
                { PlacementIndex = binding.PlacementIndex, Phase = CombatPrototypeMapMinePhase.Depleted, MinedTick = baselineTick });
            }
            var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
            var obstacle = obstacles[binding.ObstacleIndex]; obstacle.Disabled = 1; obstacles[binding.ObstacleIndex] = obstacle;
        }

        private void SetPhase(Entity source, CombatPrototypeMapResourceRestorePhase phase) =>
            EntityManager.SetComponentData(source, new CombatPrototypeMapResourceRestoreState { Phase = phase });

        protected override void OnStopRunning() { _source = Entity.Null; Bindings = Array.Empty<CombatPrototypeMapResourceBinding>(); }
        protected override void OnDestroy() { Bindings = Array.Empty<CombatPrototypeMapResourceBinding>(); }
    }
}
