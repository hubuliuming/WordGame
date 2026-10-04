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
    public partial class CombatPrototypeMapTreeRegrowSystem : SystemBase
    {
        private struct Occupant
        {
            public float2 Position;
            public float Radius;
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
            var settings = EntityManager.GetComponentData<CombatPrototypeMapTreeSettings>(source);
            if (settings.Enabled == 0 || settings.RegrowEnabled == 0) return;
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var time = SystemAPI.Time.ElapsedTime;
            using var trees = _trees.ToEntityArray(Allocator.Temp);
            var hasDueTree = false;
            foreach (var tree in trees)
                if (IsDue(tree, time)) { hasDueTree = true; break; }
            if (!hasDueTree) return;

            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            if (!tick.IsValid)
            {
                Debug.LogError("[CombatPrototype.Map] Tree regrowth prerequisites failed; stage=ReadServerTick, map=" +
                    map.MapDefinitionId + ", resource=" + settings.ResourceKey + ", reason=InvalidTick.");
                return;
            }
            if (_source != source)
            {
                _source = source;
                _obstacles.Clear();
                var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source, true);
                var objects = EntityManager.GetBuffer<CombatPrototypeMapObject>(source, true);
                for (var i = 0; i < obstacles.Length; i++)
                    if (objects[obstacles[i].ObjectIndex].Harvestable != 0)
                        _obstacles.Add(obstacles[i].PlacementIndex, i);
            }
            using var occupants = new NativeList<Occupant>(Allocator.Temp);
            if (!CollectOccupants(in map, occupants)) return;
            foreach (var tree in trees)
            {
                if (!IsDue(tree, time)) continue;
                var state = EntityManager.GetComponentData<CombatPrototypeMapTreeState>(tree);
                var stage = "ReadObstacle";
                try
                {
                    var index = _obstacles[state.PlacementIndex];
                    var obstacle = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source, true)[index];
                    stage = "CheckOccupancy";
                    if (IsOccupied(obstacle, map.CollisionSkin, occupants)) continue;
                    stage = "RestoreStanding";
                    Restore(source, tree, state, index, obstacle, tick, map.MapDefinitionId);
                    Debug.Log("[CombatPrototype.Map] Tree regrown; map=" + map.MapDefinitionId +
                        ", placement=" + state.PlacementIndex + ", tree=" + tree + ", interval=" +
                        settings.RegrowSeconds + ", tick=" + tick.SerializedData + ".");
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Tree regrowth failed; stage=" + stage + ", map=" +
                        map.MapDefinitionId + ", placement=" + state.PlacementIndex + ", tree=" + tree +
                        ", objectId=" + settings.ObjectId + ", resource=" + settings.ResourceKey + ". " + exception);
                }
            }
        }

        private bool IsDue(Entity tree, double time)
        {
            if (EntityManager.GetComponentData<CombatPrototypeMapTreeState>(tree).Phase != CombatPrototypeMapTreePhase.Felled)
                return false;
            var deadline = EntityManager.GetComponentData<CombatPrototypeMapTreeProgress>(tree).RegrowAt;
            return deadline > 0d && time >= deadline;
        }

        private bool CollectOccupants(in CombatPrototypeMapData map, NativeList<Occupant> occupants)
        {
            foreach (var (health, transform, entity) in SystemAPI.Query<RefRO<CombatPrototypePlayerHealth>,
                         RefRO<LocalTransform>>().WithAll<CombatPrototypePlayerNetCode>().WithEntityAccess())
            {
                if (health.ValueRO.IsDead != 0) continue;
                var position = transform.ValueRO.Position.xz;
                if (!math.all(math.isfinite(position)))
                {
                    Debug.LogError("[CombatPrototype.Map] Tree regrowth prerequisites failed; stage=CollectOccupants, map=" +
                        map.MapDefinitionId + ", player=" + entity + ", reason=NonfinitePosition.");
                    return false;
                }
                occupants.Add(new Occupant { Position = position, Radius = map.PlayerRadius });
            }
            foreach (var (health, transform, entity) in SystemAPI.Query<RefRO<CombatPrototypeEnemyState>,
                         RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (health.ValueRO.IsDead != 0) continue;
                var position = transform.ValueRO.Position.xz;
                if (!math.all(math.isfinite(position)))
                {
                    Debug.LogError("[CombatPrototype.Map] Tree regrowth prerequisites failed; stage=CollectOccupants, map=" +
                        map.MapDefinitionId + ", enemy=" + entity + ", reason=NonfinitePosition.");
                    return false;
                }
                occupants.Add(new Occupant { Position = position, Radius = map.EnemyRadius });
            }
            return true;
        }

        private static bool IsOccupied(CombatPrototypeMapObstacle obstacle, float skin, NativeList<Occupant> occupants)
        {
            foreach (var occupant in occupants)
            {
                var radius = obstacle.Radius + occupant.Radius + skin;
                if (math.distancesq(obstacle.Position, occupant.Position) <= radius * radius) return true;
            }
            return false;
        }

        private void Restore(Entity source, Entity tree, CombatPrototypeMapTreeState state, int index,
            CombatPrototypeMapObstacle obstacle, NetworkTick tick, FixedString64Bytes mapId)
        {
            var progress = EntityManager.GetComponentData<CombatPrototypeMapTreeProgress>(tree);
            var history = EntityManager.GetBuffer<CombatPrototypeMapTreeBlockingEvent>(tree);
            var historyLength = history.Length;
            // Prepare the only allocation before changing authoritative state or the blocking timeline.
            history.EnsureCapacity(checked(historyLength + 1));
            var original = state;
            try
            {
                history.Add(new CombatPrototypeMapTreeBlockingEvent { TransitionTick = tick.SerializedData, Disabled = 0 });
                EntityManager.SetComponentData(tree, default(CombatPrototypeMapTreeProgress));
                state.Phase = CombatPrototypeMapTreePhase.Standing;
                state.CollectorNetworkId = 0;
                state.FelledTick = 0;
                EntityManager.SetComponentData(tree, state);
                var restored = obstacle;
                restored.Disabled = 0;
                var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                obstacles[index] = restored;
            }
            catch
            {
                try
                {
                    EntityManager.GetBuffer<CombatPrototypeMapTreeBlockingEvent>(tree).ResizeUninitialized(historyLength);
                    EntityManager.SetComponentData(tree, progress);
                    EntityManager.SetComponentData(tree, original);
                    var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                    obstacles[index] = obstacle;
                }
                catch (Exception rollback)
                {
                    Debug.LogError("[CombatPrototype.Map] Tree regrowth rollback failed; stage=RollbackRestore, map=" +
                        mapId + ", placement=" + original.PlacementIndex + ", tree=" + tree + ". " + rollback);
                }
                throw;
            }
        }

        protected override void OnStopRunning()
        {
            _source = Entity.Null;
            _obstacles.Clear();
        }
    }
}
