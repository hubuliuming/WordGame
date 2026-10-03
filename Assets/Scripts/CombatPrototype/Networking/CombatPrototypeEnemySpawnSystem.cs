using Code_01.CombatPrototype.Map;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
    [UpdateBefore(typeof(CombatPrototypeGoInGameServerSystem))]
    public partial struct CombatPrototypeEnemySpawnSystem : ISystem
    {
        private bool _spawnAttempted;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatPrototypePlayerSpawner>();
            state.RequireForUpdate<CombatPrototypeMapData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_spawnAttempted)
                return;
            _spawnAttempted = true;

            var spawner = SystemAPI.GetSingleton<CombatPrototypePlayerSpawner>();
            var manager = state.EntityManager;
            var prefab = spawner.EnemyPrefab;
            // These dependencies are shared by the whole batch, so report them once at its boundary.
            if (!manager.HasComponent<Prefab>(prefab) ||
                !manager.HasComponent<LocalTransform>(prefab) ||
                !manager.HasComponent<CombatPrototypeEnemyState>(prefab) ||
                !manager.HasComponent<CombatPrototypeEnemyMovement>(prefab) ||
                !manager.HasComponent<CombatPrototypeEnemyTarget>(prefab) ||
                !manager.HasBuffer<CombatPrototypeDamageEvent>(prefab))
            {
                Debug.LogError($"[CombatPrototype.NetCode] Enemy batch prerequisites failed; prefab={prefab}, required prefab components are missing.");
                return;
            }

            var succeeded = 0;
            for (var index = 0; index < spawner.EnemyCount; index++)
            {
                var enemy = Entity.Null;
                try
                {
                    enemy = manager.Instantiate(prefab);
                    var column = index % spawner.EnemyColumns;
                    var row = index / spawner.EnemyColumns;
                    var position = spawner.EnemyPosition +
                                   new float3(column * spawner.EnemySpacing, 0f, row * spawner.EnemySpacing);
                    manager.SetComponentData(enemy, LocalTransform.FromPosition(position));
                    succeeded++;
                }
                catch (global::System.Exception exception)
                {
                    Debug.LogError($"[CombatPrototype.NetCode] Enemy batch instantiate/initialize failed; index={index}, prefab={prefab}. {exception}");
                    if (enemy != Entity.Null && manager.Exists(enemy))
                    {
                        try
                        {
                            manager.DestroyEntity(enemy);
                        }
                        catch (global::System.Exception cleanupException)
                        {
                            Debug.LogError($"[CombatPrototype.NetCode] Enemy batch cleanup failed; index={index}, prefab={prefab}, entity={enemy}. {cleanupException}");
                        }
                    }
                }
            }
            Debug.Log($"[CombatPrototype.NetCode] Server enemy batch complete; requested={spawner.EnemyCount}, spawned={succeeded}, failed={spawner.EnemyCount - succeeded}, columns={spawner.EnemyColumns}, spacing={spawner.EnemySpacing}.");
        }
    }
}