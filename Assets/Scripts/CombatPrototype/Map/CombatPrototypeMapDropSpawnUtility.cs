using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapDropSpawnUtility
    {
        internal static Entity InstantiateRestored(EntityManager manager, in CombatPrototypeMapData map,
            in CombatPrototypeMapDropSettings settings, CombatPrototypeMapInventoryDropDefinition definition,
            CombatPrototypeMapDropSaveEntry entry, double time)
        {
            var entity = Entity.Null;
            var stage = "PrepareRestore";
            try
            {
                var position = new float3(entry.PositionX, entry.PositionY, entry.PositionZ);
                var expiresAt = entry.HasExpiry ? time + entry.RemainingLifetimeSeconds : 0;
                if (double.IsNaN(expiresAt) || double.IsInfinity(expiresAt))
                    throw new InvalidOperationException("Restored drop deadline must be finite.");
                stage = "InstantiateRestoredDrop";
                entity = manager.Instantiate(definition.Prefab);
                stage = "InitializeRestoredTransform";
                manager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, settings.VisualScale));
                stage = "InitializeRestoredProgress";
                manager.SetComponentData(entity, new CombatPrototypeMapDropProgress
                {
                    StartPosition = position, EndPosition = position, StartedAt = time, ExpiresAt = expiresAt
                });
                stage = "CommitRestoredDrop";
                manager.SetComponentData(entity, new CombatPrototypeMapDropState
                {
                    DropId = entry.DropId, ItemId = definition.ItemId, Quantity = entry.Quantity, Phase = CombatPrototypeMapDropPhase.Landed
                });
                return entity;
            }
            catch (Exception exception)
            {
                if (entity != Entity.Null && manager.Exists(entity))
                {
                    try { manager.DestroyEntity(entity); }
                    catch (Exception cleanup)
                    {
                        Debug.LogError("[CombatPrototype.Map] Drop restore partial cleanup failed; stage=RestoreCleanup, map=" +
                            map.MapDefinitionId + ", DropId=" + entry.DropId + ", itemId=" + entry.ItemId +
                            ", resource=" + definition.ResourceKey + ", entity=" + entity + ". " + cleanup);
                    }
                }
                throw new InvalidOperationException("Drop restore creation failed; stage=" + stage + ", DropId=" + entry.DropId +
                    ", itemId=" + entry.ItemId + ", resource=" + definition.ResourceKey + ".", exception);
            }
        }

        public static Entity Instantiate(EntityManager manager, in CombatPrototypeMapData map,
            in CombatPrototypeMapDropSettings settings, Entity prefab, FixedString64Bytes resource,
            FixedString64Bytes itemId, int quantity, int dropId, float3 start, double time,
            CombatPrototypeMapDropPhase phase = CombatPrototypeMapDropPhase.Airborne)
        {
            var entity = Entity.Null;
            var stage = "PrepareDrop";
            try
            {
                if (!math.all(math.isfinite(start)))
                    throw new InvalidOperationException("Drop start position must be finite.");
                var random = new Unity.Mathematics.Random(math.hash(new uint2(map.Seed, (uint)dropId)) | 1u);
                var angle = random.NextFloat(0f, 2f * math.PI);
                var radius = math.sqrt(random.NextFloat()) * settings.ScatterRadius;
                var end = new float3(start.x + math.cos(angle) * radius,
                    map.BaseHeight + settings.GroundOffset, start.z + math.sin(angle) * radius);
                stage = "Instantiate";
                entity = manager.Instantiate(prefab);
                stage = "InitializeTransform";
                manager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                    start, quaternion.identity, settings.VisualScale));
                stage = "InitializeProgress";
                manager.SetComponentData(entity, new CombatPrototypeMapDropProgress
                {
                    StartPosition = start, EndPosition = end, StartedAt = time,
                    ExpiresAt = settings.Lifetime > 0f ? time + settings.Lifetime : 0d
                });
                stage = "InitializeState";
                manager.SetComponentData(entity, new CombatPrototypeMapDropState
                {
                    DropId = dropId, ItemId = itemId, Quantity = quantity,
                    Phase = phase
                });
                return entity;
            }
            catch (Exception exception)
            {
                if (entity != Entity.Null && manager.Exists(entity))
                {
                    try { manager.DestroyEntity(entity); }
                    catch (Exception cleanup)
                    {
                        Debug.LogError("[CombatPrototype.Map] Drop partial cleanup failed; stage=SpawnCleanup, map=" +
                            map.MapDefinitionId + ", DropId=" + dropId + ", itemId=" + itemId +
                            ", resource=" + resource + ", entity=" + entity + ". " + cleanup);
                    }
                }
                throw new InvalidOperationException("Drop creation failed; stage=" + stage + ", DropId=" + dropId +
                    ", itemId=" + itemId + ", resource=" + resource + ", prefab=" + prefab + ".", exception);
            }
        }
    }
}
