using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMapDropPickupSystem))]
    [UpdateBefore(typeof(CombatPrototypeMapGatherSystem))]
    public partial class CombatPrototypeMapDropCleanupSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypeMapDropSettings>();
        }

        protected override void OnUpdate()
        {
            var map = SystemAPI.GetSingleton<CombatPrototypeMapData>();
            var time = SystemAPI.Time.ElapsedTime;
            var commands = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);
            foreach (var (drop, progress, entity) in SystemAPI.Query<
                         RefRW<CombatPrototypeMapDropState>, RefRW<CombatPrototypeMapDropProgress>>().WithEntityAccess())
            {
                if (progress.ValueRO.CleanupQueued != 0) continue;
                var expired = progress.ValueRO.ExpiresAt > 0d && time >= progress.ValueRO.ExpiresAt;
                if (drop.ValueRO.Phase != CombatPrototypeMapDropPhase.Consumed && !expired) continue;
                try
                {
                    commands.DestroyEntity(entity);
                    // Prevent repeated destruction commands during multiple simulation ticks before ECB playback.
                    progress.ValueRW.CleanupQueued = 1;
                    drop.ValueRW.Phase = CombatPrototypeMapDropPhase.Consumed;
                    Debug.Log("[CombatPrototype.Map] Drop cleanup queued; map=" + map.MapDefinitionId +
                        ", DropId=" + drop.ValueRO.DropId + ", itemId=" + drop.ValueRO.ItemId +
                        ", reason=" + (expired ? "Expired" : "PickedUp") + ".");
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Drop cleanup failed; stage=QueueDestroy, map=" +
                        map.MapDefinitionId + ", DropId=" + drop.ValueRO.DropId + ", itemId=" + drop.ValueRO.ItemId +
                        ", entity=" + entity + ". " + exception);
                }
            }
        }
    }
}
