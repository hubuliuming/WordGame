using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapResourceStatusTargetSelector
    {
        internal struct Target
        {
            internal Entity Entity;
            internal byte Kind;
            internal int PlacementIndex;
            internal float2 Position;
            internal float InteractionDistance;
            internal Entity Collector;
            internal CombatPrototypeMapResourceStatusHudMode Mode;
            internal float RemainingSeconds;
        }

        internal static Target Select(float2 position, Entity player, CombatPrototypeMapInteractionHudState interaction,
            List<Target> targets, Dictionary<(byte, int), Target> identities, HashSet<(byte, int)> failed)
        {
            if (interaction.Mode == CombatPrototypeMapInteractionHudMode.Ready ||
                interaction.Mode == CombatPrototypeMapInteractionHudMode.NoSpace ||
                interaction.Mode == CombatPrototypeMapInteractionHudMode.Working)
            {
                // CaptureState already reports a failed resource; do not report the same failure per player.
                if (failed.Contains((interaction.Kind, interaction.PlacementIndex))) return default;
                if (!identities.TryGetValue((interaction.Kind, interaction.PlacementIndex), out var current))
                    throw new InvalidOperationException("The authoritative F target is absent from resource status data; type=" +
                        interaction.Kind + ", placement=" + interaction.PlacementIndex + ".");
                if (interaction.Mode != CombatPrototypeMapInteractionHudMode.Working ?
                    current.Mode != CombatPrototypeMapResourceStatusHudMode.Available :
                    current.Mode != CombatPrototypeMapResourceStatusHudMode.Occupied || current.Collector != player)
                    throw new InvalidOperationException("Resource state does not match the authoritative F target.");
                return current;
            }
            if (interaction.Mode != CombatPrototypeMapInteractionHudMode.Hidden)
                throw new InvalidOperationException("Unsupported authoritative F mode: " + interaction.Mode);

            var best = default(Target);
            var bestDistance = float.PositiveInfinity;
            foreach (var target in targets)
            {
                if (failed.Contains((target.Kind, target.PlacementIndex))) continue;
                var distance = math.lengthsq(target.Position - position);
                if (distance > target.InteractionDistance * target.InteractionDistance) continue;
                if (best.Entity == Entity.Null || distance < bestDistance ||
                    distance == bestDistance && target.PlacementIndex < best.PlacementIndex)
                {
                    best = target;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
