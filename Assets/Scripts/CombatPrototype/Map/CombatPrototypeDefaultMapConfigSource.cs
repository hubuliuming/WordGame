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
                    schemaVersion = 8, configRevision = 11,
                    mapDefinitionId = mapDefinitionId, defaultSeed = 12345,
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
                        enabled = true, itemId = CombatPrototypeMapYieldItemResolver.VitalityAppleId,
                        quantity = 1, visualResourceKey = "drop_apple", pickupDistanceMeters = 2f,
                        flightDurationSeconds = 0.4f, scatterRadiusMeters = 0.6f, arcHeightMeters = 0.6f,
                        groundOffsetMeters = 0.05f, visualScale = 0.5f, lifetimeSeconds = 600f
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
                        enabled = true, craftFeedbackSeconds = 2f,
                        tools = new[]
                        {
                            new MapGatherToolDefinitionConfig
                            {
                                toolId = "stone_axe", displayName = "Axe", targetKind = "tree",
                                maxDurability = 60, durabilityCostPerCompletion = 1, durationMultiplier = 0.75f,
                                craftWoodQuantity = 3, craftStoneQuantity = 2
                            },
                            new MapGatherToolDefinitionConfig
                            {
                                toolId = "stone_pickaxe", displayName = "Pickaxe", targetKind = "mine",
                                maxDurability = 40, durabilityCostPerCompletion = 1, durationMultiplier = 0.75f,
                                craftWoodQuantity = 2, craftStoneQuantity = 3
                            }
                        }
                    },
                    interactionHud = new MapInteractionHudConfig
                    {
                        enabled = true, panelWidthPixels = 320f, panelHeightPixels = 104f,
                        bottomMarginPixels = 48f, fontSize = 20, progressBarHeightPixels = 10f,
                        gatherLabel = "Gather Apple", treeLabel = "Chop Tree", mineLabel = "Mine Rock"
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
