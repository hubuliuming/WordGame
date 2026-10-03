using System;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMapDropSpawnSystem))]
    [UpdateAfter(typeof(CombatPrototypePlayerDamageSystem))]
    [UpdateBefore(typeof(CombatPrototypeMapDropPickupSystem))]
    public partial class CombatPrototypeMapDropMotionSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypeMapDropSettings>();
        }

        protected override void OnUpdate()
        {
            var map = SystemAPI.GetSingleton<CombatPrototypeMapData>();
            var settings = SystemAPI.GetSingleton<CombatPrototypeMapDropSettings>();
            var time = SystemAPI.Time.ElapsedTime;
            foreach (var (drop, progress, transform, entity) in SystemAPI.Query<
                         RefRW<CombatPrototypeMapDropState>, RefRO<CombatPrototypeMapDropProgress>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (drop.ValueRO.Phase != CombatPrototypeMapDropPhase.Airborne) continue;
                try
                {
                    var t = (float)math.clamp((time - progress.ValueRO.StartedAt) / settings.FlightDuration, 0d, 1d);
                    var position = math.lerp(progress.ValueRO.StartPosition, progress.ValueRO.EndPosition, t);
                    position.y += 4f * t * (1f - t) * settings.ArcHeight;
                    transform.ValueRW.Position = position;
                    if (t < 1f) continue;
                    drop.ValueRW.Phase = CombatPrototypeMapDropPhase.Landed;
                    Debug.Log("[CombatPrototype.Map] Drop landed; map=" + map.MapDefinitionId +
                        ", DropId=" + drop.ValueRO.DropId + ", itemId=" + drop.ValueRO.ItemId + ".");
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Drop motion failed; stage=Flight, map=" + map.MapDefinitionId +
                        ", DropId=" + drop.ValueRO.DropId + ", itemId=" + drop.ValueRO.ItemId +
                        ", entity=" + entity + ". " + exception);
                }
            }
        }
    }
}
