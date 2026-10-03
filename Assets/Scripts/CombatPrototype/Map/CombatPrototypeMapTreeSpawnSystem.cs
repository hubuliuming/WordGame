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
    public partial class CombatPrototypeMapTreeSpawnSystem : SystemBase
    {
        private EntityQuery _maps;
        private Entity _source;
        private readonly List<Entity> _owned = new List<Entity>();

        protected override void OnCreate()
        {
            _maps = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapData>(),
                ComponentType.ReadOnly<CombatPrototypeMapTreeSettings>());
            RequireForUpdate(_maps);
            RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = _maps.GetSingletonEntity();
            if (source == _source) return;
            ReleaseOwned();
            _source = source;
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapTreeSettings>(source);
            if (settings.Enabled == 0) return;
            // Keep copies while Instantiate changes archetypes and invalidates buffer handles.
            using var objects = EntityManager.GetBuffer<CombatPrototypeMapObject>(source, true).ToNativeArray(Allocator.Temp);
            using var placements = EntityManager.GetBuffer<CombatPrototypeMapDecoration>(source, true).ToNativeArray(Allocator.Temp);
            var valid = new bool[objects.Length];
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i].Harvestable == 0) continue;
                var prefab = objects[i].Prefab;
                valid[i] = EntityManager.HasComponent<Prefab>(prefab) && EntityManager.HasComponent<GhostType>(prefab) &&
                    EntityManager.HasComponent<LocalTransform>(prefab) &&
                    EntityManager.HasComponent<CombatPrototypeMapTreeState>(prefab) &&
                    EntityManager.HasComponent<CombatPrototypeMapTreeProgress>(prefab);
                if (!valid[i])
                    Debug.LogError("[CombatPrototype.Map] Tree spawn prerequisites failed; stage=ValidatePrefab, map=" +
                        map.MapDefinitionId + ", objectId=" + objects[i].ObjectId + ", resource=" +
                        objects[i].ResourceKey + ", prefab=" + prefab + ".");
            }
            var requested = 0;
            var succeeded = 0;
            for (var i = 0; i < placements.Length; i++)
            {
                var placement = placements[i];
                var definition = objects[placement.ObjectIndex];
                if (definition.Harvestable == 0) continue;
                requested++;
                if (!valid[placement.ObjectIndex]) continue;
                var entity = Entity.Null;
                var stage = "Instantiate";
                try
                {
                    entity = EntityManager.Instantiate(definition.Prefab);
                    stage = "Initialize";
                    EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(
                        placement.Position, Unity.Mathematics.quaternion.RotateY(placement.YawRadians)));
                    EntityManager.SetComponentData(entity, new CombatPrototypeMapTreeState
                    {
                        PlacementIndex = i, Phase = CombatPrototypeMapTreePhase.Standing
                    });
                    EntityManager.SetComponentData(entity, default(CombatPrototypeMapTreeProgress));
                    _owned.Add(entity);
                    succeeded++;
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Tree spawn failed; stage=" + stage + ", map=" +
                        map.MapDefinitionId + ", placement=" + i + ", objectId=" + definition.ObjectId +
                        ", resource=" + definition.ResourceKey + ", prefab=" + definition.Prefab + ". " + exception);
                    DestroyOwned(entity, "SpawnCleanup");
                }
            }
            Debug.Log("[CombatPrototype.Map] Tree spawn batch complete; map=" + map.MapDefinitionId +
                ", requested=" + requested + ", spawned=" + succeeded + ", failed=" + (requested - succeeded) + ".");
        }

        private void DestroyOwned(Entity entity, string stage)
        {
            try
            {
                if (entity != Entity.Null && EntityManager.Exists(entity)) EntityManager.DestroyEntity(entity);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Tree cleanup failed; stage=" + stage +
                    ", entity=" + entity + ". " + exception);
            }
        }

        private void ReleaseOwned()
        {
            foreach (var entity in _owned) DestroyOwned(entity, "MapCleanup");
            _owned.Clear();
            _source = Entity.Null;
        }

        protected override void OnStopRunning() { ReleaseOwned(); }
        protected override void OnDestroy() { ReleaseOwned(); }
    }
}
