using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapDropTargetSelector
    {
        internal static Entity Select(float2 position, float pickupDistance, double time, NativeArray<Entity> drops,
            ComponentLookup<CombatPrototypeMapDropState> states,
            ComponentLookup<CombatPrototypeMapDropProgress> progresses, ComponentLookup<LocalTransform> transforms)
        {
            var target = Entity.Null;
            var bestDistance = float.PositiveInfinity;
            var bestId = int.MaxValue;
            foreach (var entity in drops)
            {
                var drop = states[entity];
                if (drop.Phase != CombatPrototypeMapDropPhase.Landed) continue;
                var expiresAt = progresses[entity].ExpiresAt;
                if (expiresAt > 0d && time >= expiresAt) continue;
                var distance = math.distancesq(position, transforms[entity].Position.xz);
                if (distance > pickupDistance * pickupDistance || distance > bestDistance ||
                    (distance == bestDistance && drop.DropId >= bestId)) continue;
                target = entity;
                bestDistance = distance;
                bestId = drop.DropId;
            }
            return target;
        }
    }
}
