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
    [UpdateAfter(typeof(CombatPrototypeMapMineHarvestSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapMineRegrowSystem : SystemBase
    {
        private struct Occupant
        {
            public float2 Position;
            public float Radius;
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
            var settings = EntityManager.GetComponentData<CombatPrototypeMapMineSettings>(source);
            if (settings.Enabled == 0 || settings.RegrowEnabled == 0) return;
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var time = SystemAPI.Time.ElapsedTime;
            using var mines = _mines.ToEntityArray(Allocator.Temp);
            var hasDueMine = false;
            foreach (var mine in mines)
                if (IsDue(mine, time)) { hasDueMine = true; break; }
            if (!hasDueMine) return;

            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            if (!tick.IsValid)
            {
                Debug.LogError("[CombatPrototype.Map] Mine regrowth prerequisites failed; stage=ReadServerTick, map=" +
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
                    if (objects[obstacles[i].ObjectIndex].Mineable != 0)
                        _obstacles.Add(obstacles[i].PlacementIndex, i);
            }
            using var occupants = new NativeList<Occupant>(Allocator.Temp);
            if (!CollectOccupants(in map, occupants)) return;
            foreach (var mine in mines)
            {
                if (!IsDue(mine, time)) continue;
                var state = EntityManager.GetComponentData<CombatPrototypeMapMineState>(mine);
                var stage = "ReadObstacle";
                try
                {
                    var index = _obstacles[state.PlacementIndex];
                    var obstacle = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source, true)[index];
                    stage = "CheckOccupancy";
                    if (IsOccupied(obstacle, map.CollisionSkin, occupants)) continue;
                    stage = "RestoreAvailable";
                    Restore(source, mine, state, index, obstacle, tick, map.MapDefinitionId);
                    Debug.Log("[CombatPrototype.Map] Mine regrown; map=" + map.MapDefinitionId +
                        ", placement=" + state.PlacementIndex + ", mine=" + mine + ", interval=" +
                        settings.RegrowSeconds + ", tick=" + tick.SerializedData + ".");
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Mine regrowth failed; stage=" + stage + ", map=" +
                        map.MapDefinitionId + ", placement=" + state.PlacementIndex + ", mine=" + mine +
                        ", objectId=" + settings.ObjectId + ", resource=" + settings.ResourceKey + ". " + exception);
                }
            }
        }

        private bool IsDue(Entity mine, double time)
        {
            if (EntityManager.GetComponentData<CombatPrototypeMapMineState>(mine).Phase != CombatPrototypeMapMinePhase.Depleted)
                return false;
            var deadline = EntityManager.GetComponentData<CombatPrototypeMapMineProgress>(mine).RegrowAt;
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
                    Debug.LogError("[CombatPrototype.Map] Mine regrowth prerequisites failed; stage=CollectOccupants, map=" +
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
                    Debug.LogError("[CombatPrototype.Map] Mine regrowth prerequisites failed; stage=CollectOccupants, map=" +
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

        private void Restore(Entity source, Entity mine, CombatPrototypeMapMineState state, int index,
            CombatPrototypeMapObstacle obstacle, NetworkTick tick, FixedString64Bytes mapId)
        {
            var progress = EntityManager.GetComponentData<CombatPrototypeMapMineProgress>(mine);
            var history = EntityManager.GetBuffer<CombatPrototypeMapMineBlockingEvent>(mine);
            var historyLength = history.Length;
            // Prepare the only allocation before changing authoritative state or the blocking timeline.
            history.EnsureCapacity(checked(historyLength + 1));
            var original = state;
            try
            {
                history.Add(new CombatPrototypeMapMineBlockingEvent { TransitionTick = tick.SerializedData, Disabled = 0 });
                EntityManager.SetComponentData(mine, default(CombatPrototypeMapMineProgress));
                state.Phase = CombatPrototypeMapMinePhase.Available;
                state.CollectorNetworkId = 0;
                state.MinedTick = 0;
                EntityManager.SetComponentData(mine, state);
                var restored = obstacle;
                restored.Disabled = 0;
                var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                obstacles[index] = restored;
            }
            catch
            {
                try
                {
                    EntityManager.GetBuffer<CombatPrototypeMapMineBlockingEvent>(mine).ResizeUninitialized(historyLength);
                    EntityManager.SetComponentData(mine, progress);
                    EntityManager.SetComponentData(mine, original);
                    var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
                    obstacles[index] = obstacle;
                }
                catch (Exception rollback)
                {
                    Debug.LogError("[CombatPrototype.Map] Mine regrowth rollback failed; stage=RollbackRestore, map=" +
                        mapId + ", placement=" + original.PlacementIndex + ", mine=" + mine + ". " + rollback);
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
