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
