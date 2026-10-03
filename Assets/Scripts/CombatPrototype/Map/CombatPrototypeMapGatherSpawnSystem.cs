using System;
using System.Collections.Generic;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
    [UpdateBefore(typeof(CombatPrototypeGoInGameServerSystem))]
    public partial class CombatPrototypeMapGatherSpawnSystem : SystemBase
    {
        private EntityQuery _mapQuery;
        private Entity _source;
        private readonly List<Entity> _owned = new List<Entity>();

        protected override void OnCreate()
        {
            _mapQuery = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapData>());
            RequireForUpdate(_mapQuery);
            RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        protected override void OnUpdate()
        {
            var source = _mapQuery.GetSingletonEntity();
            if (source == _source) return;
            ReleaseOwned();
            _source = source;
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            // Structural changes below must not retain handles into the map buffers.
            using var objects = EntityManager.GetBuffer<CombatPrototypeMapObject>(source, true).ToNativeArray(Allocator.Temp);
            using var placements = EntityManager.GetBuffer<CombatPrototypeMapDecoration>(source, true).ToNativeArray(Allocator.Temp);
            var valid = new bool[objects.Length];
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i].Gatherable == 0) continue;
                var prefab = objects[i].Prefab;
                valid[i] = EntityManager.HasComponent<Prefab>(prefab) &&
                    EntityManager.HasComponent<GhostType>(prefab) && EntityManager.HasComponent<LocalTransform>(prefab) &&
                    EntityManager.HasComponent<CombatPrototypeMapGatherState>(prefab) &&
                    EntityManager.HasComponent<CombatPrototypeMapGatherConfig>(prefab) &&
                    EntityManager.HasComponent<CombatPrototypeMapGatherProgress>(prefab);
                if (!valid[i])
                    Debug.LogError("[CombatPrototype.Map] Gather spawn prerequisites failed; map=" + map.MapDefinitionId +
                        ", objectId=" + objects[i].ObjectId + ", resource=" + objects[i].ResourceKey +
                        ", prefab=" + prefab + ", required Ghost components are missing.");
            }

            var requested = 0;
            var succeeded = 0;
            for (var i = 0; i < placements.Length; i++)
            {
                var placement = placements[i];
                var definition = objects[placement.ObjectIndex];
                if (definition.Gatherable == 0) continue;
                requested++;
                if (!valid[placement.ObjectIndex]) continue;
                var entity = Entity.Null;
                try
                {
                    entity = EntityManager.Instantiate(definition.Prefab);
                    EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                        placement.Position, Unity.Mathematics.quaternion.RotateY(placement.YawRadians)));
                    EntityManager.SetComponentData(entity, new CombatPrototypeMapGatherState
                    {
                        PlacementIndex = i, Phase = CombatPrototypeMapGatherPhase.Available
                    });
                    EntityManager.SetComponentData(entity, new CombatPrototypeMapGatherConfig
                    {
                        ObjectId = definition.ObjectId, InteractionDistance = definition.InteractionDistance,
                        GatherDuration = definition.GatherDuration, YieldItemName = definition.YieldItemName,
                        YieldQuantity = definition.YieldQuantity
                    });
                    EntityManager.SetComponentData(entity, default(CombatPrototypeMapGatherProgress));
                    _owned.Add(entity);
                    succeeded++;
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Gather spawn failed; map=" + map.MapDefinitionId +
                        ", placement=" + i + ", objectId=" + definition.ObjectId + ", resource=" +
                        definition.ResourceKey + ", prefab=" + definition.Prefab + ". " + exception);
                    DestroyOwned(entity, "SpawnCleanup", i.ToString());
                }
            }
            Debug.Log("[CombatPrototype.Map] Gather spawn batch complete; map=" + map.MapDefinitionId +
                ", requested=" + requested + ", spawned=" + succeeded + ", failed=" + (requested - succeeded) + ".");
        }

        private void DestroyOwned(Entity entity, string stage, string item)
        {
            try
            {
                if (entity != Entity.Null && EntityManager.Exists(entity)) EntityManager.DestroyEntity(entity);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Gather cleanup failed; stage=" + stage +
                    ", item=" + item + ", entity=" + entity + ". " + exception);
            }
        }

        private void ReleaseOwned()
        {
            foreach (var entity in _owned) DestroyOwned(entity, "MapCleanup", entity.ToString());
            _owned.Clear();
            _source = Entity.Null;
        }

        protected override void OnStopRunning() { ReleaseOwned(); }
        protected override void OnDestroy() { ReleaseOwned(); }
    }
}
