using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapDropMergeUtility
    {
        internal struct Candidate
        {
            internal Entity Entity;
            internal int DropId, Quantity;
            internal FixedString64Bytes ItemId;
            internal float3 Position;
            internal double ExpiresAt;
            internal bool Absorbed;
        }

        internal static readonly Comparison<Candidate> ByDropId = (a, b) => a.DropId.CompareTo(b.DropId);
        private static readonly FixedString64Bytes AppleId = CombatPrototypeMapYieldItemResolver.VitalityAppleId;
        private static readonly FixedString64Bytes WoodId = CombatPrototypeMapYieldItemResolver.WoodId;
        private static readonly FixedString64Bytes StoneId = CombatPrototypeMapYieldItemResolver.StoneId;

        internal static bool IsEligible(CombatPrototypeMapDropState state, CombatPrototypeMapDropProgress progress, double time)
        {
            if (state.Phase != CombatPrototypeMapDropPhase.Landed)
            {
                if (state.Phase == CombatPrototypeMapDropPhase.Airborne || state.Phase == CombatPrototypeMapDropPhase.Prepared ||
                    state.Phase == CombatPrototypeMapDropPhase.Consumed) return false;
                throw new InvalidOperationException("Unknown drop phase.");
            }
            return progress.CleanupQueued == 0 && !(progress.ExpiresAt > 0d && time >= progress.ExpiresAt);
        }

        internal static void ValidateCandidate(CombatPrototypeMapDropState state, CombatPrototypeMapDropProgress progress,
            float3 position, int lastDropId)
        {
            if (state.DropId <= 0 || state.DropId > lastDropId || state.Quantity <= 0)
                throw new InvalidOperationException("Invalid drop identity/quantity.");
            if (state.ItemId != AppleId && state.ItemId != WoodId && state.ItemId != StoneId)
                throw new InvalidOperationException("Unsupported drop item ID.");
            if (!math.all(math.isfinite(position)) || double.IsNaN(progress.ExpiresAt) ||
                double.IsInfinity(progress.ExpiresAt) || progress.ExpiresAt < 0d)
                throw new InvalidOperationException("Invalid landing position/expiry.");
        }

        internal static int SelectTarget(List<Candidate> candidates, int sourceIndex, float mergeDistance, int maxQuantity)
        {
            var source = candidates[sourceIndex];
            if (source.Absorbed || source.Quantity > maxQuantity) return -1;
            var selected = -1;
            var bestDistance = double.PositiveInfinity;
            var bestId = int.MaxValue;
            var rangeSquared = (double)mergeDistance * mergeDistance;
            for (var index = 0; index < sourceIndex; index++)
            {
                var target = candidates[index];
                if (target.Absorbed || target.DropId >= source.DropId || target.ItemId != source.ItemId ||
                    (target.ExpiresAt > 0d) != (source.ExpiresAt > 0d) ||
                    (long)target.Quantity + source.Quantity > maxQuantity) continue;
                // Double precision keeps finite float positions/radii from overflowing or underflowing when squared.
                var x = (double)source.Position.x - target.Position.x;
                var z = (double)source.Position.z - target.Position.z;
                var distance = x * x + z * z;
                if (distance > rangeSquared || distance > bestDistance ||
                    (distance == bestDistance && target.DropId >= bestId)) continue;
                selected = index;
                bestDistance = distance;
                bestId = target.DropId;
            }
            return selected;
        }

        internal static void Merge(Candidate source, Candidate target, int maxQuantity, double time,
            ComponentLookup<CombatPrototypeMapDropState> states, ComponentLookup<CombatPrototypeMapDropProgress> progresses,
            out int quantity, out double expiresAt)
        {
            // Acquire all required component references and prepare the pair before mutating either entity.
            var sourceState = states.GetRefRW(source.Entity);
            var targetState = states.GetRefRW(target.Entity);
            var targetProgress = progresses.GetRefRW(target.Entity);
            var sourceProgress = progresses[source.Entity];
            if (!IsEligible(sourceState.ValueRO, sourceProgress, time) ||
                !IsEligible(targetState.ValueRO, targetProgress.ValueRO, time) ||
                sourceState.ValueRO.DropId != source.DropId || targetState.ValueRO.DropId != target.DropId ||
                sourceState.ValueRO.ItemId != source.ItemId || targetState.ValueRO.ItemId != target.ItemId ||
                sourceState.ValueRO.Quantity != source.Quantity || targetState.ValueRO.Quantity != target.Quantity ||
                sourceProgress.ExpiresAt != source.ExpiresAt || targetProgress.ValueRO.ExpiresAt != target.ExpiresAt ||
                source.ItemId != target.ItemId || target.DropId >= source.DropId ||
                (source.ExpiresAt > 0d) != (target.ExpiresAt > 0d))
                throw new InvalidOperationException("Merge pair no longer matches the selected candidates.");
            quantity = checked(target.Quantity + source.Quantity);
            if (quantity > maxQuantity)
                throw new InvalidOperationException("Merge pair exceeds the configured stack limit.");
            expiresAt = target.ExpiresAt > 0d ? Math.Min(target.ExpiresAt, source.ExpiresAt) : 0d;

            targetState.ValueRW.Quantity = quantity;
            targetProgress.ValueRW.ExpiresAt = expiresAt;
            sourceState.ValueRW.Phase = CombatPrototypeMapDropPhase.Consumed;
        }
    }
}
