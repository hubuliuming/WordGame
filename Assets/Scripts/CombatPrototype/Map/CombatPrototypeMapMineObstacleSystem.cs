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
            // Restore the baked state before applying this prediction tick, including rollback.
            foreach (var index in _indices.Values)
            {
                var obstacle = obstacles[index];
                obstacle.Disabled = 0;
                obstacles[index] = obstacle;
            }
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            if (!tick.IsValid) return;
            foreach (var (mine, mineEntity) in SystemAPI.Query<RefRO<CombatPrototypeMapMineState>>().WithEntityAccess())
            {
                var value = mine.ValueRO;
                if (!_indices.TryGetValue(value.PlacementIndex, out var index))
                {
                    Debug.LogError("[CombatPrototype.Map] Mine obstacle update failed; stage=FindObstacle, placement=" +
                        value.PlacementIndex + ", mapSource=" + source + ".");
                    continue;
                }
                if (!EntityManager.HasBuffer<CombatPrototypeMapMineBlockingEvent>(mineEntity))
                {
                    Debug.LogError("[CombatPrototype.Map] Mine obstacle update failed; stage=ReadHistory, placement=" +
                        value.PlacementIndex + ", mapSource=" + source + ", reason=MissingBlockingEvents.");
                    continue;
                }
                var history = EntityManager.GetBuffer<CombatPrototypeMapMineBlockingEvent>(mineEntity, true);
                // Both transitions commit after movement, taking effect from the following simulated tick.
                for (var eventIndex = history.Length - 1; eventIndex >= 0; eventIndex--)
                {
                    var transition = history[eventIndex];
                    var transitionTick = new NetworkTick { SerializedData = transition.TransitionTick };
                    if (!transitionTick.IsValid || transition.Disabled > 1)
                    {
                        Debug.LogError("[CombatPrototype.Map] Mine obstacle update failed; stage=ReadTransition, placement=" +
                            value.PlacementIndex + ", mapSource=" + source + ", eventIndex=" + eventIndex + ".");
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
