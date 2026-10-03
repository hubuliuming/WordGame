using Unity.Collections;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapMovementUtility
    {
        public static float2 Move(float2 position, float2 displacement, float actorRadius,
            in CombatPrototypeMapData map, NativeArray<CombatPrototypeMapObstacle> obstacles)
        {
            var remaining = displacement;
            for (var iteration = 0; iteration < map.MaxSlideIterations; iteration++)
            {
                var lengthSquared = math.lengthsq(remaining);
                if (lengthSquared <= 1e-12f) break;
                var time = 1f;
                var normal = float2.zero;
                var hit = false;
                for (var i = 0; i < obstacles.Length; i++)
                {
                    var obstacle = obstacles[i];
                    if (obstacle.Disabled != 0) continue;
                    if (!Sweep(position - obstacle.Position, remaining, lengthSquared,
                        actorRadius + obstacle.Radius + map.CollisionSkin, out var candidateTime, out var candidateNormal))
                        continue;
                    // Equal-time contacts use the stable baked obstacle order.
                    if (hit && candidateTime >= time) continue;
                    hit = true;
                    time = candidateTime;
                    normal = candidateNormal;
                }
                if (!hit) return position + remaining;
                position += remaining * time;
                remaining *= 1f - time;
                var inward = math.dot(remaining, normal);
                if (inward < 0f) remaining -= normal * inward;
            }
            // Unresolved displacement is discarded at the configured iteration limit.
            return position;
        }

        private static bool Sweep(float2 offset, float2 displacement, float lengthSquared, float radius,
            out float time, out float2 normal)
        {
            time = 0f;
            normal = float2.zero;
            var distanceToBoundary = math.lengthsq(offset) - radius * radius;
            var approach = math.dot(offset, displacement);
            if (distanceToBoundary <= 0f)
            {
                // Existing overlap may move out or sideways, but cannot move farther inward.
                if (approach >= 0f) return false;
                normal = math.normalizesafe(offset, new float2(1f, 0f));
                return math.dot(displacement, normal) < 0f;
            }
            if (approach >= 0f) return false;
            var discriminant = approach * approach - lengthSquared * distanceToBoundary;
            if (discriminant < 0f) return false;
            time = (-approach - math.sqrt(discriminant)) / lengthSquared;
            if (time < 0f || time > 1f) return false;
            normal = math.normalizesafe(offset + displacement * time, new float2(1f, 0f));
            return true;
        }
    }
}
