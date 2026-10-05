using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapResourceSaveStore
    {
        public const int CurrentVersion = 2;
        private static readonly UTF8Encoding Encoding = new UTF8Encoding(false, true);

        public static string GetSavePath(string slotId, string mapId) =>
            Path.Combine(Application.persistentDataPath, "CombatPrototype", "Worlds", slotId, mapId + ".resources.json");

        internal static CombatPrototypeMapResourceSaveData Load(CombatPrototypeMapResourcePersistenceSettings settings,
            CombatPrototypeMapData map, CombatPrototypeMapResourceBinding[] bindings,
            CombatPrototypeMapInventoryDropDefinition[] dropBindings, out bool restored)
        {
            var path = GetSavePath(settings.SaveSlotId.ToString(), map.MapDefinitionId.ToString());
            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch (FileNotFoundException) { restored = false; return CreateInitial(settings, map); }
            catch (DirectoryNotFoundException) { restored = false; return CreateInitial(settings, map); }
            var offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
            using var input = new StringReader(Encoding.GetString(bytes, offset, bytes.Length - offset));
            using var reader = new JsonTextReader(input)
            {
                DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double
            };
            var root = JToken.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }) as JObject;
            if (reader.Read()) throw Error(path, "$", "Extra content after root.");
            if (root == null || root.Property("Version", StringComparison.Ordinal) == null)
                throw Error(path, "Version", "Expected world save object with required version.");
            var version = Integer(root["Version"], path, "Version", 1, int.MaxValue);
            if (version == 1)
                Shape(root, path, "$", "Version", "SaveSlotId", "MapDefinitionId", "Seed", "ConfigRevision", "LayoutSignature", "Resources");
            else if (version == CurrentVersion)
                Shape(root, path, "$", "Version", "SaveSlotId", "MapDefinitionId", "Seed", "ConfigRevision", "LayoutSignature", "Resources", "LastDropId", "Drops");
            else throw Error(path, "Version", "Unsupported world save version.");
            var seed = Integer(root["Seed"], path, "Seed", 1, uint.MaxValue);
            var revision = Integer(root["ConfigRevision"], path, "ConfigRevision", 1, int.MaxValue);
            var slot = Text(root["SaveSlotId"], path, "SaveSlotId");
            var mapId = Text(root["MapDefinitionId"], path, "MapDefinitionId");
            var signature = Text(root["LayoutSignature"], path, "LayoutSignature");
            if (slot != settings.SaveSlotId.ToString() || mapId != map.MapDefinitionId.ToString() ||
                seed != map.Seed || signature != settings.LayoutSignature.ToString())
                throw Error(path, "$", "Version, slot, map, seed or resource layout/regrowth signature does not match.");
            if (!(root["Resources"] is JArray resources) || resources.Count > bindings.Length)
                throw Error(path, "Resources", "Expected depleted resource array within current resource count.");
            var expected = new Dictionary<int, CombatPrototypeMapResourceBinding>(bindings.Length);
            foreach (var binding in bindings) expected.Add(binding.PlacementIndex, binding);
            var seen = new HashSet<int>();
            var entries = new CombatPrototypeMapResourceSaveEntry[resources.Count];
            for (var index = 0; index < resources.Count; index++)
            {
                var entryPath = "Resources[" + index + "]";
                var entry = resources[index] as JObject;
                Shape(entry, path, entryPath, "Kind", "PlacementIndex", "ObjectId", "RemainingSeconds");
                var placement = (int)Integer(entry["PlacementIndex"], path, entryPath + ".PlacementIndex", 0, int.MaxValue);
                var kind = Text(entry["Kind"], path, entryPath + ".Kind");
                var objectId = Text(entry["ObjectId"], path, entryPath + ".ObjectId");
                var token = entry["RemainingSeconds"];
                if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
                    throw Error(path, entryPath + ".RemainingSeconds", "Expected finite nonnegative number.");
                var remaining = token.Value<double>();
                if (!expected.TryGetValue(placement, out var binding) || !seen.Add(placement) ||
                    kind != binding.SaveKind || objectId != binding.ObjectId || double.IsNaN(remaining) || double.IsInfinity(remaining) ||
                    remaining < 0 || remaining > binding.RegrowSeconds || (binding.RegrowEnabled == 0 && remaining != 0))
                    throw Error(path, entryPath, "Duplicate/unknown resource identity or invalid remaining regrowth seconds.");
                entries[index] = new CombatPrototypeMapResourceSaveEntry
                {
                    Kind = kind, PlacementIndex = placement, ObjectId = objectId, RemainingSeconds = remaining
                };
            }
            var lastDropId = version == 1 ? 0 : (int)Integer(root["LastDropId"], path, "LastDropId", 0, int.MaxValue);
            var drops = version == 1 ? Array.Empty<CombatPrototypeMapDropSaveEntry>() :
                CombatPrototypeMapDropSaveValidator.Read(root["Drops"], lastDropId, path, dropBindings);
            restored = true;
            return new CombatPrototypeMapResourceSaveData
            {
                Version = CurrentVersion, SaveSlotId = slot, MapDefinitionId = mapId, Seed = (uint)seed,
                ConfigRevision = (int)revision, LayoutSignature = signature, Resources = entries, LastDropId = lastDropId, Drops = drops
            };
        }

        internal static CombatPrototypeMapResourceSaveData CreateInitial(CombatPrototypeMapResourcePersistenceSettings settings,
            CombatPrototypeMapData map) => new CombatPrototypeMapResourceSaveData
        {
            Version = CurrentVersion, SaveSlotId = settings.SaveSlotId.ToString(), MapDefinitionId = map.MapDefinitionId.ToString(),
            Seed = map.Seed, ConfigRevision = map.ConfigRevision, LayoutSignature = settings.LayoutSignature.ToString(),
            Resources = Array.Empty<CombatPrototypeMapResourceSaveEntry>(), LastDropId = 0, Drops = Array.Empty<CombatPrototypeMapDropSaveEntry>()
        };

        public static void SavePrepared(CombatPrototypeMapResourceSaveData data)
        {
            var path = GetSavePath(data.SaveSlotId, data.MapDefinitionId);
            var temporary = path + ".tmp";
            var bytes = Encoding.GetBytes(JsonConvert.SerializeObject(data, Formatting.Indented));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        internal static void Shape(JObject value, string path, string field, params string[] fields)
        {
            if (value == null || value.Count != fields.Length) throw Error(path, field, "Object fields do not match required schema.");
            foreach (var name in fields)
                if (value.Property(name, StringComparison.Ordinal) == null) throw Error(path, field + "." + name, "Missing required field.");
        }

        internal static long Integer(JToken value, string path, string field, long minimum, long maximum)
        {
            if (value.Type != JTokenType.Integer || !long.TryParse(value.ToString(Formatting.None), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var number) || number < minimum || number > maximum)
                throw Error(path, field, "Integer outside allowed range.");
            return number;
        }

        internal static string Text(JToken value, string path, string field)
        {
            if (value.Type != JTokenType.String) throw Error(path, field, "Expected string.");
            return value.Value<string>();
        }

        private static InvalidDataException Error(string path, string field, string reason) =>
            new InvalidDataException("Map resource save validation failed; path=" + path + ", field=" + field + ". " + reason);
    }
}
