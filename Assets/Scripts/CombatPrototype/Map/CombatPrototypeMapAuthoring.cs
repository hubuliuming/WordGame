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
                var persistence = config.map.resourcePersistence;
                AddComponent(entity, new CombatPrototypeMapResourcePersistenceSettings
                {
                    Enabled = (byte)(persistence.enabled ? 1 : 0), SaveSlotId = persistence.saveSlotId,
                    SaveIntervalSeconds = persistence.saveIntervalSeconds, SaveGroundDrops = (byte)(persistence.saveGroundDrops ? 1 : 0),
                    ManualSaveEnabled = (byte)(persistence.manualSaveEnabled ? 1 : 0),
                    ManualSaveCooldownSeconds = persistence.manualSaveCooldownSeconds,
                    LayoutSignature = new FixedString128Bytes(CombatPrototypeMapResourceLayoutSignature.Compute(config, layout))
                });
                AddComponent(entity, new CombatPrototypeMapResourceRestoreState
                {
                    Phase = persistence.enabled ? CombatPrototypeMapResourceRestorePhase.Pending : CombatPrototypeMapResourceRestorePhase.Ready
                });
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
                var pickupHud = config.map.pickupHud;
                AddComponent(entity, new CombatPrototypeMapPickupHudSettings
                {
                    Enabled = (byte)(pickupHud.enabled ? 1 : 0),
                    PanelWidthPixels = pickupHud.panelWidthPixels, PanelHeightPixels = pickupHud.panelHeightPixels,
                    BottomMarginPixels = pickupHud.bottomMarginPixels, FontSize = pickupHud.fontSize,
                    PickupLabel = new FixedString64Bytes(pickupHud.pickupLabel),
                    AppleLabel = new FixedString64Bytes(pickupHud.appleLabel),
                    WoodLabel = new FixedString64Bytes(pickupHud.woodLabel),
                    StoneLabel = new FixedString64Bytes(pickupHud.stoneLabel),
                    LifetimeEnabled = (byte)(pickupHud.lifetimeEnabled ? 1 : 0),
                    ExpiryWarningEnabled = (byte)(pickupHud.expiryWarningEnabled ? 1 : 0),
                    ExpiryWarningSeconds = pickupHud.expiryWarningSeconds,
                    ExpiresInLabel = new FixedString64Bytes(pickupHud.expiresInLabel),
                    PermanentLabel = new FixedString64Bytes(pickupHud.permanentLabel),
                    ExpiringSoonLabel = new FixedString64Bytes(pickupHud.expiringSoonLabel),
                    SecondsLabel = new FixedString64Bytes(pickupHud.secondsLabel),
                    ExpiryWarningColor = CombatPrototypeMapInteractionHighlightSettings.ColorFromHex(pickupHud.expiryWarningColorHex)
                });
                var highlight = config.map.interactionHighlight;
                AddComponent(entity, new CombatPrototypeMapInteractionHighlightSettings
                {
                    Enabled = (byte)(highlight.enabled ? 1 : 0),
                    FTargetsEnabled = (byte)(highlight.fTargetsEnabled ? 1 : 0),
                    GTargetsEnabled = (byte)(highlight.gTargetsEnabled ? 1 : 0),
                    GatherRadius = highlight.gatherRadiusMeters, TreeRadius = highlight.treeRadiusMeters,
                    MineRadius = highlight.mineRadiusMeters, DropRadius = highlight.dropRadiusMeters,
                    LineWidthPixels = highlight.lineWidthPixels, SegmentCount = highlight.segmentCount,
                    ReadyColor = CombatPrototypeMapInteractionHighlightSettings.ColorFromHex(highlight.readyColorHex),
                    WorkingColor = CombatPrototypeMapInteractionHighlightSettings.ColorFromHex(highlight.workingColorHex),
                    PickupColor = CombatPrototypeMapInteractionHighlightSettings.ColorFromHex(highlight.pickupColorHex),
                    Opacity = highlight.opacity, HeightOffset = highlight.heightOffsetMeters
                });
                var resourceStatus = config.map.resourceStatusHud;
                AddComponent(entity, new CombatPrototypeMapResourceStatusHudSettings
                {
                    Enabled = (byte)(resourceStatus.enabled ? 1 : 0),
                    PanelWidthPixels = resourceStatus.panelWidthPixels, PanelHeightPixels = resourceStatus.panelHeightPixels,
                    BottomMarginPixels = resourceStatus.bottomMarginPixels, FontSize = resourceStatus.fontSize,
                    AvailableLabel = new FixedString64Bytes(resourceStatus.availableLabel),
                    WorkingLabel = new FixedString64Bytes(resourceStatus.workingLabel),
                    OccupiedLabel = new FixedString64Bytes(resourceStatus.occupiedLabel),
                    RegrowingLabel = new FixedString64Bytes(resourceStatus.regrowingLabel),
                    WaitingLabel = new FixedString64Bytes(resourceStatus.waitingLabel),
                    DepletedLabel = new FixedString64Bytes(resourceStatus.depletedLabel)
                });
                var worldSave = config.map.worldSaveHud;
                AddComponent(entity, new CombatPrototypeMapWorldSaveHudSettings
                {
                    Enabled = (byte)(worldSave.enabled ? 1 : 0), PanelWidthPixels = worldSave.panelWidthPixels,
                    PanelHeightPixels = worldSave.panelHeightPixels, BottomMarginPixels = worldSave.bottomMarginPixels,
                    FontSize = worldSave.fontSize, FeedbackSeconds = worldSave.feedbackSeconds,
                    DisabledLabel = new FixedString64Bytes(worldSave.disabledLabel),
                    NotSavedLabel = new FixedString64Bytes(worldSave.notSavedLabel),
                    SavedLabel = new FixedString64Bytes(worldSave.savedLabel),
                    CaptureFailedLabel = new FixedString64Bytes(worldSave.captureFailedLabel),
                    SaveFailedLabel = new FixedString64Bytes(worldSave.saveFailedLabel),
                    ManualSaveLabel = new FixedString64Bytes(worldSave.manualSaveLabel),
                    ManualDisabledLabel = new FixedString64Bytes(worldSave.manualDisabledLabel),
                    CooldownLabel = new FixedString64Bytes(worldSave.cooldownLabel),
                    UnavailableLabel = new FixedString64Bytes(worldSave.unavailableLabel),
                    ErrorColor = CombatPrototypeMapInteractionHighlightSettings.ColorFromHex(worldSave.errorColorHex)
                });
                var inventoryPanel = config.map.inventoryPanel;
                AddComponent(entity, new CombatPrototypeMapInventoryPanelSettings
                {
                    Enabled = (byte)(inventoryPanel.enabled ? 1 : 0),
                    InitiallyOpen = (byte)(inventoryPanel.initiallyOpen ? 1 : 0),
                    PanelWidthPixels = inventoryPanel.panelWidthPixels,
                    PanelHeightPixels = inventoryPanel.panelHeightPixels,
                    RightMarginPixels = inventoryPanel.rightMarginPixels,
                    TopMarginPixels = inventoryPanel.topMarginPixels,
                    FontSize = inventoryPanel.fontSize,
                    RowHeightPixels = inventoryPanel.rowHeightPixels,
                    PanelTitle = new FixedString64Bytes(inventoryPanel.panelTitle),
                    MaterialsLabel = new FixedString64Bytes(inventoryPanel.materialsLabel),
                    ToolsLabel = new FixedString64Bytes(inventoryPanel.toolsLabel),
                    CraftLabel = new FixedString64Bytes(inventoryPanel.craftLabel),
                    CraftButtonLabel = new FixedString64Bytes(inventoryPanel.craftButtonLabel),
                    EmptyInventoryLabel = new FixedString64Bytes(inventoryPanel.emptyInventoryLabel),
                    WoodLabel = new FixedString64Bytes(inventoryPanel.woodLabel),
                    StoneLabel = new FixedString64Bytes(inventoryPanel.stoneLabel),
                    AppleLabel = new FixedString64Bytes(inventoryPanel.appleLabel),
                    MeatLabel = new FixedString64Bytes(inventoryPanel.meatLabel),
                    CloseLabel = new FixedString64Bytes(inventoryPanel.closeLabel),
                    MissingLabel = new FixedString64Bytes(inventoryPanel.missingLabel),
                    UsableLabel = new FixedString64Bytes(inventoryPanel.usableLabel),
                    BrokenLabel = new FixedString64Bytes(inventoryPanel.brokenLabel),
                    NotOwnedLabel = new FixedString64Bytes(inventoryPanel.notOwnedLabel),
                    DisabledLabel = new FixedString64Bytes(inventoryPanel.disabledLabel),
                    ReadyLabel = new FixedString64Bytes(inventoryPanel.readyLabel),
                    RepairLabel = new FixedString64Bytes(inventoryPanel.repairLabel),
                    RepairButtonLabel = new FixedString64Bytes(inventoryPanel.repairButtonLabel),
                    FullDurabilityLabel = new FixedString64Bytes(inventoryPanel.fullDurabilityLabel)
                });
                var inventoryDrop = config.map.inventoryDrop;
                AddComponent(entity, new CombatPrototypeMapInventoryDropSettings
                {
                    Enabled = (byte)(inventoryDrop.enabled ? 1 : 0),
                    SingleDropQuantity = inventoryDrop.singleDropQuantity,
                    AllowDropAll = (byte)(inventoryDrop.allowDropAll ? 1 : 0),
                    FeedbackSeconds = inventoryDrop.feedbackSeconds,
                    DropLabel = new FixedString64Bytes(inventoryDrop.dropLabel),
                    DropAllLabel = new FixedString64Bytes(inventoryDrop.dropAllLabel),
                    UnavailableLabel = new FixedString64Bytes(inventoryDrop.unavailableLabel),
                    SuccessLabel = new FixedString64Bytes(inventoryDrop.successLabel),
                    RejectedLabel = new FixedString64Bytes(inventoryDrop.rejectedLabel),
                    FailureLabel = new FixedString64Bytes(inventoryDrop.failureLabel)
                });
                var inventoryDropDefinitions = AddBuffer<CombatPrototypeMapInventoryDropDefinition>(entity);
                foreach (var item in inventoryDrop.items)
                {
                    var prefab = ReadGhostPrefab(prefabs, item.visualResourceKey, typeof(CombatPrototypeMapDropAuthoring));
                    inventoryDropDefinitions.Add(new CombatPrototypeMapInventoryDropDefinition
                    {
                        Kind = CombatPrototypeMapInventoryDropUtility.ResolveKind(item.itemId),
                        ItemId = new FixedString64Bytes(item.itemId),
                        ItemName = CombatPrototypeMapYieldItemResolver.Resolve(item.itemId),
                        ResourceKey = new FixedString64Bytes(item.visualResourceKey),
                        Prefab = GetEntity(prefab, TransformUsageFlags.Dynamic)
                    });
                }
                var gatheringTools = config.map.gatherTools;
                AddComponent(entity, new CombatPrototypeMapGatherToolSettings
                {
                    Enabled = (byte)(gatheringTools.enabled ? 1 : 0), CraftFeedbackSeconds = gatheringTools.craftFeedbackSeconds,
                    RepairEnabled = (byte)(gatheringTools.repairEnabled ? 1 : 0), RepairFeedbackSeconds = gatheringTools.repairFeedbackSeconds
                });
                var toolDefinitions = AddBuffer<CombatPrototypeMapGatherToolDefinition>(entity);
                foreach (var tool in gatheringTools.tools)
                    toolDefinitions.Add(new CombatPrototypeMapGatherToolDefinition
                    {
                        ToolId = new FixedString64Bytes(tool.toolId), DisplayName = new FixedString64Bytes(tool.displayName),
                        Kind = CombatPrototypeMapGatherToolUtility.ResolveKind(tool.toolId), MaxDurability = tool.maxDurability,
                        DurabilityCostPerCompletion = tool.durabilityCostPerCompletion, DurationMultiplier = tool.durationMultiplier,
                        CraftWoodQuantity = tool.craftWoodQuantity, CraftStoneQuantity = tool.craftStoneQuantity,
                        RepairDurability = tool.repairDurability, RepairWoodQuantity = tool.repairWoodQuantity,
                        RepairStoneQuantity = tool.repairStoneQuantity
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
