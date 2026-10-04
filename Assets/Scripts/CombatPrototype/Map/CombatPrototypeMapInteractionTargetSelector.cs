using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Code_01.CombatPrototype.Map
{
    internal enum CombatPrototypeMapInteractionKind : byte { None, Gather, Tree, Mine }

    internal struct CombatPrototypeMapInteractionTarget
    {
        public Entity Entity;
        public CombatPrototypeMapInteractionKind Kind;
        public int PlacementIndex;
        public float DistanceSquared;
    }

    internal static class CombatPrototypeMapInteractionTargetSelector
    {
        internal static CombatPrototypeMapInteractionTarget Select(EntityManager manager, float2 position,
            NativeArray<Entity> points, NativeArray<Entity> trees, NativeArray<Entity> mines,
            CombatPrototypeMapTreeSettings treeSettings, CombatPrototypeMapMineSettings mineSettings)
        {
            var best = new CombatPrototypeMapInteractionTarget
            {
                Entity = Entity.Null, PlacementIndex = int.MaxValue, DistanceSquared = float.PositiveInfinity
            };
            foreach (var point in points)
            {
                var state = manager.GetComponentData<CombatPrototypeMapGatherState>(point);
                if (state.Phase != CombatPrototypeMapGatherPhase.Available) continue;
                Consider(ref best, manager, position, point, CombatPrototypeMapInteractionKind.Gather,
                    state.PlacementIndex, manager.GetComponentData<CombatPrototypeMapGatherConfig>(point).InteractionDistance);
            }
            if (treeSettings.Enabled != 0)
                foreach (var tree in trees)
                {
                    var state = manager.GetComponentData<CombatPrototypeMapTreeState>(tree);
                    if (state.Phase != CombatPrototypeMapTreePhase.Standing) continue;
                    Consider(ref best, manager, position, tree, CombatPrototypeMapInteractionKind.Tree,
                        state.PlacementIndex, treeSettings.InteractionDistance);
                }
            if (mineSettings.Enabled != 0)
                foreach (var mine in mines)
                {
                    var state = manager.GetComponentData<CombatPrototypeMapMineState>(mine);
                    if (state.Phase != CombatPrototypeMapMinePhase.Available) continue;
                    Consider(ref best, manager, position, mine, CombatPrototypeMapInteractionKind.Mine,
                        state.PlacementIndex, mineSettings.InteractionDistance);
                }
            return best;
        }

        private static void Consider(ref CombatPrototypeMapInteractionTarget best, EntityManager manager,
            float2 position, Entity entity, CombatPrototypeMapInteractionKind kind, int placement, float range)
        {
            var distance = math.distancesq(position, manager.GetComponentData<LocalTransform>(entity).Position.xz);
            if (distance > range * range || distance > best.DistanceSquared ||
                (distance == best.DistanceSquared && placement >= best.PlacementIndex)) return;
            best = new CombatPrototypeMapInteractionTarget
            {
                Entity = entity, Kind = kind, PlacementIndex = placement, DistanceSquared = distance
            };
        }
    }
}
