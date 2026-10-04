using System;
using System.Collections.Generic;
using Unity.Collections;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapPreset { Grassland, Forest }
    public enum CombatPrototypeMapConfigSourceMode { BuiltIn, Json }

    [Serializable]
    public sealed class CombatPrototypeMapMaterialBinding
    {
        public string ResourceKey;
        public Material Material;
    }

    [Serializable]
    public sealed class CombatPrototypeMapPrefabBinding
    {
        public string ResourceKey;
        public GameObject Prefab;
    }

    public sealed class CombatPrototypeMapAuthoring : MonoBehaviour
    {
        public CombatPrototypeMapPreset Preset = CombatPrototypeMapPreset.Forest;
        public CombatPrototypeMapConfigSourceMode SourceMode = CombatPrototypeMapConfigSourceMode.BuiltIn;
        public TextAsset GrasslandJson;
        public TextAsset ForestJson;
        public TextAsset BiomesJson;
        public TextAsset GroundsJson;
        public TextAsset ObjectsJson;
        public CombatPrototypeMapMaterialBinding[] GroundMaterials;
        public CombatPrototypeMapPrefabBinding[] DecorationPrefabs;

        public CombatMapConfigSet LoadMapConfig()
        {
            string id;
            switch (Preset)
            {
                case CombatPrototypeMapPreset.Grassland: id = CombatPrototypeDefaultMapConfigSource.GrasslandId; break;
                case CombatPrototypeMapPreset.Forest: id = CombatPrototypeDefaultMapConfigSource.ForestId; break;
                default: throw new InvalidOperationException("Unknown map preset: " + Preset);
            }
            ICombatMapConfigSource source;
            switch (SourceMode)
            {
                case CombatPrototypeMapConfigSourceMode.BuiltIn:
                    source = new CombatPrototypeDefaultMapConfigSource();
                    break;
                case CombatPrototypeMapConfigSourceMode.Json:
                    source = new CombatPrototypeJsonMapConfigSource(SelectedDefinitionJson(), BiomesJson, GroundsJson, ObjectsJson);
                    break;
                default:
                    throw new InvalidOperationException("Unknown map configuration source: " + SourceMode);
            }
            return source.LoadValidated(id);
        }

        public void RegisterConfigDependencies(IBaker baker)
        {
            if (SourceMode == CombatPrototypeMapConfigSourceMode.BuiltIn) return;
            if (SourceMode != CombatPrototypeMapConfigSourceMode.Json)
                throw new InvalidOperationException("Unknown map configuration source: " + SourceMode);
            baker.DependsOn(SelectedDefinitionJson());
            baker.DependsOn(BiomesJson);
            baker.DependsOn(GroundsJson);
            baker.DependsOn(ObjectsJson);
        }

        private TextAsset SelectedDefinitionJson()
        {
            switch (Preset)
            {
                case CombatPrototypeMapPreset.Grassland: return GrasslandJson;
                case CombatPrototypeMapPreset.Forest: return ForestJson;
                default: throw new InvalidOperationException("Unknown map preset: " + Preset);
            }
        }

        private sealed class Baker : Baker<CombatPrototypeMapAuthoring>
        {
            public override void Bake(CombatPrototypeMapAuthoring authoring)
            {
                var spawner = GetComponent<CombatPrototypePlayerSpawnerAuthoring>();
                if (spawner == null)
                    throw new InvalidOperationException("Map authoring requires the existing player spawner on the same root.");
                authoring.RegisterConfigDependencies(this);
                var config = authoring.LoadMapConfig();
                var materials = ReadMaterials(authoring.GroundMaterials);
                var prefabs = ReadPrefabs(authoring.DecorationPrefabs);
                var layout = CombatPrototypeMapLayoutBuilder.Build(config, spawner.EnemyColumns, spawner.EnemySpacing);
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, layout.Data);
                var hud = config.map.interactionHud;
                AddComponent(entity, new CombatPrototypeMapInteractionHudSettings
                {
                    Enabled = (byte)(hud.enabled ? 1 : 0),
                    PanelWidthPixels = hud.panelWidthPixels, PanelHeightPixels = hud.panelHeightPixels,
                    BottomMarginPixels = hud.bottomMarginPixels, FontSize = hud.fontSize,
                    ProgressBarHeightPixels = hud.progressBarHeightPixels,
                    GatherLabel = new FixedString64Bytes(hud.gatherLabel),
                    TreeLabel = new FixedString64Bytes(hud.treeLabel),
                    MineLabel = new FixedString64Bytes(hud.mineLabel)
                });
                var gatheringTools = config.map.gatherTools;
                AddComponent(entity, new CombatPrototypeMapGatherToolSettings
                {
                    Enabled = (byte)(gatheringTools.enabled ? 1 : 0), CraftFeedbackSeconds = gatheringTools.craftFeedbackSeconds
                });
                var toolDefinitions = AddBuffer<CombatPrototypeMapGatherToolDefinition>(entity);
                foreach (var tool in gatheringTools.tools)
                    toolDefinitions.Add(new CombatPrototypeMapGatherToolDefinition
                    {
                        ToolId = new FixedString64Bytes(tool.toolId), DisplayName = new FixedString64Bytes(tool.displayName),
                        Kind = CombatPrototypeMapGatherToolUtility.ResolveKind(tool.toolId), MaxDurability = tool.maxDurability,
                        DurabilityCostPerCompletion = tool.durabilityCostPerCompletion, DurationMultiplier = tool.durationMultiplier,
                        CraftWoodQuantity = tool.craftWoodQuantity, CraftStoneQuantity = tool.craftStoneQuantity
                    });
                var chunks = AddBuffer<CombatPrototypeMapChunk>(entity);
                foreach (var chunk in layout.Chunks) chunks.Add(chunk);
                var cells = AddBuffer<CombatPrototypeMapCell>(entity);
                foreach (var cell in layout.Cells) cells.Add(cell);
                var decorations = AddBuffer<CombatPrototypeMapDecoration>(entity);
                foreach (var decoration in layout.Decorations) decorations.Add(decoration);
                var obstacles = AddBuffer<CombatPrototypeMapObstacle>(entity);
                foreach (var obstacle in layout.Obstacles) obstacles.Add(obstacle);
                var grounds = AddBuffer<CombatPrototypeMapGround>(entity);
                foreach (var ground in config.grounds)
                {
                    if (!materials.TryGetValue(ground.visualResourceKey, out var material))
                        throw new InvalidOperationException("Missing map ground material: " + ground.visualResourceKey +
                            "; config=" + (authoring.SourceMode == CombatPrototypeMapConfigSourceMode.Json
                                ? CombatPrototypeMapJsonReader.ResourcePath(authoring.GroundsJson) : "BuiltIn") +
                            "; groundId=" + ground.groundId + "; field=visualResourceKey.");
                    DependsOn(material);
                    grounds.Add(new CombatPrototypeMapGround
                    {
                        GroundId = ground.groundId, ResourceKey = ground.visualResourceKey, Material = material,
                        Walkable = (byte)(ground.walkable ? 1 : 0), MovementMultiplier = ground.movementMultiplier
                    });
                }
                var biomes = AddBuffer<CombatPrototypeMapBiome>(entity);
                foreach (var biome in config.biomes)
                    biomes.Add(new CombatPrototypeMapBiome
                    {
                        BiomeId = biome.biomeId, TreeDensityPer100m2 = biome.treeDensityPer100m2,
                        GatherableDensityPer100m2 = biome.gatherableDensityPer100m2, MineDensityPer100m2 = biome.mineDensityPer100m2
                    });
                var treeHarvest = config.map.treeHarvest;
                var treePrefab = ReadGhostPrefab(prefabs, treeHarvest.visualResourceKey, typeof(CombatPrototypeMapTreeAuthoring));
                var woodPrefab = ReadGhostPrefab(prefabs, treeHarvest.dropVisualResourceKey, typeof(CombatPrototypeMapDropAuthoring));
                var treeDefinition = Array.Find(config.objects, item => item.objectId == treeHarvest.treeObjectId);
                AddComponent(entity, new CombatPrototypeMapTreeSettings
                {
                    Enabled = (byte)(treeHarvest.enabled ? 1 : 0),
                    ObjectId = treeHarvest.treeObjectId, ResourceKey = treeHarvest.visualResourceKey,
                    InteractionDistance = treeDefinition.interactionDistanceMeters,
                    HarvestDuration = treeHarvest.harvestDurationSeconds,
                    DropPrefab = GetEntity(woodPrefab, TransformUsageFlags.Dynamic),
                    DropResourceKey = treeHarvest.dropVisualResourceKey,
                    DropItemId = treeHarvest.dropItemId, DropQuantity = treeHarvest.dropQuantity,
                    RegrowEnabled = (byte)(treeDefinition.regrowEnabled ? 1 : 0), RegrowSeconds = treeDefinition.regrowSeconds
                });
                var mining = config.map.mining;
                var minePrefab = ReadGhostPrefab(prefabs, mining.visualResourceKey, typeof(CombatPrototypeMapMineAuthoring));
                var stonePrefab = ReadGhostPrefab(prefabs, mining.dropVisualResourceKey, typeof(CombatPrototypeMapDropAuthoring));
                var mineDefinition = Array.Find(config.objects, item => item.objectId == mining.mineObjectId);
                AddComponent(entity, new CombatPrototypeMapMineSettings
                {
                    Enabled = (byte)(mining.enabled ? 1 : 0),
                    ObjectId = mining.mineObjectId, ResourceKey = mining.visualResourceKey,
                    InteractionDistance = mineDefinition.interactionDistanceMeters,
                    HarvestDuration = mining.harvestDurationSeconds,
                    DropPrefab = GetEntity(stonePrefab, TransformUsageFlags.Dynamic),
                    DropResourceKey = mining.dropVisualResourceKey,
                    DropItemId = mining.dropItemId, DropQuantity = mining.dropQuantity,
                    RegrowEnabled = (byte)(mineDefinition.regrowEnabled ? 1 : 0), RegrowSeconds = mineDefinition.regrowSeconds
                });
                var objects = AddBuffer<CombatPrototypeMapObject>(entity);
                foreach (var item in config.objects)
                {
                    if (!prefabs.TryGetValue(item.visualResourceKey, out var prefab))
                        throw new InvalidOperationException("Missing map decoration prefab: " + item.visualResourceKey +
                            "; config=" + (authoring.SourceMode == CombatPrototypeMapConfigSourceMode.Json
                                ? CombatPrototypeMapJsonReader.ResourcePath(authoring.ObjectsJson) : "BuiltIn") +
                            "; objectId=" + item.objectId + "; field=visualResourceKey.");
                    var mineable = item.objectId == mining.mineObjectId;
                    var ghost = prefab.GetComponent<GhostAuthoringComponent>();
                    var gather = prefab.GetComponent<CombatPrototypeMapGatherAuthoring>();
                    if (item.gatherable)
                    {
                        if (gather == null || ghost == null || prefab.transform.childCount != 0 ||
                            ghost.HasOwner || ghost.SupportAutoCommandTarget ||
                            ghost.DefaultGhostMode != GhostMode.Interpolated || ghost.SupportedGhostModes != GhostModeMask.Interpolated)
                            throw new InvalidOperationException("Gatherable requires one root and interpolated gather Ghost prefab; objectId=" +
                                item.objectId + "; visualResourceKey=" + item.visualResourceKey);
                    }
                    else if (!mineable && (ghost != null || gather != null))
                        throw new InvalidOperationException("Static map object cannot use a gather Ghost prefab; objectId=" + item.objectId);
                    var harvestable = treeHarvest.enabled && item.objectId == treeHarvest.treeObjectId;
                    if (harvestable) prefab = treePrefab;
                    if (mineable) prefab = minePrefab;
                    objects.Add(new CombatPrototypeMapObject
                    {
                        ObjectId = item.objectId, ResourceKey = mineable ? mining.visualResourceKey : harvestable ? treeHarvest.visualResourceKey : item.visualResourceKey,
                        Prefab = GetEntity(prefab, TransformUsageFlags.Dynamic),
                        Gatherable = (byte)(item.gatherable ? 1 : 0), Harvestable = (byte)(harvestable ? 1 : 0), Mineable = (byte)(mineable ? 1 : 0),
                        InteractionDistance = item.interactionDistanceMeters, GatherDuration = item.gatherDurationSeconds,
                        YieldItemName = item.gatherable ? CombatPrototypeMapYieldItemResolver.Resolve(item.yieldItemId) : default,
                        YieldQuantity = item.yieldQuantity,
                        RegrowEnabled = (byte)(item.regrowEnabled ? 1 : 0), RegrowSeconds = item.regrowSeconds
                    });
                }
                var drops = config.map.drops;
                if (!prefabs.TryGetValue(drops.visualResourceKey, out var dropPrefab))
                    throw new InvalidOperationException("Missing map drop prefab: " + drops.visualResourceKey +
                        "; config=" + (authoring.SourceMode == CombatPrototypeMapConfigSourceMode.Json
                            ? CombatPrototypeMapJsonReader.ResourcePath(authoring.SelectedDefinitionJson()) : "BuiltIn") +
                        "; itemId=" + drops.itemId + "; field=drops.visualResourceKey.");
                var dropGhost = dropPrefab.GetComponent<GhostAuthoringComponent>();
                if (dropPrefab.GetComponent<CombatPrototypeMapDropAuthoring>() == null || dropGhost == null ||
                    dropPrefab.transform.childCount != 0 || dropGhost.HasOwner || dropGhost.SupportAutoCommandTarget ||
                    dropGhost.DefaultGhostMode != GhostMode.Interpolated || dropGhost.SupportedGhostModes != GhostModeMask.Interpolated)
                    throw new InvalidOperationException("Drop requires one root and interpolated drop Ghost prefab; itemId=" +
                        drops.itemId + "; visualResourceKey=" + drops.visualResourceKey);
                AddComponent(entity, new CombatPrototypeMapDropSettings
                {
                    Enabled = (byte)(drops.enabled ? 1 : 0),
                    Prefab = GetEntity(dropPrefab, TransformUsageFlags.Dynamic), ResourceKey = drops.visualResourceKey,
                    ItemId = drops.itemId, ItemName = CombatPrototypeMapYieldItemResolver.Resolve(drops.itemId),
                    Quantity = drops.quantity, PickupDistance = drops.pickupDistanceMeters,
                    FlightDuration = drops.flightDurationSeconds, ScatterRadius = drops.scatterRadiusMeters,
                    ArcHeight = drops.arcHeightMeters, GroundOffset = drops.groundOffsetMeters,
                    VisualScale = drops.visualScale, Lifetime = drops.lifetimeSeconds
                });
            }

            private static GameObject ReadGhostPrefab(Dictionary<string, GameObject> prefabs, string key, Type component)
            {
                if (!prefabs.TryGetValue(key, out var prefab))
                    throw new InvalidOperationException("Missing map interaction prefab; resource=" + key + ", requiredComponent=" + component.Name);
                var ghost = prefab.GetComponent<GhostAuthoringComponent>();
                if (prefab.GetComponent(component) == null || ghost == null || prefab.transform.childCount != 0 ||
                    ghost.HasOwner || ghost.SupportAutoCommandTarget || ghost.DefaultGhostMode != GhostMode.Interpolated ||
                    ghost.SupportedGhostModes != GhostModeMask.Interpolated)
                    throw new InvalidOperationException("Map interaction requires a single-root interpolated Ghost; resource=" + key +
                        ", requiredComponent=" + component.Name);
                return prefab;
            }

            private static Dictionary<string, Material> ReadMaterials(CombatPrototypeMapMaterialBinding[] bindings)
            {
                if (bindings == null) throw new InvalidOperationException("Map material bindings are required.");
                var result = new Dictionary<string, Material>(StringComparer.Ordinal);
                foreach (var binding in bindings)
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.ResourceKey) ||
                        binding.Material == null || !result.TryAdd(binding.ResourceKey, binding.Material))
                        throw new InvalidOperationException("Invalid or duplicate map material binding.");
                }
                return result;
            }

            private static Dictionary<string, GameObject> ReadPrefabs(CombatPrototypeMapPrefabBinding[] bindings)
            {
                if (bindings == null) throw new InvalidOperationException("Map decoration prefab bindings are required.");
                var result = new Dictionary<string, GameObject>(StringComparer.Ordinal);
                foreach (var binding in bindings)
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.ResourceKey) ||
                        binding.Prefab == null || !result.TryAdd(binding.ResourceKey, binding.Prefab))
                        throw new InvalidOperationException("Invalid or duplicate map prefab binding.");
                    var filter = binding.Prefab.GetComponent<MeshFilter>();
                    var renderer = binding.Prefab.GetComponent<MeshRenderer>();
                    if (filter == null || filter.sharedMesh == null || renderer == null || renderer.sharedMaterial == null)
                        throw new InvalidOperationException("Map decoration requires root mesh and material: " + binding.ResourceKey);
                }
                return result;
            }
        }
    }
}
