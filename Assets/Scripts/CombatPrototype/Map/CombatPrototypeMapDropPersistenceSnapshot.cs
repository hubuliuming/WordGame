using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapDropPersistenceSnapshot
    {
        private struct Entry
        {
            internal int DropId, Quantity;
            internal FixedString64Bytes ItemId;
            internal float3 Position;
            internal double ExpiresAt;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly HashSet<int> _ids = new HashSet<int>();
        internal int LastDropId { get; private set; }

        internal void Capture(EntityManager manager, CombatPrototypeMapDropSpawnSystem owner, Entity source,
            CombatPrototypeMapInventoryDropDefinition[] bindings, double time)
        {
            _entries.Clear(); _ids.Clear();
            LastDropId = owner.GetLastAllocatedDropId(source);
            var owned = owner.GetOwnedDrops(source);
            for (var index = 0; index < owned.Count; index++)
            {
                var entity = owned[index];
                // Normal EndSimulation ECB playback can already have removed an owned drop.
                if (!manager.Exists(entity)) continue;
                var state = manager.GetComponentData<CombatPrototypeMapDropState>(entity);
                var progress = manager.GetComponentData<CombatPrototypeMapDropProgress>(entity);
                if (state.Phase == CombatPrototypeMapDropPhase.Prepared || state.Phase == CombatPrototypeMapDropPhase.Consumed ||
                    progress.CleanupQueued != 0 || (progress.ExpiresAt > 0 && time >= progress.ExpiresAt)) continue;
                if (state.Phase != CombatPrototypeMapDropPhase.Airborne && state.Phase != CombatPrototypeMapDropPhase.Landed)
                    throw Error(state, "Unknown committed drop phase.");
                if (state.DropId <= 0 || state.DropId > LastDropId || !_ids.Add(state.DropId) || state.Quantity <= 0)
                    throw Error(state, "Invalid or duplicate drop identity/quantity.");
                CombatPrototypeMapDropPersistenceBindings.Resolve(bindings, state.ItemId);
                var position = state.Phase == CombatPrototypeMapDropPhase.Airborne ? progress.EndPosition :
                    manager.GetComponentData<LocalTransform>(entity).Position;
                if (!math.all(math.isfinite(position)) || double.IsNaN(progress.ExpiresAt) ||
                    double.IsInfinity(progress.ExpiresAt) || progress.ExpiresAt < 0)
                    throw Error(state, "Invalid landing position/expiry.");
                _entries.Add(new Entry { DropId = state.DropId, ItemId = state.ItemId, Quantity = state.Quantity,
                    Position = position, ExpiresAt = progress.ExpiresAt });
            }
        }

        internal bool SameState(CombatPrototypeMapDropPersistenceSnapshot other)
        {
            if (LastDropId != other.LastDropId || _entries.Count != other._entries.Count) return false;
            for (var index = 0; index < _entries.Count; index++)
            {
                var a = _entries[index]; var b = other._entries[index];
                if (a.DropId != b.DropId || a.ItemId != b.ItemId || a.Quantity != b.Quantity ||
                    !math.all(a.Position == b.Position) || a.ExpiresAt != b.ExpiresAt) return false;
            }
            return true;
        }

        internal CombatPrototypeMapDropSaveEntry[] CreateEntries(double observedTime)
        {
            var result = new CombatPrototypeMapDropSaveEntry[_entries.Count];
            for (var index = 0; index < result.Length; index++)
            {
                var entry = _entries[index];
                result[index] = new CombatPrototypeMapDropSaveEntry
                {
                    DropId = entry.DropId, ItemId = entry.ItemId.ToString(), Quantity = entry.Quantity,
                    PositionX = entry.Position.x, PositionY = entry.Position.y, PositionZ = entry.Position.z,
                    HasExpiry = entry.ExpiresAt > 0,
                    RemainingLifetimeSeconds = entry.ExpiresAt > 0 ? Math.Max(entry.ExpiresAt - observedTime, 0) : 0
                };
            }
            return result;
        }

        private static InvalidOperationException Error(CombatPrototypeMapDropState state, string reason) =>
            new InvalidOperationException("Drop snapshot capture failed; DropId=" + state.DropId + ", itemId=" + state.ItemId + ". " + reason);
    }
}
