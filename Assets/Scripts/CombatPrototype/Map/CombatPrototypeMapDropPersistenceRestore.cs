using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapDropPersistenceRestore
    {
        internal static int Apply(CombatPrototypeMapDropSpawnSystem owner, Entity source,
            CombatPrototypeMapResourceSaveData data, CombatPrototypeMapInventoryDropDefinition[] bindings, double time)
        {
            owner.BeginDropRestore(source, data.LastDropId);
            var restored = new List<Entity>(data.Drops.Length);
            try
            {
                foreach (var entry in data.Drops)
                {
                    // A valid zero remainder with HasExpiry means due, never a permanent item.
                    if (entry.HasExpiry && entry.RemainingLifetimeSeconds == 0) continue;
                    var definition = CombatPrototypeMapDropPersistenceBindings.Resolve(bindings, entry.ItemId);
                    restored.Add(owner.RestoreOwnedDrop(source, definition, entry, time));
                }
                return restored.Count;
            }
            catch
            {
                foreach (var entity in restored) owner.ReleaseDrop(entity, "DropRestoreRollback");
                throw;
            }
        }
    }
}
