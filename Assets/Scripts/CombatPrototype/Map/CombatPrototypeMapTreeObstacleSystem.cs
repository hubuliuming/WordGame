using System.Collections.Generic;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(CombatPrototypePlayerMovementSystem))]
    [UpdateBefore(typeof(CombatPrototypeEnemyMovementSystem))]
    public partial class CombatPrototypeMapTreeObstacleSystem : SystemBase
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
                    if (objects[obstacles[i].ObjectIndex].Harvestable != 0)
                        _indices.Add(obstacles[i].PlacementIndex, i);
            }
            // Restore the baked state before applying this prediction tick, including rollback.
            foreach (var index in _indices.Values)
            {
                var obstacle = obstacles[index];
                obstacle.Disabled = 0;
                obstacles[index] = obstacle;
            }
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            if (!tick.IsValid) return;
            foreach (var tree in SystemAPI.Query<RefRO<CombatPrototypeMapTreeState>>())
            {
                if (tree.ValueRO.Phase != CombatPrototypeMapTreePhase.Felled) continue;
                if (!_indices.TryGetValue(tree.ValueRO.PlacementIndex, out var index))
                {
                    Debug.LogError("[CombatPrototype.Map] Tree obstacle update failed; stage=FindObstacle, placement=" +
                        tree.ValueRO.PlacementIndex + ", mapSource=" + source + ".");
                    continue;
                }
                var felledTick = new NetworkTick { SerializedData = tree.ValueRO.FelledTick };
                if (!felledTick.IsValid)
                {
                    Debug.LogError("[CombatPrototype.Map] Tree obstacle update failed; stage=ReadFelledTick, placement=" +
                        tree.ValueRO.PlacementIndex + ", mapSource=" + source + ".");
                    continue;
                }
                // The cut commits after movement; remove blocking from the following simulated tick.
                if (!tick.IsNewerThan(felledTick)) continue;
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
