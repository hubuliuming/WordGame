using System;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMapGatherSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapGatherRegrowSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var map = SystemAPI.GetSingleton<CombatPrototypeMapData>();
            var time = SystemAPI.Time.ElapsedTime;
            foreach (var (state, progress, config, point) in SystemAPI.Query<
                         RefRW<CombatPrototypeMapGatherState>, RefRW<CombatPrototypeMapGatherProgress>,
                         RefRO<CombatPrototypeMapGatherConfig>>().WithEntityAccess())
            {
                if (state.ValueRO.Phase != CombatPrototypeMapGatherPhase.Depleted ||
                    config.ValueRO.RegrowEnabled == 0 || time < progress.ValueRO.RegrowAt)
                    continue;
                var placement = state.ValueRO.PlacementIndex;
                try
                {
                    progress.ValueRW = default;
                    state.ValueRW.CollectorNetworkId = 0;
                    state.ValueRW.Phase = CombatPrototypeMapGatherPhase.Available;
                    Debug.Log("[CombatPrototype.Map] Gather regrown; map=" + map.MapDefinitionId +
                        ", placement=" + placement + ", objectId=" + config.ValueRO.ObjectId +
                        ", point=" + point + ", interval=" + config.ValueRO.RegrowSeconds + ".");
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Gather regrowth failed; map=" + map.MapDefinitionId +
                        ", placement=" + placement + ", objectId=" + config.ValueRO.ObjectId +
                        ", point=" + point + ", stage=RestoreAvailable. " + exception);
                }
            }
        }
    }
}
