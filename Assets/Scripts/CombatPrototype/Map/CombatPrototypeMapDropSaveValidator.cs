using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Unity.Collections;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapDropSaveValidator
    {
        // A null binding set is intentional when ground-drop persistence is disabled.
        internal static CombatPrototypeMapDropSaveEntry[] Read(JToken token, int lastDropId, string path,
            CombatPrototypeMapInventoryDropDefinition[] bindings)
        {
            if (!(token is JArray drops)) throw Error(path, "Drops", "Expected drop array.");
            var entries = new CombatPrototypeMapDropSaveEntry[drops.Count];
            var seen = new HashSet<int>();
            for (var index = 0; index < drops.Count; index++)
            {
                var field = "Drops[" + index + "]";
                var value = drops[index] as JObject;
                CombatPrototypeMapResourceSaveStore.Shape(value, path, field, "DropId", "ItemId", "Quantity",
                    "PositionX", "PositionY", "PositionZ", "HasExpiry", "RemainingLifetimeSeconds");
                var id = (int)CombatPrototypeMapResourceSaveStore.Integer(value["DropId"], path, field + ".DropId", 1, int.MaxValue);
                if (id > lastDropId || !seen.Add(id)) throw Error(path, field + ".DropId", "Duplicate or above allocated high-water mark.");
                var item = CombatPrototypeMapResourceSaveStore.Text(value["ItemId"], path, field + ".ItemId");
                CombatPrototypeMapYieldItemResolver.Resolve(item);
                if (bindings != null) CombatPrototypeMapDropPersistenceBindings.Resolve(bindings, new FixedString64Bytes(item));
                var quantity = (int)CombatPrototypeMapResourceSaveStore.Integer(value["Quantity"], path, field + ".Quantity", 1, int.MaxValue);
                var x = Position(value["PositionX"], path, field + ".PositionX");
                var y = Position(value["PositionY"], path, field + ".PositionY");
                var z = Position(value["PositionZ"], path, field + ".PositionZ");
                if (value["HasExpiry"].Type != JTokenType.Boolean) throw Error(path, field + ".HasExpiry", "Expected boolean.");
                var expiry = value["HasExpiry"].Value<bool>();
                var remaining = Number(value["RemainingLifetimeSeconds"], path, field + ".RemainingLifetimeSeconds");
                if (remaining < 0 || (!expiry && remaining != 0))
                    throw Error(path, field + ".RemainingLifetimeSeconds", "Expected nonnegative seconds; permanent drops require zero.");
                entries[index] = new CombatPrototypeMapDropSaveEntry
                {
                    DropId = id, ItemId = item, Quantity = quantity, PositionX = x, PositionY = y, PositionZ = z,
                    HasExpiry = expiry, RemainingLifetimeSeconds = remaining
                };
            }
            return entries;
        }

        private static float Position(JToken token, string path, string field)
        {
            var value = Number(token, path, field);
            if (value < -float.MaxValue || value > float.MaxValue) throw Error(path, field, "Position exceeds finite float range.");
            return (float)value;
        }

        private static double Number(JToken token, string path, string field)
        {
            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float) throw Error(path, field, "Expected number.");
            var value = token.Value<double>();
            if (double.IsNaN(value) || double.IsInfinity(value)) throw Error(path, field, "Expected finite number.");
            return value;
        }

        private static InvalidDataException Error(string path, string field, string reason) =>
            new InvalidDataException("Map drop save validation failed; path=" + path + ", field=" + field + ". " + reason);
    }
}
