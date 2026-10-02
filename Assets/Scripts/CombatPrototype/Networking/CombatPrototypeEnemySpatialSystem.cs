using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypeEnemySpatialEntry
    {
        public Entity Entity;
        public float3 Position;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeEnemyMovementSystem))]
    public partial class CombatPrototypeEnemySpatialSystem : SystemBase
    {
        public const float CellSize = 2f;
        private NativeParallelMultiHashMap<int2, CombatPrototypeEnemySpatialEntry> _cells;
        private EntityQuery _enemies;

        public NativeParallelMultiHashMap<int2, CombatPrototypeEnemySpatialEntry>.ReadOnly Cells => _cells.AsReadOnly();

        public static int2 GetCell(float3 position)
        {
            return (int2)math.floor(position.xz / CellSize);
        }

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _enemies = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeEnemyState>(), ComponentType.ReadOnly<LocalTransform>());
            _cells = new NativeParallelMultiHashMap<int2, CombatPrototypeEnemySpatialEntry>(32, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _cells.Dispose();
        }

        protected override void OnUpdate()
        {
            _cells.Clear();
            var count = _enemies.CalculateEntityCount();
            if (count > _cells.Capacity)
                _cells.Capacity = count;
            foreach (var (health, transform, entity) in SystemAPI.Query<
                         RefRO<CombatPrototypeEnemyState>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (health.ValueRO.IsDead != 0)
                    continue;
                var position = transform.ValueRO.Position;
                // Each living enemy belongs to exactly one cell.
                _cells.Add(GetCell(position), new CombatPrototypeEnemySpatialEntry { Entity = entity, Position = position });
            }
        }
    }
}