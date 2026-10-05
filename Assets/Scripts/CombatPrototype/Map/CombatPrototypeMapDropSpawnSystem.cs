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
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeRewardSystem))]
    [UpdateBefore(typeof(CombatPrototypeEnemyAttackSystem))]
    public partial class CombatPrototypeMapDropSpawnSystem : SystemBase
    {
        private EntityQuery _maps;
        private EntityQuery _enemies;
        private Entity _source;
        private int _nextId;
        private bool _prefabValid;
        private readonly HashSet<Entity> _attemptedDeaths = new HashSet<Entity>();
        private readonly List<Entity> _owned = new List<Entity>();

        protected override void OnCreate()
        {
            _maps = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapData>(),
                ComponentType.ReadOnly<CombatPrototypeMapDropSettings>());
            _enemies = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeEnemyState>(),
                ComponentType.ReadOnly<LocalTransform>());
            RequireForUpdate(_maps);
            RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = _maps.GetSingletonEntity();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapDropSettings>(source);
            EnsureSource(source);
            if (EntityManager.GetComponentData<CombatPrototypeMapResourceRestoreState>(source).Phase !=
                CombatPrototypeMapResourceRestorePhase.Ready) return;
            if (settings.Enabled == 0 || !_prefabValid) return;

            // Copy the roster before Instantiate invalidates component handles.
            using var enemies = _enemies.ToEntityArray(Allocator.Temp);
            foreach (var enemy in enemies)
            {
                if (EntityManager.GetComponentData<CombatPrototypeEnemyState>(enemy).IsDead == 0 ||
                    !_attemptedDeaths.Add(enemy)) continue;
                var dropId = 0;
                try
                {
                    SpawnOwnedDrop(source, settings.Prefab, settings.ResourceKey, settings.ItemId, settings.Quantity,
                        EntityManager.GetComponentData<LocalTransform>(enemy).Position, out dropId);
                    Debug.Log("[CombatPrototype.Map] Drop spawned; map=" + map.MapDefinitionId +
                        ", DropId=" + dropId + ", enemy=" + enemy + ", itemId=" + settings.ItemId +
                        ", quantity=" + settings.Quantity + ", resource=" + settings.ResourceKey + ".");
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Drop spawn failed; map=" + map.MapDefinitionId +
                        ", DropId=" + dropId + ", enemy=" + enemy + ", itemId=" + settings.ItemId +
                        ", resource=" + settings.ResourceKey + ", stage=SpawnOwnedDrop. " + exception);
                }
            }
        }

        private void EnsureSource(Entity source)
        {
            if (source == _source) return;
            ReleaseOwned();
            _source = source;
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapDropSettings>(source);
            var prefab = settings.Prefab;
            _prefabValid = EntityManager.HasComponent<Prefab>(prefab) &&
                EntityManager.HasComponent<GhostType>(prefab) && EntityManager.HasComponent<LocalTransform>(prefab) &&
                EntityManager.HasComponent<CombatPrototypeMapDropState>(prefab) &&
                EntityManager.HasComponent<CombatPrototypeMapDropProgress>(prefab);
            if (!_prefabValid)
                Debug.LogError("[CombatPrototype.Map] Drop spawn prerequisites failed; stage=ValidatePrefab, map=" +
                    map.MapDefinitionId + ", itemId=" + settings.ItemId + ", resource=" + settings.ResourceKey +
                    ", prefab=" + prefab + ", required Ghost components are missing.");
        }

        internal void BeginDropRestore(Entity source, int lastDropId)
        {
            EnsureSource(source);
            if (_owned.Count != 0 || _nextId != 0)
                throw new InvalidOperationException("Drop restore requires an unused owner; map source=" + source);
            _nextId = lastDropId;
        }

        internal int GetLastAllocatedDropId(Entity source)
        {
            RequireSource(source);
            return _nextId;
        }

        internal IReadOnlyList<Entity> GetOwnedDrops(Entity source)
        {
            RequireSource(source);
            return _owned;
        }

        private void RequireSource(Entity source)
        {
            if (source != _source) throw new InvalidOperationException("Drop owner source mismatch; source=" + source);
        }

        internal Entity RestoreOwnedDrop(Entity source, CombatPrototypeMapInventoryDropDefinition definition,
            CombatPrototypeMapDropSaveEntry entry, double time)
        {
            RequireSource(source);
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapDropSettings>(source);
            var entity = CombatPrototypeMapDropSpawnUtility.InstantiateRestored(EntityManager, map, settings, definition, entry, time);
            try { _owned.Add(entity); }
            catch
            {
                DestroyOwned(entity, "RestoreOwnershipCleanup");
                throw;
            }
            return entity;
        }

        public Entity SpawnOwnedDrop(Entity source, Entity prefab, FixedString64Bytes resource,
            FixedString64Bytes itemId, int quantity, Unity.Mathematics.float3 start, out int dropId,
            CombatPrototypeMapDropPhase phase = CombatPrototypeMapDropPhase.Airborne)
        {
            dropId = 0;
            if (source != _source)
                throw new InvalidOperationException("Drop owner is not initialized for map source=" + source + ".");
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapDropSettings>(source);
            dropId = checked(_nextId + 1);
            _nextId = dropId;
            var entity = CombatPrototypeMapDropSpawnUtility.Instantiate(EntityManager, map, settings,
                prefab, resource, itemId, quantity, dropId, start, World.Time.ElapsedTime, phase);
            try { _owned.Add(entity); }
            catch
            {
                DestroyOwned(entity, "OwnershipCleanup");
                throw;
            }
            return entity;
        }

        public void ReleaseDrop(Entity entity, string stage = "TreeRollback")
        {
            if (DestroyOwned(entity, stage)) _owned.Remove(entity);
        }

        private bool DestroyOwned(Entity entity, string stage)
        {
            try
            {
                if (entity == Entity.Null || !EntityManager.Exists(entity)) return true;
                // Pending ECB destruction must remain the sole destroy operation until playback.
                if (EntityManager.HasComponent<CombatPrototypeMapDropProgress>(entity) &&
                    EntityManager.GetComponentData<CombatPrototypeMapDropProgress>(entity).CleanupQueued != 0) return true;
                EntityManager.DestroyEntity(entity);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Drop cleanup failed; stage=" + stage +
                    ", entity=" + entity + ". " + exception);
                return false;
            }
        }

        private void ReleaseOwned()
        {
            foreach (var entity in _owned) DestroyOwned(entity, "MapCleanup");
            _owned.Clear();
            _attemptedDeaths.Clear();
            _source = Entity.Null;
            _nextId = 0;
            _prefabValid = false;
        }

        protected override void OnStopRunning() { ReleaseOwned(); }
        protected override void OnDestroy() { ReleaseOwned(); }
    }
}
