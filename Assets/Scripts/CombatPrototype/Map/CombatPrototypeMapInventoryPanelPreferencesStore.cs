using System;
using System.Globalization;
using System.Collections.Generic;
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
        public const int CurrentVersion = 5;
        private static readonly UTF8Encoding Encoding = new UTF8Encoding(false, true);

        public static string PathFor(string fileId, string mapId) => Path.Combine(Application.persistentDataPath,
            "CombatPrototype", "Client", "InventoryDisplay", fileId, mapId + ".json");

        public static CombatPrototypeMapInventoryPanelPreferencesData Load(string path, string mapId, CombatPrototypeMapInventoryRecipeFilterMode legacyRecipeMode)
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
            if (!(token is JObject root))
                throw new InvalidDataException("Preferences require an object: " + path);
            var version = root["version"];
            if (version == null || version.Type != JTokenType.Integer ||
                !int.TryParse(version.ToString(Formatting.None), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
                (value != 1 && value != 2 && value != 3 && value != 4 && value != CurrentVersion))
                throw new InvalidDataException("Preferences require version=1, 2, 3, 4 or 5: " + path);
            if (root.Count != (value == 1 ? 5 : value == 2 ? 6 : value == 3 ? 7 : value == 4 ? 9 : 10))
                throw new InvalidDataException("Preferences require exactly five v1, six v2, seven v3, nine v4 or ten v5 fields: " + path);
            var storedMap = ReadString(root, "mapDefinitionId", path);
            if (!string.Equals(storedMap, mapId, StringComparison.Ordinal))
                throw new InvalidDataException("Preferences mapDefinitionId mismatch: " + path);
            var search = ReadString(root, "searchText", path);
            ValidateSearch(search, path, "searchText");
            var recipeSearch = value >= 4 ? ReadString(root, "recipeSearchText", path) : string.Empty;
            ValidateSearch(recipeSearch, path, "recipeSearchText");
            return new CombatPrototypeMapInventoryPanelPreferencesData
            {
                Version = CurrentVersion,
                MapDefinitionId = storedMap,
                SortMode = CombatPrototypeMapInventoryPanelListView.ResolveSortMode(ReadString(root, "sortMode", path)),
                FilterMode = CombatPrototypeMapInventoryPanelListView.ResolveFilterMode(ReadString(root, "filterMode", path)),
                SearchText = search,
                FavoriteItemNames = value == 1 ? new List<string>() : ReadFavorites(root, path),
                FavoritesOnly = value >= 3 && ReadBool(root, "favoritesOnly", path),
                RecipeFilterMode = value >= 4 ? CombatPrototypeMapInventoryRecipeFilter.ResolveMode(ReadString(root, "recipeFilterMode", path), "recipeFilterMode") : legacyRecipeMode,
                RecipeSearchText = recipeSearch,
                FavoriteRecipeIds = value == CurrentVersion ? ReadRecipeFavorites(root, path) : new List<string>()
            };
        }

        public static void Save(string path, CombatPrototypeMapInventoryPanelPreferencesData data)
        {
            var favorites = new JArray();
            foreach (var name in data.FavoriteItemNames) favorites.Add(name);
            var recipeFavorites = new JArray();
            foreach (var id in data.FavoriteRecipeIds) recipeFavorites.Add(id);
            var root = new JObject
            {
                ["version"] = data.Version,
                ["mapDefinitionId"] = data.MapDefinitionId,
                ["sortMode"] = data.SortMode.ToString().ToLowerInvariant(),
                ["filterMode"] = data.FilterMode.ToString().ToLowerInvariant(),
                ["searchText"] = data.SearchText,
                ["favoriteItemNames"] = favorites,
                ["favoritesOnly"] = data.FavoritesOnly,
                ["recipeFilterMode"] = data.RecipeFilterMode.ToString().ToLowerInvariant(),
                ["recipeSearchText"] = data.RecipeSearchText,
                ["favoriteRecipeIds"] = recipeFavorites
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

        private static List<string> ReadRecipeFavorites(JObject root, string path)
        {
            if (!(root["favoriteRecipeIds"] is JArray values) || values.Count > CombatPrototypeMapInventoryRecipeFavorites.MaximumStoredCount)
                throw new InvalidDataException("favoriteRecipeIds requires an array of at most seven stable recipe IDs: " + path);
            var ids = new List<string>(values.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (value.Type != JTokenType.String || !CombatPrototypeMapInventoryRecipeFavorites.TryResolveId(value.Value<string>(), out _) || !seen.Add(value.Value<string>()))
                    throw new InvalidDataException("favoriteRecipeIds requires unique known recipe IDs: " + path);
                ids.Add(value.Value<string>());
            }
            return ids;
        }

        private static string ReadString(JObject root, string name, string path)
        {
            var token = root[name];
            if (token == null || token.Type != JTokenType.String)
                throw new InvalidDataException(name + " must be a string: " + path);
            return token.Value<string>();
        }

        private static bool ReadBool(JObject root, string name, string path)
        {
            var token = root[name];
            if (token == null || token.Type != JTokenType.Boolean)
                throw new InvalidDataException(name + " must be a boolean: " + path);
            return token.Value<bool>();
        }

        private static List<string> ReadFavorites(JObject root, string path)
        {
            if (!(root["favoriteItemNames"] is JArray values) || values.Count > CombatPrototypeMapInventoryPanelFavorites.MaximumStoredCount)
                throw new InvalidDataException("favoriteItemNames requires an array of at most 256 names: " + path);
            var names = new List<string>(values.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                if (value.Type != JTokenType.String)
                    throw new InvalidDataException("favoriteItemNames requires strings; index=" + index + ": " + path);
                var name = value.Value<string>();
                if (string.IsNullOrWhiteSpace(name) || Encoding.GetByteCount(name) > 61 || !seen.Add(name))
                    throw new InvalidDataException("favoriteItemNames requires unique nonempty names up to 61 UTF-8 bytes; index=" + index + ": " + path);
                foreach (var character in name)
                    if (char.IsControl(character))
                        throw new InvalidDataException("favoriteItemNames does not allow control characters; index=" + index + ": " + path);
                names.Add(name);
            }
            return names;
        }

        private static void ValidateSearch(string text, string path, string name)
        {
            if (text.Length > 64) throw new InvalidDataException(name + " exceeds 64 UTF-16 units: " + path);
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (char.IsControl(character) || char.IsLowSurrogate(character))
                    throw new InvalidDataException(name + " contains a control or isolated surrogate: " + path);
                if (!char.IsHighSurrogate(character)) continue;
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                    throw new InvalidDataException(name + " contains an isolated surrogate: " + path);
                index++;
            }
        }
    }
}
