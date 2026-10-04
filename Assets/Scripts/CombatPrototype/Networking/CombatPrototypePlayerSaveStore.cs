using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Code_01.CombatPrototype.Map;
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
        public const int CurrentVersion = 2;
        private static readonly UTF8Encoding WriteEncoding = new UTF8Encoding(false, true);
        private static readonly ProfilerMarker LoadMarker = new ProfilerMarker("CombatPrototype.PlayerSave.Load");
        private static readonly ProfilerMarker SaveMarker = new ProfilerMarker("CombatPrototype.PlayerSave.SavePrepared");

        // PlayerId has already been validated at the server RPC boundary.
        public static string GetSavePath(string playerId)
        {
            return Path.Combine(Application.persistentDataPath, "CombatPrototype", "Players", playerId + ".json");
        }

        public static CombatPrototypePlayerSaveData Load(string playerId,
            DynamicBuffer<CombatPrototypeMapGatherToolDefinition> toolDefinitions, out bool restored)
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
                var version = ReadInteger(root, "Version", 1, CurrentVersion);
                if (root.Count != (version == 1 ? 5 : 6))
                    throw new InvalidDataException("Save v1 requires five original fields; v2 additionally requires Tools.");
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
                var loadedTools = Array.Empty<CombatPrototypePlayerSaveTool>();
                if (version == 2)
                {
                    if (!(root["Tools"] is JArray tools) || tools.Count > 2)
                        throw new InvalidDataException("Save Tools must be an array of at most two unique tools.");
                    loadedTools = new CombatPrototypePlayerSaveTool[tools.Count];
                    var toolIds = new HashSet<string>(StringComparer.Ordinal);
                    for (var index = 0; index < tools.Count; index++)
                    {
                        if (!(tools[index] is JObject tool) || tool.Count != 2 ||
                            tool["ToolId"] == null || tool["ToolId"].Type != JTokenType.String)
                            throw new InvalidDataException("Invalid saved tool at index=" + index);
                        var toolId = tool["ToolId"].Value<string>();
                        if (!toolIds.Add(toolId)) throw new InvalidDataException("Duplicate saved tool at index=" + index);
                        var kind = CombatPrototypeMapGatherToolUtility.ResolveKind(toolId);
                        var definition = CombatPrototypeMapGatherToolUtility.RequireDefinition(toolDefinitions, kind);
                        loadedTools[index] = new CombatPrototypePlayerSaveTool
                        {
                            ToolId = toolId, Durability = ReadInteger(tool, "Durability", 0, definition.MaxDurability)
                        };
                    }
                }
                restored = true;
                return new CombatPrototypePlayerSaveData
                {
                    Version = CurrentVersion,
                    PlayerId = playerId,
                    Coin = coin,
                    Experience = experience,
                    Items = loadedItems,
                    Tools = loadedTools
                };
            }
        }

        public static CombatPrototypePlayerSaveData PrepareReward(FixedString64Bytes playerId,
            CombatPrototypePlayerReward reward, DynamicBuffer<CombatPrototypeInventoryItem> inventory,
            DynamicBuffer<CombatPrototypeMapGatherTool> tools,
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
                Items = items,
                Tools = ProjectTools(tools)
            };
        }

        public static CombatPrototypePlayerSaveData PrepareItemConsumption(FixedString64Bytes playerId,
            CombatPrototypePlayerReward reward, DynamicBuffer<CombatPrototypeInventoryItem> inventory,
            DynamicBuffer<CombatPrototypeMapGatherTool> tools,
            int itemIndex, CombatPrototypeInventoryItem nextItem)
        {
            var removeItem = nextItem.Quantity == 0;
            var items = new CombatPrototypePlayerSaveItem[inventory.Length - (removeItem ? 1 : 0)];
            var targetIndex = 0;
            for (var index = 0; index < inventory.Length; index++)
            {
                if (index == itemIndex && removeItem)
                    continue;
                var item = index == itemIndex ? nextItem : inventory[index];
                items[targetIndex++] = new CombatPrototypePlayerSaveItem
                {
                    ItemName = item.ItemName.ToString(),
                    Quantity = item.Quantity
                };
            }
            return new CombatPrototypePlayerSaveData
            {
                Version = CurrentVersion,
                PlayerId = playerId.ToString(),
                Coin = reward.Coin,
                Experience = reward.Experience,
                Items = items,
                Tools = ProjectTools(tools)
            };
        }

        public static CombatPrototypePlayerSaveData PrepareToolCraft(FixedString64Bytes playerId,
            CombatPrototypePlayerReward reward, DynamicBuffer<CombatPrototypeInventoryItem> inventory,
            DynamicBuffer<CombatPrototypeMapGatherTool> tools, int woodIndex, int nextWood,
            int stoneIndex, int nextStone, int toolIndex, CombatPrototypeMapGatherTool nextTool)
        {
            var count = inventory.Length - (woodIndex >= 0 && nextWood == 0 ? 1 : 0) -
                        (stoneIndex >= 0 && nextStone == 0 ? 1 : 0);
            var items = new CombatPrototypePlayerSaveItem[count];
            var targetIndex = 0;
            for (var index = 0; index < inventory.Length; index++)
            {
                var item = inventory[index];
                if (index == woodIndex) item.Quantity = nextWood;
                if (index == stoneIndex) item.Quantity = nextStone;
                if (item.Quantity == 0) continue;
                items[targetIndex++] = new CombatPrototypePlayerSaveItem { ItemName = item.ItemName.ToString(), Quantity = item.Quantity };
            }
            return new CombatPrototypePlayerSaveData
            {
                Version = CurrentVersion, PlayerId = playerId.ToString(), Coin = reward.Coin, Experience = reward.Experience,
                Items = items, Tools = ProjectTools(tools, toolIndex, nextTool)
            };
        }

        public static CombatPrototypePlayerSaveData PrepareToolUse(FixedString64Bytes playerId,
            CombatPrototypePlayerReward reward, DynamicBuffer<CombatPrototypeInventoryItem> inventory,
            DynamicBuffer<CombatPrototypeMapGatherTool> tools, int toolIndex, CombatPrototypeMapGatherTool nextTool)
        {
            var items = new CombatPrototypePlayerSaveItem[inventory.Length];
            for (var index = 0; index < inventory.Length; index++)
                items[index] = new CombatPrototypePlayerSaveItem
                {
                    ItemName = inventory[index].ItemName.ToString(), Quantity = inventory[index].Quantity
                };
            return new CombatPrototypePlayerSaveData
            {
                Version = CurrentVersion, PlayerId = playerId.ToString(), Coin = reward.Coin, Experience = reward.Experience,
                Items = items, Tools = ProjectTools(tools, toolIndex, nextTool)
            };
        }

        private static CombatPrototypePlayerSaveTool[] ProjectTools(DynamicBuffer<CombatPrototypeMapGatherTool> tools)
        {
            var result = new CombatPrototypePlayerSaveTool[tools.Length];
            for (var index = 0; index < tools.Length; index++)
                result[index] = new CombatPrototypePlayerSaveTool { ToolId = tools[index].ToolId.ToString(), Durability = tools[index].Durability };
            return result;
        }

        private static CombatPrototypePlayerSaveTool[] ProjectTools(DynamicBuffer<CombatPrototypeMapGatherTool> tools,
            int toolIndex, CombatPrototypeMapGatherTool nextTool)
        {
            var result = new CombatPrototypePlayerSaveTool[checked(tools.Length + (toolIndex < 0 ? 1 : 0))];
            for (var index = 0; index < tools.Length; index++)
            {
                var tool = index == toolIndex ? nextTool : tools[index];
                result[index] = new CombatPrototypePlayerSaveTool { ToolId = tool.ToolId.ToString(), Durability = tool.Durability };
            }
            if (toolIndex < 0)
                result[tools.Length] = new CombatPrototypePlayerSaveTool { ToolId = nextTool.ToolId.ToString(), Durability = nextTool.Durability };
            return result;
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
                Items = Array.Empty<CombatPrototypePlayerSaveItem>(),
                Tools = Array.Empty<CombatPrototypePlayerSaveTool>()
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
