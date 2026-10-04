using System;
using System.Collections.Generic;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapConfigValidator
    {
        public static void Validate(CombatMapConfigSet config)
        {
            if (config == null || config.map == null || config.map.geometry == null ||
                config.map.layout == null || config.map.movement == null || config.map.drops == null || config.map.treeHarvest == null ||
                config.map.mining == null || config.map.gatherTools == null || config.map.gatherTools.tools == null ||
                config.map.interactionHud == null || config.map.inventoryPanel == null || config.map.population == null || config.map.spawn == null ||
                config.biomes == null || config.grounds == null || config.objects == null ||
                config.map.biomeIds == null || config.map.biomeRegions == null)
                throw new InvalidOperationException("Map configuration is missing required sections.");
            var map = config.map;
            var hud = map.interactionHud;
            Positive(hud.panelWidthPixels, "interactionHud.panelWidthPixels");
            Positive(hud.panelHeightPixels, "interactionHud.panelHeightPixels");
            Nonnegative(hud.bottomMarginPixels, "interactionHud.bottomMarginPixels");
            Positive(hud.progressBarHeightPixels, "interactionHud.progressBarHeightPixels");
            if (hud.fontSize <= 0 || hud.panelWidthPixels <= 32f || hud.panelWidthPixels > 1920f ||
                hud.panelHeightPixels + hud.bottomMarginPixels > 1080f ||
                hud.panelHeightPixels < 2d * hud.fontSize + hud.progressBarHeightPixels + 54d)
                throw new InvalidOperationException("interactionHud requires a positive font size, panel width in (32,1920], " +
                    "panel height >= 2*fontSize + progressBarHeightPixels + 54, and panel height + bottom margin <= 1080.");
            HudLabel(hud.gatherLabel, "interactionHud.gatherLabel");
            HudLabel(hud.treeLabel, "interactionHud.treeLabel");
            HudLabel(hud.mineLabel, "interactionHud.mineLabel");
            ValidateInventoryPanel(map.inventoryPanel);
            Id(map.mapDefinitionId, "mapDefinitionId");
            if (map.schemaVersion != 9 || map.configRevision < 1 || map.defaultSeed < 1)
                throw new InvalidOperationException("Map requires schemaVersion=9, positive revision and seed.");
            var drops = map.drops;
            Id(drops.itemId, "drops.itemId");
            Id(drops.visualResourceKey, "drops.visualResourceKey");
            CombatPrototypeMapYieldItemResolver.Resolve(drops.itemId);
            if (drops.quantity <= 0)
                throw new InvalidOperationException("drops.quantity must be a positive integer.");
            Positive(drops.pickupDistanceMeters, "drops.pickupDistanceMeters");
            Positive(drops.flightDurationSeconds, "drops.flightDurationSeconds");
            Nonnegative(drops.scatterRadiusMeters, "drops.scatterRadiusMeters");
            Nonnegative(drops.arcHeightMeters, "drops.arcHeightMeters");
            Nonnegative(drops.groundOffsetMeters, "drops.groundOffsetMeters");
            Positive(drops.visualScale, "drops.visualScale");
            Nonnegative(drops.lifetimeSeconds, "drops.lifetimeSeconds");
            var treeHarvest = map.treeHarvest;
            Id(treeHarvest.treeObjectId, "treeHarvest.treeObjectId");
            Id(treeHarvest.visualResourceKey, "treeHarvest.visualResourceKey");
            Positive(treeHarvest.harvestDurationSeconds, "treeHarvest.harvestDurationSeconds");
            Id(treeHarvest.dropItemId, "treeHarvest.dropItemId");
            CombatPrototypeMapYieldItemResolver.Resolve(treeHarvest.dropItemId);
            if (treeHarvest.dropQuantity <= 0)
                throw new InvalidOperationException("treeHarvest.dropQuantity must be a positive integer.");
            Id(treeHarvest.dropVisualResourceKey, "treeHarvest.dropVisualResourceKey");
            var mining = map.mining;
            Id(mining.mineObjectId, "mining.mineObjectId");
            Id(mining.visualResourceKey, "mining.visualResourceKey");
            Positive(mining.harvestDurationSeconds, "mining.harvestDurationSeconds");
            Id(mining.dropItemId, "mining.dropItemId");
            CombatPrototypeMapYieldItemResolver.Resolve(mining.dropItemId);
            if (mining.dropQuantity <= 0)
                throw new InvalidOperationException("mining.dropQuantity must be a positive integer.");
            Id(mining.dropVisualResourceKey, "mining.dropVisualResourceKey");
            var gatheringTools = map.gatherTools;
            Positive(gatheringTools.craftFeedbackSeconds, "gatherTools.craftFeedbackSeconds");
            if (gatheringTools.tools.Length != 2)
                throw new InvalidOperationException("gatherTools requires exactly stone_axe and stone_pickaxe.");
            var toolIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tool in gatheringTools.tools)
            {
                if (tool == null) throw new InvalidOperationException("gatherTools.tools does not allow null entries.");
                Unique(toolIds, tool.toolId, "gatherTools.toolId");
                var kind = CombatPrototypeMapGatherToolUtility.ResolveKind(tool.toolId);
                var expectedTarget = kind == CombatPrototypeMapGatherToolKind.Axe ? "tree" : "mine";
                if (tool.targetKind != expectedTarget)
                    throw new InvalidOperationException("gatherTools targetKind mismatch for " + tool.toolId);
                HudLabel(tool.displayName, "gatherTools.displayName");
                if (tool.maxDurability <= 0 || tool.durabilityCostPerCompletion <= 0 ||
                    tool.durabilityCostPerCompletion > tool.maxDurability)
                    throw new InvalidOperationException("gatherTools requires 0 < durability cost <= maximum for " + tool.toolId);
                Positive(tool.durationMultiplier, "gatherTools.durationMultiplier");
                if (tool.durationMultiplier > 1f)
                    throw new InvalidOperationException("gatherTools durationMultiplier must be <= 1 for " + tool.toolId);
                Positive((kind == CombatPrototypeMapGatherToolKind.Axe ? treeHarvest.harvestDurationSeconds : mining.harvestDurationSeconds)
                    * tool.durationMultiplier, "gatherTools.actualDuration");
                if (tool.craftWoodQuantity < 0 || tool.craftStoneQuantity < 0 ||
                    (long)tool.craftWoodQuantity + tool.craftStoneQuantity == 0)
                    throw new InvalidOperationException("gatherTools recipe requires nonnegative material quantities and a nonzero cost for " + tool.toolId);
            }
            var geometry = map.geometry;
            Positive(geometry.cellSizeMeters, "cellSizeMeters");
            Finite(geometry.baseHeightMeters, "baseHeightMeters");
            if (geometry.cellsPerChunk <= 0 || geometry.chunkCountX <= 0 || geometry.chunkCountZ <= 0)
                throw new InvalidOperationException("Map grid dimensions must be positive integers.");
            var totalCells = checked((long)geometry.cellsPerChunk * geometry.cellsPerChunk *
                geometry.chunkCountX * geometry.chunkCountZ);
            if (totalCells > int.MaxValue)
                throw new InvalidOperationException("Map cell count exceeds buffer capacity.");
            var width = geometry.cellSizeMeters * geometry.cellsPerChunk * geometry.chunkCountX;
            var depth = geometry.cellSizeMeters * geometry.cellsPerChunk * geometry.chunkCountZ;
            Positive(width, "mapWidthMeters"); Positive(depth, "mapDepthMeters");
            var layout = map.layout;
            Positive(layout.mainPathWidthMeters, "mainPathWidthMeters");
            Positive(layout.minimumPathWidthMeters, "minimumPathWidthMeters");
            Nonnegative(layout.edgeKeepoutMeters, "edgeKeepoutMeters");
            Nonnegative(layout.spawnSafeRadiusMeters, "spawnSafeRadiusMeters");
            Nonnegative(layout.combatClearRadiusMeters, "combatClearRadiusMeters");
            Nonnegative(layout.enemySpawnMinDistanceMeters, "enemySpawnMinDistanceMeters");
            var halfExtent = Math.Min(width, depth) * 0.5f - layout.edgeKeepoutMeters;
            if (layout.minimumPathWidthMeters > layout.mainPathWidthMeters ||
                halfExtent <= 0f || layout.mainPathWidthMeters * 0.5f > halfExtent ||
                Math.Max(layout.spawnSafeRadiusMeters, layout.combatClearRadiusMeters) > halfExtent)
                throw new InvalidOperationException("Map layout does not fit the configured bounds.");
            var movement = map.movement;
            Positive(movement.playerRadiusMeters, "movement.playerRadiusMeters");
            Positive(movement.enemyRadiusMeters, "movement.enemyRadiusMeters");
            Positive(movement.collisionSkinMeters, "movement.collisionSkinMeters");
            if (movement.collisionSkinMeters >= Math.Min(movement.playerRadiusMeters, movement.enemyRadiusMeters))
                throw new InvalidOperationException("movement.collisionSkinMeters must be smaller than both actor radii.");
            if (movement.maxSlideIterations < 1 || movement.maxSlideIterations > 8)
                throw new InvalidOperationException("movement.maxSlideIterations must be between 1 and 8.");
            if (layout.minimumPathWidthMeters < 2f *
                (Math.Max(movement.playerRadiusMeters, movement.enemyRadiusMeters) + movement.collisionSkinMeters))
                throw new InvalidOperationException("minimumPathWidthMeters must fit the actor diameter and collision skin.");
            if (map.population.initialEnemyCount <= 0)
                throw new InvalidOperationException("initialEnemyCount must be positive.");
            var spawn = map.spawn;
            Finite(spawn.playerOriginX, "playerOriginX"); Finite(spawn.playerOriginZ, "playerOriginZ");
            Finite(spawn.enemyOriginX, "enemyOriginX"); Finite(spawn.enemyOriginZ, "enemyOriginZ");
            Nonnegative(spawn.actorHeightOffsetMeters, "actorHeightOffsetMeters");
            Positive(spawn.playerSpacingMeters, "playerSpacingMeters");
            var protectedRadius = Math.Max(layout.spawnSafeRadiusMeters, layout.combatClearRadiusMeters);
            if (Math.Abs(spawn.playerOriginX) + protectedRadius > width * 0.5f - layout.edgeKeepoutMeters ||
                Math.Abs(spawn.playerOriginZ) + protectedRadius > depth * 0.5f - layout.edgeKeepoutMeters)
                throw new InvalidOperationException("Player spawn area is outside the map.");

            var groundIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ground in config.grounds)
            {
                if (ground == null) throw new InvalidOperationException("Null ground definition.");
                Unique(groundIds, ground.groundId, "groundId");
                Id(ground.visualResourceKey, "ground.visualResourceKey");
                Positive(ground.movementMultiplier, "movementMultiplier");
            }
            Reference(groundIds, layout.mainPathGroundId, "mainPathGroundId");
            var objectIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in config.objects)
            {
                if (item == null) throw new InvalidOperationException("Null object definition.");
                Unique(objectIds, item.objectId, "objectId");
                Id(item.visualResourceKey, "object.visualResourceKey");
                Nonnegative(item.footprintRadiusMeters, "footprintRadiusMeters");
                if (item.blocksMovement && item.footprintRadiusMeters <= 0f)
                    throw new InvalidOperationException("Movement blocker requires a positive footprintRadiusMeters; objectId=" + item.objectId);
                Positive(item.minimumSameTypeSpacingMeters, "minimumSameTypeSpacingMeters");
                Nonnegative(item.interactionDistanceMeters, "interactionDistanceMeters");
                Nonnegative(item.gatherDurationSeconds, "gatherDurationSeconds");
                Nonnegative(item.regrowSeconds, "regrowSeconds");
                if (item.regrowEnabled && item.regrowSeconds <= 0f)
                    throw new InvalidOperationException("Enabled regrowth requires a positive interval.");
                if (item.gatherable)
                {
                    Id(item.yieldItemId, "yieldItemId");
                    if (item.yieldQuantity <= 0 || item.interactionDistanceMeters <= 0f || item.gatherDurationSeconds <= 0f)
                        throw new InvalidOperationException("Gatherable requires positive quantity, interaction distance and duration; objectId=" + item.objectId);
                    if (item.blocksMovement || item.blocksMelee || item.blocksProjectile)
                        throw new InvalidOperationException("Current gatherables require blocking disabled; objectId=" + item.objectId);
                    CombatPrototypeMapYieldItemResolver.Resolve(item.yieldItemId);
                }
            }
            Reference(objectIds, treeHarvest.treeObjectId, "treeHarvest.treeObjectId");
            var treeDefinition = Array.Find(config.objects, item => item.objectId == treeHarvest.treeObjectId);
            if (treeDefinition.gatherable || !treeDefinition.blocksMovement || treeDefinition.interactionDistanceMeters <= 0f ||
                !Array.Exists(config.biomes, biome => biome != null && biome.treeObjectId == treeHarvest.treeObjectId))
                throw new InvalidOperationException("treeHarvest.treeObjectId must reference a blocking, nongatherable biome tree with positive interaction distance.");
            Reference(objectIds, mining.mineObjectId, "mining.mineObjectId");
            var mineDefinition = Array.Find(config.objects, item => item.objectId == mining.mineObjectId);
            if (mineDefinition.gatherable || !mineDefinition.blocksMovement || mineDefinition.interactionDistanceMeters <= 0f ||
                mining.mineObjectId == treeHarvest.treeObjectId ||
                !Array.Exists(config.biomes, biome => biome != null && biome.mineObjectId == mining.mineObjectId))
                throw new InvalidOperationException("mining.mineObjectId requires a distinct blocking, nongatherable biome mine with positive interaction distance.");
            var biomeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var biome in config.biomes)
            {
                if (biome == null) throw new InvalidOperationException("Null biome definition.");
                Unique(biomeIds, biome.biomeId, "biomeId");
                Reference(groundIds, biome.groundId, "biome.groundId");
                Reference(objectIds, biome.decorationObjectId, "biome.decorationObjectId");
                Reference(objectIds, biome.rockObjectId, "biome.rockObjectId");
                Reference(objectIds, biome.treeObjectId, "biome.treeObjectId");
                Reference(objectIds, biome.gatherObjectId, "biome.gatherObjectId");
                Reference(objectIds, biome.mineObjectId, "biome.mineObjectId");
                if (biome.mineObjectId != mining.mineObjectId || biome.mineObjectId == biome.treeObjectId ||
                    biome.mineObjectId == biome.gatherObjectId || biome.mineObjectId == biome.decorationObjectId ||
                    biome.mineObjectId == biome.rockObjectId)
                    throw new InvalidOperationException("biome.mineObjectId must use the configured mine separately from existing content; biomeId=" + biome.biomeId);
                var gatherDefinition = Array.Find(config.objects, item => item.objectId == biome.gatherObjectId);
                if (!gatherDefinition.gatherable)
                    throw new InvalidOperationException("biome.gatherObjectId must reference a gatherable; biomeId=" + biome.biomeId);
                Nonnegative(biome.decorationDensityPer100m2, "decorationDensityPer100m2");
                Nonnegative(biome.treeDensityPer100m2, "treeDensityPer100m2");
                Nonnegative(biome.gatherableDensityPer100m2, "gatherableDensityPer100m2");
                Nonnegative(biome.rockDensityPer100m2, "rockDensityPer100m2");
                Nonnegative(biome.mineDensityPer100m2, "mineDensityPer100m2");
            }
            var enabledBiomes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in map.biomeIds)
            {
                Reference(biomeIds, id, "map.biomeIds");
                Unique(enabledBiomes, id, "map.biomeIds");
            }
            Reference(enabledBiomes, map.defaultBiomeId, "defaultBiomeId");
            foreach (var region in map.biomeRegions)
            {
                if (region == null) throw new InvalidOperationException("Null biome region.");
                Reference(enabledBiomes, region.biomeId, "region.biomeId");
                Finite(region.minX, "region.minX"); Finite(region.minZ, "region.minZ");
                Finite(region.maxX, "region.maxX"); Finite(region.maxZ, "region.maxZ");
                if (region.minX < 0f || region.minZ < 0f || region.maxX > 1f || region.maxZ > 1f ||
                    region.minX >= region.maxX || region.minZ >= region.maxZ)
                    throw new InvalidOperationException("Biome region must be a nonempty normalized rectangle.");
            }
        }

        private static void ValidateInventoryPanel(MapInventoryPanelConfig panel)
        {
            Positive(panel.panelWidthPixels, "inventoryPanel.panelWidthPixels");
            Positive(panel.panelHeightPixels, "inventoryPanel.panelHeightPixels");
            Nonnegative(panel.rightMarginPixels, "inventoryPanel.rightMarginPixels");
            Nonnegative(panel.topMarginPixels, "inventoryPanel.topMarginPixels");
            Positive(panel.rowHeightPixels, "inventoryPanel.rowHeightPixels");
            if (panel.fontSize <= 0 || panel.rowHeightPixels < (double)panel.fontSize + 8d ||
                panel.panelWidthPixels < 12d * panel.fontSize + 48d ||
                panel.panelHeightPixels < 8d * panel.rowHeightPixels + 48d ||
                (double)panel.panelWidthPixels + panel.rightMarginPixels > 1920d ||
                (double)panel.panelHeightPixels + panel.topMarginPixels > 1080d)
                throw new InvalidOperationException("inventoryPanel requires positive font size, row height >= fontSize+8, " +
                    "panel width >= 12*fontSize+48, panel height >= 8*rowHeight+48, " +
                    "width+right margin <= 1920 and height+top margin <= 1080.");
            HudLabel(panel.panelTitle, "inventoryPanel.panelTitle");
            HudLabel(panel.materialsLabel, "inventoryPanel.materialsLabel");
            HudLabel(panel.toolsLabel, "inventoryPanel.toolsLabel");
            HudLabel(panel.craftLabel, "inventoryPanel.craftLabel");
            HudLabel(panel.craftButtonLabel, "inventoryPanel.craftButtonLabel");
            HudLabel(panel.emptyInventoryLabel, "inventoryPanel.emptyInventoryLabel");
            HudLabel(panel.woodLabel, "inventoryPanel.woodLabel");
            HudLabel(panel.stoneLabel, "inventoryPanel.stoneLabel");
            HudLabel(panel.appleLabel, "inventoryPanel.appleLabel");
            HudLabel(panel.meatLabel, "inventoryPanel.meatLabel");
            HudLabel(panel.closeLabel, "inventoryPanel.closeLabel");
            HudLabel(panel.missingLabel, "inventoryPanel.missingLabel");
            HudLabel(panel.usableLabel, "inventoryPanel.usableLabel");
            HudLabel(panel.brokenLabel, "inventoryPanel.brokenLabel");
            HudLabel(panel.notOwnedLabel, "inventoryPanel.notOwnedLabel");
            HudLabel(panel.disabledLabel, "inventoryPanel.disabledLabel");
            HudLabel(panel.readyLabel, "inventoryPanel.readyLabel");
        }

        private static void HudLabel(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || global::System.Text.Encoding.UTF8.GetByteCount(value) > 61)
                throw new InvalidOperationException(field + " requires a nonempty label up to 61 UTF-8 bytes.");
            foreach (var character in value)
                if (char.IsControl(character))
                    throw new InvalidOperationException(field + " does not allow control characters.");
        }

        private static void Finite(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidOperationException(field + " must be finite.");
        }
        private static void Positive(float value, string field)
        {
            Finite(value, field);
            if (value <= 0f) throw new InvalidOperationException(field + " must be positive.");
        }
        private static void Nonnegative(float value, string field)
        {
            Finite(value, field);
            if (value < 0f) throw new InvalidOperationException(field + " must be nonnegative.");
        }
        private static void Id(string id, string field)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 61)
                throw new InvalidOperationException(field + " requires a nonempty ID up to 61 characters.");
            for (var i = 0; i < id.Length; i++)
                if (!(id[i] >= 'a' && id[i] <= 'z') && !(id[i] >= '0' && id[i] <= '9') && id[i] != '_')
                    throw new InvalidOperationException(field + " requires lowercase ASCII, digits or underscores.");
        }
        private static void Unique(HashSet<string> ids, string id, string field)
        {
            Id(id, field);
            if (!ids.Add(id)) throw new InvalidOperationException("Duplicate " + field + ": " + id);
        }
        private static void Reference(HashSet<string> ids, string id, string field)
        {
            if (id == null || !ids.Contains(id))
                throw new InvalidOperationException("Unknown " + field + ": " + id);
        }
    }
}
