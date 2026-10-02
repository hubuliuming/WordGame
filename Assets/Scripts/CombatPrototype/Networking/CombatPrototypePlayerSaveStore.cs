using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    public static class CombatPrototypePlayerSaveStore
    {
        public const int CurrentVersion = 1;
        private static readonly UTF8Encoding WriteEncoding = new UTF8Encoding(false, true);
        private static readonly ProfilerMarker LoadMarker = new ProfilerMarker("CombatPrototype.PlayerSave.Load");
        private static readonly ProfilerMarker SaveMarker = new ProfilerMarker("CombatPrototype.PlayerSave.SavePrepared");

        // PlayerId has already been validated at the server RPC boundary.
        public static string GetSavePath(string playerId)
        {
            return Path.Combine(Application.persistentDataPath, "CombatPrototype", "Players", playerId + ".json");
        }

        public static CombatPrototypePlayerSaveData Load(string playerId, out bool restored)
        {
            using var profilingScope = LoadMarker.Auto();
            FileStream stream;
            try
            {
                stream = new FileStream(GetSavePath(playerId), FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (FileNotFoundException)
            {
                restored = false;
                return CreateInitial(playerId);
            }
            catch (DirectoryNotFoundException)
            {
                restored = false;
                return CreateInitial(playerId);
            }

            using (stream)
            using (var reader = new StreamReader(stream, new UTF8Encoding(true, true), false))
            {
                var root = JObject.Parse(reader.ReadToEnd(), new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if (root.Count != 5)
                    throw new InvalidDataException("Save requires exactly Version, PlayerId, Coin, Experience and Items.");
                var version = ReadInteger(root, "Version", CurrentVersion, CurrentVersion);
                var identity = root["PlayerId"];
                if (identity == null || identity.Type != JTokenType.String ||
                    !string.Equals(identity.Value<string>(), playerId, StringComparison.Ordinal))
                    throw new InvalidDataException("Save PlayerId does not match the admitted identity.");
                var coin = ReadInteger(root, "Coin", 0, int.MaxValue);
                var experience = ReadInteger(root, "Experience", 0, int.MaxValue);
                if (!(root["Items"] is JArray items))
                    throw new InvalidDataException("Save Items must be an array.");

                var loadedItems = new CombatPrototypePlayerSaveItem[items.Count];
                var names = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < items.Count; index++)
                {
                    if (!(items[index] is JObject item) || item.Count != 2 ||
                        item["ItemName"] == null || item["ItemName"].Type != JTokenType.String)
                        throw new InvalidDataException("Invalid saved inventory entry at index=" + index);
                    var name = item["ItemName"].Value<string>();
                    if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
                        throw new InvalidDataException("Empty or duplicate saved inventory name at index=" + index);
                    // The constructor's overflow exception depends on Unity debug checks.
                    // Validate strict UTF-8 and capacity at this external-data boundary in every build.
                    if (WriteEncoding.GetByteCount(name) > FixedString64Bytes.UTF8MaxLengthInBytes)
                        throw new InvalidDataException("Saved inventory name exceeds the Ghost UTF-8 capacity at index=" + index);
                    loadedItems[index] = new CombatPrototypePlayerSaveItem
                    {
                        ItemName = name,
                        Quantity = ReadInteger(item, "Quantity", 1, int.MaxValue)
                    };
                }
                restored = true;
                return new CombatPrototypePlayerSaveData
                {
                    Version = version,
                    PlayerId = playerId,
                    Coin = coin,
                    Experience = experience,
                    Items = loadedItems
                };
            }
        }

        public static CombatPrototypePlayerSaveData PrepareReward(FixedString64Bytes playerId,
            CombatPrototypePlayerReward reward, DynamicBuffer<CombatPrototypeInventoryItem> inventory,
            int itemIndex, CombatPrototypeInventoryItem nextItem)
        {
            // This projection is the serialized candidate, not a second mutable ECS inventory.
            var itemCount = checked(inventory.Length + (itemIndex < 0 ? 1 : 0));
            var items = new CombatPrototypePlayerSaveItem[itemCount];
            for (var index = 0; index < inventory.Length; index++)
            {
                var item = index == itemIndex ? nextItem : inventory[index];
                items[index] = new CombatPrototypePlayerSaveItem { ItemName = item.ItemName.ToString(), Quantity = item.Quantity };
            }
            if (itemIndex < 0)
                items[inventory.Length] = new CombatPrototypePlayerSaveItem { ItemName = nextItem.ItemName.ToString(), Quantity = nextItem.Quantity };
            return new CombatPrototypePlayerSaveData
            {
                Version = CurrentVersion,
                PlayerId = playerId.ToString(),
                Coin = reward.Coin,
                Experience = reward.Experience,
                Items = items
            };
        }

        public static void SavePrepared(CombatPrototypePlayerSaveData data)
        {
            using var profilingScope = SaveMarker.Auto();
            var path = GetSavePath(data.PlayerId);
            var temporaryPath = path + ".tmp";
            var bytes = WriteEncoding.GetBytes(JsonConvert.SerializeObject(data, Formatting.Indented));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            // A failed write or replacement leaves the previous final file untouched.
            // No work that can fail is scheduled after successful replacement.
            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
        }

        private static CombatPrototypePlayerSaveData CreateInitial(string playerId)
        {
            return new CombatPrototypePlayerSaveData
            {
                Version = CurrentVersion,
                PlayerId = playerId,
                Coin = 0,
                Experience = 0,
                Items = Array.Empty<CombatPrototypePlayerSaveItem>()
            };
        }

        private static int ReadInteger(JObject source, string field, int minimum, int maximum)
        {
            var token = source[field];
            if (token == null || token.Type != JTokenType.Integer)
                throw new InvalidDataException("Save field must be an integer: " + field);
            var value = token.Value<long>();
            if (value < minimum || value > maximum)
                throw new InvalidDataException("Save field is outside the supported range: " + field);
            return (int)value;
        }
    }
}
