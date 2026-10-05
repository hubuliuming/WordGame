using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapDefinitionConfig
    {
        public int schemaVersion;
        public int configRevision;
        public string mapDefinitionId;
        public int defaultSeed;
        public MapResourcePersistenceConfig resourcePersistence;
        public MapGeometryConfig geometry;
        public MapLayoutConfig layout;
        public MapMovementConfig movement;
        public MapDropConfig drops;
        public MapTreeHarvestConfig treeHarvest;
        public MapMiningConfig mining;
        public MapGatherToolsConfig gatherTools;
        public MapInteractionHudConfig interactionHud;
        public MapPickupHudConfig pickupHud;
        public MapInteractionHighlightConfig interactionHighlight;
        public MapResourceStatusHudConfig resourceStatusHud;
        public MapWorldSaveHudConfig worldSaveHud;
        public MapInventoryPanelConfig inventoryPanel;
        public MapInventoryCapacityConfig inventoryCapacity;
        public MapInventoryCapacityUpgradeConfig inventoryCapacityUpgrade;
        public MapInventoryDropConfig inventoryDrop;
        public MapPopulationConfig population;
        public MapSpawnConfig spawn;
        public string[] biomeIds;
        public string defaultBiomeId;
        public MapBiomeRegionConfig[] biomeRegions;
    }

    [Serializable]
    public sealed class MapGeometryConfig
    {
        public float cellSizeMeters;
        public int cellsPerChunk;
        public int chunkCountX;
        public int chunkCountZ;
        public float baseHeightMeters;
    }

    [Serializable]
    public sealed class MapLayoutConfig
    {
        public string mainPathGroundId;
        public float mainPathWidthMeters;
        public float minimumPathWidthMeters;
        public float edgeKeepoutMeters;
        public float spawnSafeRadiusMeters;
        public float combatClearRadiusMeters;
        public float enemySpawnMinDistanceMeters;
    }

    [Serializable]
    public sealed class MapPopulationConfig
    {
        public int initialEnemyCount;
    }

    [Serializable]
    public sealed class MapSpawnConfig
    {
        public float playerOriginX;
        public float playerOriginZ;
        public float playerSpacingMeters;
        public float actorHeightOffsetMeters;
        public float enemyOriginX;
        public float enemyOriginZ;
    }

    [Serializable]
    public sealed class MapBiomeRegionConfig
    {
        public string biomeId;
        public float minX;
        public float minZ;
        public float maxX;
        public float maxZ;
    }
}
