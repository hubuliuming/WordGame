using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    // 文件协议和读写独立于界面状态；只有客户端偏好协调类调用。
    internal static class CombatPrototypeMapInventoryPanelPreferencesStore
    {
        public const int CurrentVersion = 1;
        private static readonly UTF8Encoding Encoding = new UTF8Encoding(false, true);

        public static string PathFor(string fileId, string mapId) => Path.Combine(Application.persistentDataPath,
            "CombatPrototype", "Client", "InventoryDisplay", fileId, mapId + ".json");

        public static CombatPrototypeMapInventoryPanelPreferencesData Load(string path, string mapId)
        {
            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
            // 只有正式文件不存在属于首次使用；权限/解码/数据错误必须抛给协调边界。
            var offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
            using var input = new StringReader(Encoding.GetString(bytes, offset, bytes.Length - offset));
            using var reader = new JsonTextReader(input)
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double
            };
            var token = JToken.Load(reader, new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            if (reader.Read()) throw new InvalidDataException("Extra content after the preferences root: " + path);
            if (!(token is JObject root) || root.Count != 5)
                throw new InvalidDataException("Preferences require exactly five fields: " + path);
            var version = root["version"];
            if (version == null || version.Type != JTokenType.Integer ||
                !int.TryParse(version.ToString(Formatting.None), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
                value != CurrentVersion)
                throw new InvalidDataException("Preferences require version=1: " + path);
            var storedMap = ReadString(root, "mapDefinitionId", path);
            if (!string.Equals(storedMap, mapId, StringComparison.Ordinal))
                throw new InvalidDataException("Preferences mapDefinitionId mismatch: " + path);
            var search = ReadString(root, "searchText", path);
            ValidateSearch(search, path);
            return new CombatPrototypeMapInventoryPanelPreferencesData
            {
                Version = value,
                MapDefinitionId = storedMap,
                SortMode = CombatPrototypeMapInventoryPanelListView.ResolveSortMode(ReadString(root, "sortMode", path)),
                FilterMode = CombatPrototypeMapInventoryPanelListView.ResolveFilterMode(ReadString(root, "filterMode", path)),
                SearchText = search
            };
        }

        public static void Save(string path, CombatPrototypeMapInventoryPanelPreferencesData data)
        {
            var root = new JObject
            {
                ["version"] = data.Version,
                ["mapDefinitionId"] = data.MapDefinitionId,
                ["sortMode"] = data.SortMode.ToString().ToLowerInvariant(),
                ["filterMode"] = data.FilterMode.ToString().ToLowerInvariant(),
                ["searchText"] = data.SearchText
            };
            var bytes = Encoding.GetBytes(root.ToString(Formatting.Indented) + "\n");
            var temporaryPath = path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporaryPath, path, null);
            else File.Move(temporaryPath, path);
        }

        private static string ReadString(JObject root, string name, string path)
        {
            var token = root[name];
            if (token == null || token.Type != JTokenType.String)
                throw new InvalidDataException(name + " must be a string: " + path);
            return token.Value<string>();
        }

        private static void ValidateSearch(string text, string path)
        {
            if (text.Length > 64) throw new InvalidDataException("searchText exceeds 64 UTF-16 units: " + path);
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (char.IsControl(character) || char.IsLowSurrogate(character))
                    throw new InvalidDataException("searchText contains a control or isolated surrogate: " + path);
                if (!char.IsHighSurrogate(character)) continue;
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                    throw new InvalidDataException("searchText contains an isolated surrogate: " + path);
                index++;
            }
        }
    }
}
