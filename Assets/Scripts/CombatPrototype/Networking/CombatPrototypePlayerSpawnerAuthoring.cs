using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    public sealed class CombatPrototypePlayerSpawnerAuthoring : MonoBehaviour
    {
        public GameObject PlayerPrefab;
        public GameObject EnemyPrefab;
        public Vector3 EnemyPosition = new Vector3(0f, 1f, 2f);
        public int EnemyCount = 32;
        public int EnemyColumns = 8;
        public float EnemySpacing = 3f;

        private sealed class Baker : Baker<CombatPrototypePlayerSpawnerAuthoring>
        {
            public override void Bake(CombatPrototypePlayerSpawnerAuthoring authoring)
            {
                if (authoring.PlayerPrefab == null || authoring.EnemyPrefab == null)
                    throw new global::System.InvalidOperationException("CombatPrototypeNetworkRoot requires both ghost prefab references.");

                if (authoring.EnemyCount <= 0 || authoring.EnemyColumns <= 0 ||
                    !math.isfinite(authoring.EnemySpacing) || authoring.EnemySpacing <= 0f ||
                    !math.all(math.isfinite((float3)authoring.EnemyPosition)))
                    throw new global::System.InvalidOperationException("CombatPrototypeNetworkRoot requires a valid enemy grid configuration.");

                AddComponent(GetEntity(TransformUsageFlags.None), new CombatPrototypePlayerSpawner
                {
                    PlayerPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic),
                    EnemyPrefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic),
                    EnemyPosition = authoring.EnemyPosition,
                    EnemyCount = authoring.EnemyCount,
                    EnemyColumns = authoring.EnemyColumns,
                    EnemySpacing = authoring.EnemySpacing
                });
            }
        }
    }

    public struct CombatPrototypePlayerSpawner : IComponentData
    {
        public Entity PlayerPrefab;
        public Entity EnemyPrefab;
        public float3 EnemyPosition;
        public int EnemyCount;
        public int EnemyColumns;
        public float EnemySpacing;
    }
}