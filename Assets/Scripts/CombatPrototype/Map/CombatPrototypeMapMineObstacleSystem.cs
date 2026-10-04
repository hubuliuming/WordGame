using System.Collections.Generic;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(CombatPrototypeMapTreeObstacleSystem))]
    public partial class CombatPrototypeMapMineObstacleSystem : SystemBase
    {
        private Entity _source;
        private readonly Dictionary<int, int> _indices = new Dictionary<int, int>();

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            RequireForUpdate<NetworkTime>();
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(source);
            if (source != _source)
            {
                _indices.Clear();
                _source = source;
                var objects = EntityManager.GetBuffer<CombatPrototypeMapObject>(source, true);
                for (var i = 0; i < obstacles.Length; i++)
                    if (objects[obstacles[i].ObjectIndex].Mineable != 0)
                        _indices.Add(obstacles[i].PlacementIndex, i);
            }
            // Reconstruct the obstacle for each prediction tick, including rollback before depletion.
            foreach (var index in _indices.Values)
            {
                var obstacle = obstacles[index];
                obstacle.Disabled = 0;
                obstacles[index] = obstacle;
            }
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            if (!tick.IsValid) return;
            foreach (var mine in SystemAPI.Query<RefRO<CombatPrototypeMapMineState>>())
            {
                var value = mine.ValueRO;
                if (!_indices.TryGetValue(value.PlacementIndex, out var index))
                {
                    Debug.LogError("[CombatPrototype.Map] Mine obstacle update failed; stage=FindObstacle, placement=" +
                        value.PlacementIndex + ", mapSource=" + source + ".");
                    continue;
                }
                if (value.Phase != CombatPrototypeMapMinePhase.Depleted) continue;
                var minedTick = new NetworkTick { SerializedData = value.MinedTick };
                if (!minedTick.IsValid)
                {
                    Debug.LogError("[CombatPrototype.Map] Mine obstacle update failed; stage=ReadMinedTick, placement=" +
                        value.PlacementIndex + ", mapSource=" + source + ".");
                    continue;
                }
                // Depletion commits after movement and applies from the following simulated tick.
                if (!tick.IsNewerThan(minedTick)) continue;
                var obstacle = obstacles[index];
                obstacle.Disabled = 1;
                obstacles[index] = obstacle;
            }
        }

        protected override void OnStopRunning()
        {
            if (EntityManager.Exists(_source) && EntityManager.HasBuffer<CombatPrototypeMapObstacle>(_source))
            {
                var obstacles = EntityManager.GetBuffer<CombatPrototypeMapObstacle>(_source);
                foreach (var index in _indices.Values)
                {
                    var obstacle = obstacles[index];
                    obstacle.Disabled = 0;
                    obstacles[index] = obstacle;
                }
            }
            _source = Entity.Null;
            _indices.Clear();
        }
    }
}
