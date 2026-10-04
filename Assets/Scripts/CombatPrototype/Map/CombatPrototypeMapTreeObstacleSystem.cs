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
            foreach (var (tree, treeEntity) in SystemAPI.Query<RefRO<CombatPrototypeMapTreeState>>().WithEntityAccess())
            {
                if (!_indices.TryGetValue(tree.ValueRO.PlacementIndex, out var index))
                {
                    Debug.LogError("[CombatPrototype.Map] Tree obstacle update failed; stage=FindObstacle, placement=" +
                        tree.ValueRO.PlacementIndex + ", mapSource=" + source + ".");
                    continue;
                }
                if (!EntityManager.HasBuffer<CombatPrototypeMapTreeBlockingEvent>(treeEntity))
                {
                    Debug.LogError("[CombatPrototype.Map] Tree obstacle update failed; stage=ReadHistory, placement=" +
                        tree.ValueRO.PlacementIndex + ", mapSource=" + source + ", reason=MissingBlockingEvents.");
                    continue;
                }
                var history = EntityManager.GetBuffer<CombatPrototypeMapTreeBlockingEvent>(treeEntity, true);
                // Both transitions commit after movement, taking effect from the following simulated tick.
                for (var eventIndex = history.Length - 1; eventIndex >= 0; eventIndex--)
                {
                    var transition = history[eventIndex];
                    var transitionTick = new NetworkTick { SerializedData = transition.TransitionTick };
                    if (!transitionTick.IsValid || transition.Disabled > 1)
                    {
                        Debug.LogError("[CombatPrototype.Map] Tree obstacle update failed; stage=ReadTransition, placement=" +
                            tree.ValueRO.PlacementIndex + ", mapSource=" + source + ", eventIndex=" + eventIndex + ".");
                        break;
                    }
                    if (!tick.IsNewerThan(transitionTick)) continue;
                    var obstacle = obstacles[index];
                    obstacle.Disabled = transition.Disabled;
                    obstacles[index] = obstacle;
                    break;
                }
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
