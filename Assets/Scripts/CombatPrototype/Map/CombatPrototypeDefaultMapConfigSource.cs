using System;

namespace Code_01.CombatPrototype.Map
{
    public sealed class CombatPrototypeDefaultMapConfigSource : ICombatMapConfigSource
    {
        public const string GrasslandId = "battle_grassland_01";
        public const string ForestId = "battle_forest_01";

        public CombatMapConfigSet LoadValidated(string mapDefinitionId)
        {
            var forest = mapDefinitionId == ForestId;
            if (!forest && mapDefinitionId != GrasslandId)
                throw new InvalidOperationException("Unknown map definition: " + mapDefinitionId);

            var config = new CombatMapConfigSet
            {
                map = new MapDefinitionConfig
                {
                    schemaVersion = 32, configRevision = 35,
                    mapDefinitionId = mapDefinitionId, defaultSeed = 12345,
                    resourcePersistence = new MapResourcePersistenceConfig
                    {
                        enabled = true, saveSlotId = "default_world", saveIntervalSeconds = 10f, saveGroundDrops = true,
                        manualSaveEnabled = true, manualSaveCooldownSeconds = 5f
                    },
                    geometry = new MapGeometryConfig
                    {
                        cellSizeMeters = 2f, cellsPerChunk = 16, chunkCountX = 3,
                        chunkCountZ = 3, baseHeightMeters = 0f
                    },
                    layout = new MapLayoutConfig
                    {
                        mainPathGroundId = "grass", mainPathWidthMeters = 4f, minimumPathWidthMeters = 2f,
                        edgeKeepoutMeters = 2f, spawnSafeRadiusMeters = 6f,
                        combatClearRadiusMeters = 8f, enemySpawnMinDistanceMeters = 12f
                    },
                    movement = new MapMovementConfig
                    {
                        playerRadiusMeters = 0.4f, enemyRadiusMeters = 0.45f,
                        collisionSkinMeters = 0.01f, maxSlideIterations = 3
                    },
                    population = new MapPopulationConfig { initialEnemyCount = 32 },
                    drops = new MapDropConfig
                    {
                        enabled = true, partialPickupEnabled = true, itemId = CombatPrototypeMapYieldItemResolver.VitalityAppleId,
                        quantity = 1, visualResourceKey = "drop_apple", pickupDistanceMeters = 2f,
                        flightDurationSeconds = 0.4f, scatterRadiusMeters = 0.6f, arcHeightMeters = 0.6f,
                        groundOffsetMeters = 0.05f, visualScale = 0.5f, lifetimeSeconds = 600f
                    },
                    dropMerge = new MapDropMergeConfig
                    {
                        enabled = true, mergeDistanceMeters = 0.8f, maxStackQuantity = 99, scanIntervalSeconds = 0.2f
                    },
                    treeHarvest = new MapTreeHarvestConfig
                    {
                        enabled = true, treeObjectId = "tree_normal", visualResourceKey = "tree_harvest",
                        harvestDurationSeconds = 2f, dropItemId = CombatPrototypeMapYieldItemResolver.WoodId,
                        dropQuantity = 3, dropVisualResourceKey = "drop_wood"
                    },
                    mining = new MapMiningConfig
                    {
                        enabled = true, mineObjectId = "mine_rock", visualResourceKey = "mine_rock",
                        harvestDurationSeconds = 3f, dropItemId = CombatPrototypeMapYieldItemResolver.StoneId,
                        dropQuantity = 3, dropVisualResourceKey = "drop_stone"
                    },
                    gatherTools = new MapGatherToolsConfig
                    {
                        enabled = true, craftFeedbackSeconds = 2f, repairEnabled = true, repairFeedbackSeconds = 2f,
                        tools = new[]
                        {
                            new MapGatherToolDefinitionConfig
                            {
                                toolId = "stone_axe", displayName = "Axe", targetKind = "tree",
                                maxDurability = 60, durabilityCostPerCompletion = 1, durationMultiplier = 0.75f,
                                craftWoodQuantity = 3, craftStoneQuantity = 2,
                                repairDurability = 20, repairWoodQuantity = 1, repairStoneQuantity = 1
                            },
                            new MapGatherToolDefinitionConfig
                            {
                                toolId = "stone_pickaxe", displayName = "Pickaxe", targetKind = "mine",
                                maxDurability = 40, durabilityCostPerCompletion = 1, durationMultiplier = 0.75f,
                                craftWoodQuantity = 2, craftStoneQuantity = 3,
                                repairDurability = 15, repairWoodQuantity = 1, repairStoneQuantity = 1
                            }
                        }
                    },
                    gatherToolUpgrade = new MapGatherToolUpgradeConfig
                    {
                        enabled = true, feedbackSeconds = 2f, upgradeLabel = "Tool upgrade", upgradeButtonLabel = "Upgrade",
                        levelLabel = "Lv", maxLevelLabel = "Max level", successLabel = "Tool upgraded",
                        rejectedLabel = "Upgrade rejected", failureLabel = "Upgrade failed", recraftLabel = "Recraft at Lv1",
                        levels = new[]
                        {
                            new MapGatherToolUpgradeLevelConfig
                            { toolId = "stone_axe", level = 2, maxDurability = 90, durationMultiplier = 0.60f, woodQuantity = 12, stoneQuantity = 8 },
                            new MapGatherToolUpgradeLevelConfig
                            { toolId = "stone_axe", level = 3, maxDurability = 120, durationMultiplier = 0.50f, woodQuantity = 24, stoneQuantity = 16 },
                            new MapGatherToolUpgradeLevelConfig
                            { toolId = "stone_pickaxe", level = 2, maxDurability = 60, durationMultiplier = 0.60f, woodQuantity = 8, stoneQuantity = 12 },
                            new MapGatherToolUpgradeLevelConfig
                            { toolId = "stone_pickaxe", level = 3, maxDurability = 80, durationMultiplier = 0.50f, woodQuantity = 16, stoneQuantity = 24 }
                        }
                    },
                    gatherToolDurabilityHud = new MapGatherToolDurabilityHudConfig
                    {
                        enabled = true, warningRatio = 0.25f, criticalRatio = 0.10f,
                        warningColorHex = "#FFD166", criticalColorHex = "#FF9F43", brokenColorHex = "#FF6B6B",
                        warningLabel = "Low", criticalLabel = "Critical", brokenLabel = "Broken",
                        remainingUsesLabel = "Uses", repairHintLabel = "Repair"
                    },
                    interactionFailureHud = new MapInteractionFailureHudConfig
                    {
                        enabled = true, feedbackSeconds = 2f, errorColorHex = "#FF6B6B",
                        alreadyInteractingLabel = "Already interacting", movingLabel = "Stop moving first",
                        attackingLabel = "Finish attack first", noTargetLabel = "No available resource",
                        targetUnavailableLabel = "Target unavailable", failedLabel = "Interaction failed"
                    },
                    gatherOutcomeHud = new MapGatherOutcomeHudConfig
                    {
                        enabled = true, feedbackSeconds = 2f,
                        completedColorHex = "#6ED88A", interruptedColorHex = "#FFB454", failedColorHex = "#FF6B6B",
                        gatherCompletedLabel = "Gathering completed", treeCompletedLabel = "Tree felled",
                        mineCompletedLabel = "Mining completed", movingLabel = "Interrupted: moving",
                        attackingLabel = "Interrupted: attacking", hitLabel = "Interrupted: hit",
                        outOfRangeLabel = "Interrupted: out of range", failedLabel = "Resource work failed"
                    },
                    interactionHud = new MapInteractionHudConfig
                    {
                        enabled = true, panelWidthPixels = 320f, panelHeightPixels = 104f,
                        bottomMarginPixels = 48f, fontSize = 20, progressBarHeightPixels = 10f,
                        gatherLabel = "Gather Apple", treeLabel = "Chop Tree", mineLabel = "Mine Rock", noSpaceLabel = "Not enough space"
                    },
                    pickupFeedbackHud = new MapPickupFeedbackHudConfig
                    {
                        enabled = true, feedbackSeconds = 2f, successColorHex = "#6ED88A", failureColorHex = "#FF6B6B",
                        successLabel = "Picked up", movingLabel = "Stop moving first", attackingLabel = "Finish attack first",
                        noTargetLabel = "No available drop", failedLabel = "Pickup failed"
                    },
                    pickupHud = new MapPickupHudConfig
                    {
                        enabled = true, panelWidthPixels = 400f, panelHeightPixels = 84f,
                        bottomMarginPixels = 168f, fontSize = 20, pickupLabel = "Pick up",
                        appleLabel = "Vitality Apple", woodLabel = "Wood", stoneLabel = "Stone", noSpaceLabel = "Not enough space",
                        lifetimeEnabled = true, expiryWarningEnabled = true, expiryWarningSeconds = 30f,
                        expiresInLabel = "Expires in", permanentLabel = "Permanent", expiringSoonLabel = "Expiring soon",
                        secondsLabel = "s", expiryWarningColorHex = "#FFB454"
                    },
                    interactionHighlight = new MapInteractionHighlightConfig
                    {
                        enabled = true, fTargetsEnabled = true, gTargetsEnabled = true,
                        gatherRadiusMeters = 0.65f, treeRadiusMeters = 0.9f,
                        mineRadiusMeters = 0.9f, dropRadiusMeters = 0.45f,
                        lineWidthPixels = 3f, segmentCount = 48,
                        readyColorHex = "#FFD166", workingColorHex = "#6ED88A", pickupColorHex = "#6EC6FF",
                        opacity = 0.9f, heightOffsetMeters = 0.03f
                    },
                    resourceStatusHud = new MapResourceStatusHudConfig
                    {
                        enabled = true, panelWidthPixels = 400f, panelHeightPixels = 52f,
                        bottomMarginPixels = 268f, fontSize = 20, availableLabel = "Available",
                        workingLabel = "Working", occupiedLabel = "In use", regrowingLabel = "Regrows in",
                        waitingLabel = "Waiting to regrow", depletedLabel = "No regrowth"
                    },
                    worldSaveHud = new MapWorldSaveHudConfig
                    {
                        enabled = true, panelWidthPixels = 400f, panelHeightPixels = 84f,
                        bottomMarginPixels = 336f, fontSize = 20, feedbackSeconds = 3f,
                        disabledLabel = "World saving disabled", notSavedLabel = "No world checkpoint yet",
                        savedLabel = "World saved", captureFailedLabel = "Snapshot failed", saveFailedLabel = "World save failed",
                        manualSaveLabel = "F5 Save world", manualDisabledLabel = "Manual save disabled",
                        cooldownLabel = "Save cooldown", unavailableLabel = "Save unavailable", errorColorHex = "#FF6B6B"
                    },
                    inventoryPanel = new MapInventoryPanelConfig
                    {
                        enabled = true,
                        initiallyOpen = false,
                        panelWidthPixels = 380.0f,
                        panelHeightPixels = 640.0f,
                        rightMarginPixels = 24.0f,
                        topMarginPixels = 64.0f,
                        fontSize = 18,
                        rowHeightPixels = 32.0f,
                        panelTitle = "Inventory",
                        materialsLabel = "Materials",
                        capacityLabel = "Capacity", unlimitedLabel = "Unlimited",
                        toolsLabel = "Tools",
                        craftLabel = "Crafting",
                        craftButtonLabel = "Craft",
                        emptyInventoryLabel = "Empty",
                        woodLabel = "Wood",
                        stoneLabel = "Stone",
                        appleLabel = "Apple",
                        meatLabel = "Meat",
                        closeLabel = "B: Close",
                        missingLabel = "Missing",
                        usableLabel = "Still usable",
                        brokenLabel = "Broken",
                        notOwnedLabel = "Not owned",
                        disabledLabel = "Disabled",
                        readyLabel = "Ready",
                        repairLabel = "Repair", repairButtonLabel = "Repair", fullDurabilityLabel = "Full durability",
                        sortEnabled = true, filterEnabled = true,
                        defaultSortMode = "type", defaultFilterMode = "all",
                        sortLabel = "Sort",
                        originalOrderLabel = "Original",
                        typeOrderLabel = "Type",
                        quantityOrderLabel = "Quantity",
                        filterLabel = "Filter",
                        allFilterLabel = "All",
                        resourcesFilterLabel = "Resources",
                        suppliesFilterLabel = "Supplies",
                        otherFilterLabel = "Other",
                        noMatchingItemsLabel = "No matching items",
                        searchEnabled = true, searchIgnoreCase = true, searchMaxLength = 32,
                        searchLabel = "Search", searchPlaceholderLabel = "Name keyword",
                        clearSearchLabel = "Clear", noSearchResultsLabel = "No search results",
                        preferencesEnabled = true, preferencesSaveSearch = true,
                        preferencesFileId = "inventory_display", preferencesSaveDelaySeconds = 0.5f,
                        preferencesResetEnabled = true, preferencesResetLabel = "Reset view",
                        detailsEnabled = true,
                        detailsButtonLabel = "Details",
                        detailsTitleLabel = "Material details",
                        detailsCloseLabel = "Close details",
                        detailsQuantityLabel = "Quantity",
                        detailsDescriptionLabel = "Description",
                        detailsUsageLabel = "Uses",
                        detailsNoUsageLabel = "No listed uses",
                        detailsUnknownDescriptionLabel = "No description configured",
                        woodDescriptionLabel = "Material from trees",
                        stoneDescriptionLabel = "Material from ore nodes",
                        appleDescriptionLabel = "Supply from vegetation",
                        meatDescriptionLabel = "Supply from enemy rewards",
                        meatUsageLabel = "E: Restore power"
                    },
                    inventoryCapacity = new MapInventoryCapacityConfig
                    {
                        enabled = true, maxTotalQuantity = 300,
                        items = new[]
                        {
                            new MapInventoryCapacityItemConfig { itemId = CombatPrototypeMapYieldItemResolver.VitalityAppleId, maxQuantity = 200 },
                            new MapInventoryCapacityItemConfig { itemId = CombatPrototypeMapYieldItemResolver.WoodId, maxQuantity = 200 },
                            new MapInventoryCapacityItemConfig { itemId = CombatPrototypeMapYieldItemResolver.StoneId, maxQuantity = 200 }
                        }
                    },
                    inventoryCapacityUpgrade = new MapInventoryCapacityUpgradeConfig
                    {
                        enabled = true, feedbackSeconds = 2f, upgradeLabel = "Backpack upgrade", upgradeButtonLabel = "Upgrade",
                        levelLabel = "Lv", maxLevelLabel = "Max level", successLabel = "Backpack upgraded",
                        rejectedLabel = "Upgrade rejected", failureLabel = "Upgrade failed",
                        levels = new[]
                        {
                            new MapInventoryCapacityUpgradeLevelConfig
                            {
                                level = 2, maxTotalQuantity = 450, woodQuantity = 20, stoneQuantity = 10,
                                items = new[]
                                {
                                    new MapInventoryCapacityItemConfig { itemId = "vitality_apple", maxQuantity = 300 },
                                    new MapInventoryCapacityItemConfig { itemId = "wood", maxQuantity = 300 },
                                    new MapInventoryCapacityItemConfig { itemId = "stone", maxQuantity = 300 }
                                }
                            },
                            new MapInventoryCapacityUpgradeLevelConfig
                            {
                                level = 3, maxTotalQuantity = 600, woodQuantity = 40, stoneQuantity = 20,
                                items = new[]
                                {
                                    new MapInventoryCapacityItemConfig { itemId = "vitality_apple", maxQuantity = 400 },
                                    new MapInventoryCapacityItemConfig { itemId = "wood", maxQuantity = 400 },
                                    new MapInventoryCapacityItemConfig { itemId = "stone", maxQuantity = 400 }
                                }
                            }
                        }
                    },
                    inventoryDrop = new MapInventoryDropConfig
                    {
                        enabled = true, singleDropQuantity = 1, allowDropAll = true, feedbackSeconds = 2f,
                        dropLabel = "Drop", dropAllLabel = "All", unavailableLabel = "Unavailable",
                        successLabel = "Dropped", rejectedLabel = "Drop rejected", failureLabel = "Drop failed",
                        items = new[]
                        {
                            new MapInventoryDropItemConfig { itemId = CombatPrototypeMapYieldItemResolver.VitalityAppleId, visualResourceKey = "drop_apple" },
                            new MapInventoryDropItemConfig { itemId = CombatPrototypeMapYieldItemResolver.WoodId, visualResourceKey = "drop_wood" },
                            new MapInventoryDropItemConfig { itemId = CombatPrototypeMapYieldItemResolver.StoneId, visualResourceKey = "drop_stone" }
                        }
                    },
                    spawn = new MapSpawnConfig
                    {
                        playerOriginX = 0f, playerOriginZ = 0f, playerSpacingMeters = 2f,
                        actorHeightOffsetMeters = 1f, enemyOriginX = 0f, enemyOriginZ = 16f
                    },
                    biomeIds = new[] { "grassland", "forest", "rocky" },
                    defaultBiomeId = forest ? "forest" : "grassland",
                    biomeRegions = forest
                        ? new[]
                        {
                            Region("grassland", 0.3f, 0.3f, 0.7f, 0.7f),
                            Region("rocky", 0.75f, 0f, 1f, 0.35f)
                        }
                        : new[]
                        {
                            Region("forest", 0f, 0.82f, 1f, 1f),
                            Region("forest", 0f, 0f, 0.12f, 0.82f),
                            Region("rocky", 0.8f, 0f, 1f, 0.4f)
                        }
                },
                biomes = new[]
                {
                    Biome("grassland", "grass", 12f, 0.4f, 0.6f, 0.2f, 0.1f),
                    Biome("forest", "forest_floor", 8f, 1.5f, 0.5f, 0.2f, 0.2f),
                    Biome("rocky", "rock", 3f, 0.1f, 0.2f, 1f, 1f)
                },
                grounds = new[]
                {
                    Ground("grass", "ground_grass"),
                    Ground("forest_floor", "ground_forest"),
                    Ground("rock", "ground_rock")
                },
                objects = new[]
                {
                    new MapObjectDefinitionConfig
                    {
                        objectId = "decor_grass", visualResourceKey = "decor_grass",
                        footprintRadiusMeters = 0f, minimumSameTypeSpacingMeters = 0.5f
                    },
                    new MapObjectDefinitionConfig
                    {
                        objectId = "decor_pebble", visualResourceKey = "decor_pebble",
                        footprintRadiusMeters = 0.3f, minimumSameTypeSpacingMeters = 2.5f
                    },
                    new MapObjectDefinitionConfig
                    {
                        objectId = "tree_normal", visualResourceKey = "tree_normal",
                        footprintRadiusMeters = 0.5f, minimumSameTypeSpacingMeters = 3f,
                        blocksMovement = true, interactionDistanceMeters = 2f, gatherDurationSeconds = 1f,
                        regrowEnabled = true, regrowSeconds = 600f
                    },
                    new MapObjectDefinitionConfig
                    {
                        objectId = "gather_apple", visualResourceKey = "gather_apple",
                        footprintRadiusMeters = 0.3f, minimumSameTypeSpacingMeters = 1.5f,
                        interactionDistanceMeters = 2f, gatherable = true, gatherDurationSeconds = 1f,
                        yieldItemId = CombatPrototypeMapYieldItemResolver.VitalityAppleId, yieldQuantity = 1,
                        regrowEnabled = true, regrowSeconds = 600f
                    },
                    new MapObjectDefinitionConfig
                    {
                        objectId = "mine_rock", visualResourceKey = "mine_rock",
                        footprintRadiusMeters = 0.75f, minimumSameTypeSpacingMeters = 2.5f,
                        blocksMovement = true, interactionDistanceMeters = 2f,
                        regrowEnabled = true, regrowSeconds = 600f
                    }
                }
            };
            CombatPrototypeMapConfigValidator.Validate(config);
            return config;
        }

        private static GroundDefinitionConfig Ground(string id, string key) =>
            new GroundDefinitionConfig
            { groundId = id, visualResourceKey = key, walkable = true, movementMultiplier = 1f };

        private static BiomeDefinitionConfig Biome(string id, string ground, float grass,
            float tree, float gatherable, float rock, float mine) => new BiomeDefinitionConfig
        {
            biomeId = id, groundId = ground, decorationObjectId = "decor_grass",
            rockObjectId = "decor_pebble", treeObjectId = "tree_normal", gatherObjectId = "gather_apple",
            mineObjectId = "mine_rock", decorationDensityPer100m2 = grass,
            treeDensityPer100m2 = tree, gatherableDensityPer100m2 = gatherable,
            rockDensityPer100m2 = rock, mineDensityPer100m2 = mine
        };

        private static MapBiomeRegionConfig Region(string id, float x0, float z0, float x1, float z1) =>
            new MapBiomeRegionConfig { biomeId = id, minX = x0, minZ = z0, maxX = x1, maxZ = z1 };
    }
}
