using System;
using System.Collections.Generic;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapConfigValidator
    {
        public static void Validate(CombatMapConfigSet config)
        {
            if (config == null || config.map == null || config.map.geometry == null ||
                config.map.resourcePersistence == null || config.map.layout == null || config.map.movement == null || config.map.drops == null || config.map.dropMerge == null || config.map.treeHarvest == null ||
                config.map.mining == null || config.map.gatherTools == null || config.map.gatherTools.tools == null ||
                config.map.gatherToolUpgrade == null || config.map.gatherToolUpgrade.levels == null || config.map.gatherToolDurabilityHud == null ||
                config.map.interactionHud == null || config.map.interactionFailureHud == null || config.map.gatherOutcomeHud == null || config.map.pickupHud == null || config.map.pickupFeedbackHud == null || config.map.interactionHighlight == null || config.map.resourceStatusHud == null || config.map.inventoryPanel == null || config.map.inventoryDrop == null || config.map.inventoryDrop.items == null ||
                config.map.worldSaveHud == null || config.map.inventoryCapacity == null || config.map.inventoryCapacity.items == null ||
                config.map.inventoryCapacityUpgrade == null || config.map.inventoryCapacityUpgrade.levels == null ||
                config.map.population == null || config.map.spawn == null ||
                config.biomes == null || config.grounds == null || config.objects == null ||
                config.map.biomeIds == null || config.map.biomeRegions == null)
                throw new InvalidOperationException("Map configuration is missing required sections.");
            var map = config.map;
            var persistence = map.resourcePersistence;
            Id(persistence.saveSlotId, "resourcePersistence.saveSlotId");
            var slot = persistence.saveSlotId;
            if (slot == "con" || slot == "prn" || slot == "aux" || slot == "nul" ||
                (slot.Length == 4 && (slot.StartsWith("com", StringComparison.Ordinal) || slot.StartsWith("lpt", StringComparison.Ordinal)) &&
                 slot[3] >= '1' && slot[3] <= '9'))
                throw new InvalidOperationException("resourcePersistence.saveSlotId cannot be a reserved file name.");
            Positive(persistence.saveIntervalSeconds, "resourcePersistence.saveIntervalSeconds");
            Positive(persistence.manualSaveCooldownSeconds, "resourcePersistence.manualSaveCooldownSeconds");
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
            HudLabel(hud.noSpaceLabel, "interactionHud.noSpaceLabel");
            ValidateInteractionFailureHud(map.interactionFailureHud);
            ValidateGatherOutcomeHud(map.gatherOutcomeHud);
            ValidatePickupHud(map.pickupHud, hud);
            ValidatePickupFeedbackHud(map.pickupFeedbackHud);
            ValidateInteractionHighlight(map.interactionHighlight);
            ValidateResourceStatusHud(map.resourceStatusHud, map.pickupHud);
            ValidateWorldSaveHud(map.worldSaveHud, map.resourceStatusHud);
            ValidateInventoryPanel(map.inventoryPanel);
            ValidateInventoryCapacity(map.inventoryCapacity);
            ValidateInventoryCapacityUpgrade(map.inventoryCapacityUpgrade, map.inventoryCapacity);
            ValidateInventoryDrop(map.inventoryDrop);
            Id(map.mapDefinitionId, "mapDefinitionId");
            if (map.schemaVersion != 42 || map.configRevision < 1 || map.defaultSeed < 1)
                throw new InvalidOperationException("Map requires schemaVersion=42, positive revision and seed.");
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
            ValidateDropMerge(map.dropMerge);
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
            Positive(gatheringTools.repairFeedbackSeconds, "gatherTools.repairFeedbackSeconds");
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
                if (tool.repairDurability <= 0 || tool.repairDurability > tool.maxDurability ||
                    tool.repairWoodQuantity < 0 || tool.repairStoneQuantity < 0 ||
                    (long)tool.repairWoodQuantity + tool.repairStoneQuantity == 0)
                    throw new InvalidOperationException("gatherTools repair requires recovery in (0,maximum], nonnegative materials and a nonzero cost for " + tool.toolId);
            }
            ValidateGatherToolUpgrade(map.gatherToolUpgrade, gatheringTools, treeHarvest, mining);
            ValidateGatherToolDurabilityHud(map.gatherToolDurabilityHud);
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

        private static void ValidateResourceStatusHud(MapResourceStatusHudConfig status, MapPickupHudConfig pickup)
        {
            Positive(status.panelWidthPixels, "resourceStatusHud.panelWidthPixels");
            Positive(status.panelHeightPixels, "resourceStatusHud.panelHeightPixels");
            Nonnegative(status.bottomMarginPixels, "resourceStatusHud.bottomMarginPixels");
            if (status.fontSize <= 0 || status.panelWidthPixels <= 32f || status.panelWidthPixels > 1920f ||
                status.panelHeightPixels < (double)status.fontSize + 32d ||
                (double)status.panelHeightPixels + status.bottomMarginPixels > 1080d ||
                status.bottomMarginPixels < (double)pickup.bottomMarginPixels + pickup.panelHeightPixels + 16d)
                throw new InvalidOperationException("resourceStatusHud requires positive font size, width in (32,1920], " +
                    "height >= fontSize+32, height+bottom margin <= 1080, and bottom margin at least 16 pixels above the G panel.");
            HudLabel(status.availableLabel, "resourceStatusHud.availableLabel");
            HudLabel(status.workingLabel, "resourceStatusHud.workingLabel");
            HudLabel(status.occupiedLabel, "resourceStatusHud.occupiedLabel");
            HudLabel(status.regrowingLabel, "resourceStatusHud.regrowingLabel");
            HudLabel(status.waitingLabel, "resourceStatusHud.waitingLabel");
            HudLabel(status.depletedLabel, "resourceStatusHud.depletedLabel");
        }

        private static void ValidateWorldSaveHud(MapWorldSaveHudConfig hud, MapResourceStatusHudConfig status)
        {
            Positive(hud.panelWidthPixels, "worldSaveHud.panelWidthPixels");
            Positive(hud.panelHeightPixels, "worldSaveHud.panelHeightPixels");
            Nonnegative(hud.bottomMarginPixels, "worldSaveHud.bottomMarginPixels");
            Positive(hud.feedbackSeconds, "worldSaveHud.feedbackSeconds");
            if (hud.fontSize <= 0 || hud.panelWidthPixels <= 32f || hud.panelWidthPixels > 1920f ||
                hud.panelHeightPixels < 2d * hud.fontSize + 40d ||
                (double)hud.panelHeightPixels + hud.bottomMarginPixels > 1080d ||
                hud.bottomMarginPixels < (double)status.bottomMarginPixels + status.panelHeightPixels + 16d)
                throw new InvalidOperationException("worldSaveHud requires positive font size, width in (32,1920], " +
                    "height >= 2*fontSize+40, height+bottom margin <= 1080, and bottom margin at least 16 pixels above the resource status panel.");
            HudLabel(hud.disabledLabel, "worldSaveHud.disabledLabel");
            HudLabel(hud.notSavedLabel, "worldSaveHud.notSavedLabel");
            HudLabel(hud.savedLabel, "worldSaveHud.savedLabel");
            HudLabel(hud.captureFailedLabel, "worldSaveHud.captureFailedLabel");
            HudLabel(hud.saveFailedLabel, "worldSaveHud.saveFailedLabel");
            HudLabel(hud.manualSaveLabel, "worldSaveHud.manualSaveLabel");
            HudLabel(hud.manualDisabledLabel, "worldSaveHud.manualDisabledLabel");
            HudLabel(hud.cooldownLabel, "worldSaveHud.cooldownLabel");
            HudLabel(hud.unavailableLabel, "worldSaveHud.unavailableLabel");
            HighlightColor(hud.errorColorHex, "worldSaveHud.errorColorHex");
        }

        private static void ValidateInteractionHighlight(MapInteractionHighlightConfig highlight)
        {
            HighlightRadius(highlight.gatherRadiusMeters, "interactionHighlight.gatherRadiusMeters");
            HighlightRadius(highlight.treeRadiusMeters, "interactionHighlight.treeRadiusMeters");
            HighlightRadius(highlight.mineRadiusMeters, "interactionHighlight.mineRadiusMeters");
            HighlightRadius(highlight.dropRadiusMeters, "interactionHighlight.dropRadiusMeters");
            Finite(highlight.lineWidthPixels, "interactionHighlight.lineWidthPixels");
            Finite(highlight.opacity, "interactionHighlight.opacity");
            Finite(highlight.heightOffsetMeters, "interactionHighlight.heightOffsetMeters");
            if (highlight.lineWidthPixels < 1f || highlight.lineWidthPixels > 8f ||
                highlight.segmentCount < 32 || highlight.segmentCount > 96 ||
                highlight.opacity <= 0f || highlight.opacity > 1f ||
                highlight.heightOffsetMeters < 0f || highlight.heightOffsetMeters > 1f)
                throw new InvalidOperationException("interactionHighlight requires line width in [1,8], " +
                    "integer segments in [32,96], opacity in (0,1], and height offset in [0,1].");
            HighlightColor(highlight.readyColorHex, "interactionHighlight.readyColorHex");
            HighlightColor(highlight.workingColorHex, "interactionHighlight.workingColorHex");
            HighlightColor(highlight.pickupColorHex, "interactionHighlight.pickupColorHex");
        }

        private static void HighlightRadius(float value, string field)
        {
            Positive(value, field);
            if (value > 5f) throw new InvalidOperationException(field + " must be in (0,5].");
        }

        private static void ValidateGatherToolDurabilityHud(MapGatherToolDurabilityHudConfig hud)
        {
            Positive(hud.warningRatio, "gatherToolDurabilityHud.warningRatio");
            Positive(hud.criticalRatio, "gatherToolDurabilityHud.criticalRatio");
            if (hud.warningRatio >= 1f || hud.criticalRatio >= hud.warningRatio)
                throw new InvalidOperationException("gatherToolDurabilityHud requires 0 < criticalRatio < warningRatio < 1.");
            HighlightColor(hud.warningColorHex, "gatherToolDurabilityHud.warningColorHex");
            HighlightColor(hud.criticalColorHex, "gatherToolDurabilityHud.criticalColorHex");
            HighlightColor(hud.brokenColorHex, "gatherToolDurabilityHud.brokenColorHex");
            HudLabel(hud.warningLabel, "gatherToolDurabilityHud.warningLabel");
            HudLabel(hud.criticalLabel, "gatherToolDurabilityHud.criticalLabel");
            HudLabel(hud.brokenLabel, "gatherToolDurabilityHud.brokenLabel");
            HudLabel(hud.remainingUsesLabel, "gatherToolDurabilityHud.remainingUsesLabel");
            HudLabel(hud.repairHintLabel, "gatherToolDurabilityHud.repairHintLabel");
        }

        private static void ValidateInteractionFailureHud(MapInteractionFailureHudConfig hud)
        {
            Positive(hud.feedbackSeconds, "interactionFailureHud.feedbackSeconds");
            HighlightColor(hud.errorColorHex, "interactionFailureHud.errorColorHex");
            HudLabel(hud.alreadyInteractingLabel, "interactionFailureHud.alreadyInteractingLabel");
            HudLabel(hud.movingLabel, "interactionFailureHud.movingLabel");
            HudLabel(hud.attackingLabel, "interactionFailureHud.attackingLabel");
            HudLabel(hud.noTargetLabel, "interactionFailureHud.noTargetLabel");
            HudLabel(hud.targetUnavailableLabel, "interactionFailureHud.targetUnavailableLabel");
            HudLabel(hud.failedLabel, "interactionFailureHud.failedLabel");
        }

        private static void ValidateGatherOutcomeHud(MapGatherOutcomeHudConfig hud)
        {
            Positive(hud.feedbackSeconds, "gatherOutcomeHud.feedbackSeconds");
            HighlightColor(hud.completedColorHex, "gatherOutcomeHud.completedColorHex");
            HighlightColor(hud.interruptedColorHex, "gatherOutcomeHud.interruptedColorHex");
            HighlightColor(hud.failedColorHex, "gatherOutcomeHud.failedColorHex");
            HudLabel(hud.gatherCompletedLabel, "gatherOutcomeHud.gatherCompletedLabel");
            HudLabel(hud.treeCompletedLabel, "gatherOutcomeHud.treeCompletedLabel");
            HudLabel(hud.mineCompletedLabel, "gatherOutcomeHud.mineCompletedLabel");
            HudLabel(hud.movingLabel, "gatherOutcomeHud.movingLabel");
            HudLabel(hud.attackingLabel, "gatherOutcomeHud.attackingLabel");
            HudLabel(hud.hitLabel, "gatherOutcomeHud.hitLabel");
            HudLabel(hud.outOfRangeLabel, "gatherOutcomeHud.outOfRangeLabel");
            HudLabel(hud.failedLabel, "gatherOutcomeHud.failedLabel");
        }

        private static void HighlightColor(string value, string field)
        {
            if (value == null || value.Length != 7 || value[0] != '#')
                throw new InvalidOperationException(field + " requires #RRGGBB.");
            for (var i = 1; i < value.Length; i++)
            {
                var c = value[i];
                if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f') && !(c >= 'A' && c <= 'F'))
                    throw new InvalidOperationException(field + " requires #RRGGBB.");
            }
        }

        private static void ValidateDropMerge(MapDropMergeConfig merge)
        {
            Positive(merge.mergeDistanceMeters, "dropMerge.mergeDistanceMeters");
            Positive(merge.scanIntervalSeconds, "dropMerge.scanIntervalSeconds");
            if (merge.maxStackQuantity <= 0)
                throw new InvalidOperationException("dropMerge.maxStackQuantity must be a positive integer.");
        }

        private static void ValidatePickupFeedbackHud(MapPickupFeedbackHudConfig hud)
        {
            Positive(hud.feedbackSeconds, "pickupFeedbackHud.feedbackSeconds");
            HighlightColor(hud.successColorHex, "pickupFeedbackHud.successColorHex");
            HighlightColor(hud.failureColorHex, "pickupFeedbackHud.failureColorHex");
            HudLabel(hud.successLabel, "pickupFeedbackHud.successLabel");
            HudLabel(hud.movingLabel, "pickupFeedbackHud.movingLabel");
            HudLabel(hud.attackingLabel, "pickupFeedbackHud.attackingLabel");
            HudLabel(hud.noTargetLabel, "pickupFeedbackHud.noTargetLabel");
            HudLabel(hud.failedLabel, "pickupFeedbackHud.failedLabel");
        }

        private static void ValidatePickupHud(MapPickupHudConfig pickup, MapInteractionHudConfig interaction)
        {
            Positive(pickup.panelWidthPixels, "pickupHud.panelWidthPixels");
            Positive(pickup.panelHeightPixels, "pickupHud.panelHeightPixels");
            Nonnegative(pickup.bottomMarginPixels, "pickupHud.bottomMarginPixels");
            Positive(pickup.expiryWarningSeconds, "pickupHud.expiryWarningSeconds");
            var minimumHeight = pickup.lifetimeEnabled ? 2d * pickup.fontSize + 40d : (double)pickup.fontSize + 32d;
            if (pickup.fontSize <= 0 || pickup.panelWidthPixels <= 32f || pickup.panelWidthPixels > 1920f ||
                pickup.panelHeightPixels < minimumHeight ||
                (double)pickup.panelHeightPixels + pickup.bottomMarginPixels > 1080d ||
                pickup.bottomMarginPixels < (double)interaction.bottomMarginPixels + interaction.panelHeightPixels + 16d)
                throw new InvalidOperationException("pickupHud requires positive font size, width in (32,1920], " +
                    "height >= 2*fontSize+40 with lifetime enabled (otherwise fontSize+32), " +
                    "height+bottom margin <= 1080, and bottom margin at least 16 pixels above the F panel.");
            HudLabel(pickup.pickupLabel, "pickupHud.pickupLabel");
            HudLabel(pickup.appleLabel, "pickupHud.appleLabel");
            HudLabel(pickup.woodLabel, "pickupHud.woodLabel");
            HudLabel(pickup.stoneLabel, "pickupHud.stoneLabel");
            HudLabel(pickup.noSpaceLabel, "pickupHud.noSpaceLabel");
            HudLabel(pickup.expiresInLabel, "pickupHud.expiresInLabel");
            HudLabel(pickup.permanentLabel, "pickupHud.permanentLabel");
            HudLabel(pickup.expiringSoonLabel, "pickupHud.expiringSoonLabel");
            HudLabel(pickup.secondsLabel, "pickupHud.secondsLabel");
            HighlightColor(pickup.expiryWarningColorHex, "pickupHud.expiryWarningColorHex");
        }

        private static void ValidateInventoryDrop(MapInventoryDropConfig drop)
        {
            if (drop.singleDropQuantity <= 0 || drop.items.Length < 1 || drop.items.Length > 3)
                throw new InvalidOperationException("inventoryDrop requires a positive single quantity and 1..3 item definitions.");
            Positive(drop.feedbackSeconds, "inventoryDrop.feedbackSeconds");
            HudLabel(drop.dropLabel, "inventoryDrop.dropLabel");
            HudLabel(drop.dropAllLabel, "inventoryDrop.dropAllLabel");
            HudLabel(drop.unavailableLabel, "inventoryDrop.unavailableLabel");
            HudLabel(drop.successLabel, "inventoryDrop.successLabel");
            HudLabel(drop.rejectedLabel, "inventoryDrop.rejectedLabel");
            HudLabel(drop.failureLabel, "inventoryDrop.failureLabel");
            var items = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in drop.items)
            {
                if (item == null) throw new InvalidOperationException("inventoryDrop.items does not allow null entries.");
                Unique(items, item.itemId, "inventoryDrop.items.itemId");
                CombatPrototypeMapInventoryDropUtility.ResolveKind(item.itemId);
                CombatPrototypeMapYieldItemResolver.Resolve(item.itemId);
                Id(item.visualResourceKey, "inventoryDrop.items.visualResourceKey");
            }
        }

        private static void ValidateInventoryCapacity(MapInventoryCapacityConfig capacity)
        {
            if (capacity.maxTotalQuantity <= 0 || capacity.items.Length != 3)
                throw new InvalidOperationException("inventoryCapacity requires a positive total and exactly vitality_apple, wood and stone.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in capacity.items)
            {
                if (item == null) throw new InvalidOperationException("inventoryCapacity.items does not allow null entries.");
                Unique(ids, item.itemId, "inventoryCapacity.items.itemId");
                CombatPrototypeMapYieldItemResolver.Resolve(item.itemId);
                if (item.maxQuantity <= 0)
                    throw new InvalidOperationException("inventoryCapacity.items.maxQuantity must be positive for " + item.itemId);
            }
        }

        private static void ValidateInventoryCapacityUpgrade(MapInventoryCapacityUpgradeConfig upgrade, MapInventoryCapacityConfig capacity)
        {
            Positive(upgrade.feedbackSeconds, "inventoryCapacityUpgrade.feedbackSeconds");
            HudLabel(upgrade.upgradeLabel, "inventoryCapacityUpgrade.upgradeLabel");
            HudLabel(upgrade.upgradeButtonLabel, "inventoryCapacityUpgrade.upgradeButtonLabel");
            HudLabel(upgrade.levelLabel, "inventoryCapacityUpgrade.levelLabel");
            HudLabel(upgrade.maxLevelLabel, "inventoryCapacityUpgrade.maxLevelLabel");
            HudLabel(upgrade.successLabel, "inventoryCapacityUpgrade.successLabel");
            HudLabel(upgrade.rejectedLabel, "inventoryCapacityUpgrade.rejectedLabel");
            HudLabel(upgrade.failureLabel, "inventoryCapacityUpgrade.failureLabel");
            if (upgrade.levels.Length != 2)
                throw new InvalidOperationException("inventoryCapacityUpgrade requires exactly levels 2 and 3.");
            var levels = new HashSet<int>();
            foreach (var level in upgrade.levels)
                if (level == null || level.level < 2 || level.level > 3 || !levels.Add(level.level) || level.items == null)
                    throw new InvalidOperationException("inventoryCapacityUpgrade requires unique nonnull levels 2 and 3 with items.");
            var previousTotal = capacity.maxTotalQuantity;
            var previousItems = capacity.items;
            for (var number = 2; number <= 3; number++)
            {
                MapInventoryCapacityUpgradeLevelConfig level = null;
                foreach (var entry in upgrade.levels) if (entry.level == number) { level = entry; break; }
                if (level == null) throw new InvalidOperationException("Missing capacity upgrade level=" + number);
                if (level.maxTotalQuantity <= previousTotal || level.woodQuantity <= 0 || level.stoneQuantity <= 0 || level.items.Length != 3)
                    throw new InvalidOperationException("Capacity upgrade level=" + number + " requires increasing total, positive wood/stone and exactly three materials.");
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in level.items)
                {
                    if (item == null) throw new InvalidOperationException("Null capacity upgrade material at level=" + number);
                    Unique(ids, item.itemId, "inventoryCapacityUpgrade.levels.items.itemId");
                    CombatPrototypeMapYieldItemResolver.Resolve(item.itemId);
                    foreach (var previous in previousItems)
                        if (previous.itemId == item.itemId && item.maxQuantity <= previous.maxQuantity)
                            throw new InvalidOperationException("Capacity upgrade material must increase; level=" + number + ", item=" + item.itemId);
                }
                previousTotal = level.maxTotalQuantity;
                previousItems = level.items;
            }
        }

        private static void ValidateGatherToolUpgrade(MapGatherToolUpgradeConfig upgrade, MapGatherToolsConfig tools,
            MapTreeHarvestConfig tree, MapMiningConfig mining)
        {
            Positive(upgrade.feedbackSeconds, "gatherToolUpgrade.feedbackSeconds");
            HudLabel(upgrade.upgradeLabel, "gatherToolUpgrade.upgradeLabel");
            HudLabel(upgrade.upgradeButtonLabel, "gatherToolUpgrade.upgradeButtonLabel");
            HudLabel(upgrade.levelLabel, "gatherToolUpgrade.levelLabel");
            HudLabel(upgrade.maxLevelLabel, "gatherToolUpgrade.maxLevelLabel");
            HudLabel(upgrade.successLabel, "gatherToolUpgrade.successLabel");
            HudLabel(upgrade.rejectedLabel, "gatherToolUpgrade.rejectedLabel");
            HudLabel(upgrade.failureLabel, "gatherToolUpgrade.failureLabel");
            HudLabel(upgrade.recraftLabel, "gatherToolUpgrade.recraftLabel");
            if (upgrade.levels.Length != 4)
                throw new InvalidOperationException("gatherToolUpgrade requires levels 2 and 3 for stone_axe and stone_pickaxe.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in upgrade.levels)
            {
                if (entry == null || entry.level < 2 || entry.level > 3)
                    throw new InvalidOperationException("gatherToolUpgrade requires nonnull levels 2 and 3.");
                CombatPrototypeMapGatherToolUtility.ResolveKind(entry.toolId);
                if (!keys.Add(entry.toolId + "." + entry.level))
                    throw new InvalidOperationException("Duplicate gatherToolUpgrade tool/level: " + entry.toolId + "/" + entry.level);
            }
            foreach (var tool in tools.tools)
            {
                var previousMaximum = tool.maxDurability;
                var previousMultiplier = tool.durationMultiplier;
                var baseDuration = tool.targetKind == "tree" ? tree.harvestDurationSeconds : mining.harvestDurationSeconds;
                for (var number = 2; number <= 3; number++)
                {
                    MapGatherToolUpgradeLevelConfig tier = null;
                    foreach (var entry in upgrade.levels)
                        if (entry.toolId == tool.toolId && entry.level == number) { tier = entry; break; }
                    if (tier == null) throw new InvalidOperationException("Missing gatherToolUpgrade tool/level: " + tool.toolId + "/" + number);
                    if (tier.maxDurability <= previousMaximum || tier.woodQuantity <= 0 || tier.stoneQuantity <= 0)
                        throw new InvalidOperationException("Tool upgrade requires increasing maximum and positive wood/stone: " + tool.toolId + "/" + number);
                    Positive(tier.durationMultiplier, "gatherToolUpgrade.durationMultiplier");
                    if (tier.durationMultiplier >= previousMultiplier || tier.durationMultiplier > 1f)
                        throw new InvalidOperationException("Tool upgrade durationMultiplier must strictly decrease and be <= 1: " + tool.toolId + "/" + number);
                    Positive(baseDuration * tier.durationMultiplier, "gatherToolUpgrade.actualDuration");
                    previousMaximum = tier.maxDurability;
                    previousMultiplier = tier.durationMultiplier;
                }
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
            HudLabel(panel.capacityLabel, "inventoryPanel.capacityLabel");
            HudLabel(panel.unlimitedLabel, "inventoryPanel.unlimitedLabel");
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
            HudLabel(panel.repairLabel, "inventoryPanel.repairLabel");
            HudLabel(panel.repairButtonLabel, "inventoryPanel.repairButtonLabel");
            HudLabel(panel.fullDurabilityLabel, "inventoryPanel.fullDurabilityLabel");
            CombatPrototypeMapInventoryPanelListView.ResolveSortMode(panel.defaultSortMode);
            CombatPrototypeMapInventoryPanelListView.ResolveFilterMode(panel.defaultFilterMode);
            HudLabel(panel.sortLabel, "inventoryPanel.sortLabel");
            HudLabel(panel.originalOrderLabel, "inventoryPanel.originalOrderLabel");
            HudLabel(panel.typeOrderLabel, "inventoryPanel.typeOrderLabel");
            HudLabel(panel.quantityOrderLabel, "inventoryPanel.quantityOrderLabel");
            HudLabel(panel.filterLabel, "inventoryPanel.filterLabel");
            HudLabel(panel.allFilterLabel, "inventoryPanel.allFilterLabel");
            HudLabel(panel.resourcesFilterLabel, "inventoryPanel.resourcesFilterLabel");
            HudLabel(panel.suppliesFilterLabel, "inventoryPanel.suppliesFilterLabel");
            HudLabel(panel.otherFilterLabel, "inventoryPanel.otherFilterLabel");
            HudLabel(panel.noMatchingItemsLabel, "inventoryPanel.noMatchingItemsLabel");
            if (panel.searchMaxLength < 1 || panel.searchMaxLength > 64)
                throw new InvalidOperationException("inventoryPanel.searchMaxLength requires an integer from 1 to 64.");
            HudLabel(panel.searchLabel, "inventoryPanel.searchLabel");
            HudLabel(panel.searchPlaceholderLabel, "inventoryPanel.searchPlaceholderLabel");
            HudLabel(panel.clearSearchLabel, "inventoryPanel.clearSearchLabel");
            HudLabel(panel.noSearchResultsLabel, "inventoryPanel.noSearchResultsLabel");
            Id(panel.preferencesFileId, "inventoryPanel.preferencesFileId");
            Positive(panel.preferencesSaveDelaySeconds, "inventoryPanel.preferencesSaveDelaySeconds");
            HudLabel(panel.preferencesResetLabel, "inventoryPanel.preferencesResetLabel");
            HudLabel(panel.detailsButtonLabel, "inventoryPanel.detailsButtonLabel");
            HudLabel(panel.detailsTitleLabel, "inventoryPanel.detailsTitleLabel");
            HudLabel(panel.detailsCloseLabel, "inventoryPanel.detailsCloseLabel");
            HudLabel(panel.detailsQuantityLabel, "inventoryPanel.detailsQuantityLabel");
            HudLabel(panel.detailsDescriptionLabel, "inventoryPanel.detailsDescriptionLabel");
            HudLabel(panel.detailsUsageLabel, "inventoryPanel.detailsUsageLabel");
            HudLabel(panel.detailsNoUsageLabel, "inventoryPanel.detailsNoUsageLabel");
            HudLabel(panel.detailsUnknownDescriptionLabel, "inventoryPanel.detailsUnknownDescriptionLabel");
            HudLabel(panel.woodDescriptionLabel, "inventoryPanel.woodDescriptionLabel");
            HudLabel(panel.stoneDescriptionLabel, "inventoryPanel.stoneDescriptionLabel");
            HudLabel(panel.appleDescriptionLabel, "inventoryPanel.appleDescriptionLabel");
            HudLabel(panel.meatDescriptionLabel, "inventoryPanel.meatDescriptionLabel");
            HudLabel(panel.meatUsageLabel, "inventoryPanel.meatUsageLabel");
            if (panel.favoritesMaxCount < 1 || panel.favoritesMaxCount > CombatPrototypeMapInventoryPanelFavorites.MaximumStoredCount)
                throw new InvalidOperationException("inventoryPanel.favoritesMaxCount requires an integer from 1 to 256.");
            HudLabel(panel.favoriteButtonLabel, "inventoryPanel.favoriteButtonLabel");
            HudLabel(panel.unfavoriteButtonLabel, "inventoryPanel.unfavoriteButtonLabel");
            HudLabel(panel.favoriteTagLabel, "inventoryPanel.favoriteTagLabel");
            HudLabel(panel.favoritesFullLabel, "inventoryPanel.favoritesFullLabel");
            HudLabel(panel.favoritesFilterLabel, "inventoryPanel.favoritesFilterLabel");
            HudLabel(panel.favoritesFilterAllLabel, "inventoryPanel.favoritesFilterAllLabel");
            HudLabel(panel.favoritesFilterOnlyLabel, "inventoryPanel.favoritesFilterOnlyLabel");
            HudLabel(panel.noMatchingFavoritesLabel, "inventoryPanel.noMatchingFavoritesLabel");
            HudLabel(panel.favoritesCountLabel, "inventoryPanel.favoritesCountLabel");
            HudLabel(panel.favoritesProtectedLabel, "inventoryPanel.favoritesProtectedLabel");
            HudLabel(panel.favoritesConsumptionHintLabel, "inventoryPanel.favoritesConsumptionHintLabel");
            HudLabel(panel.favoritesConsumptionConfirmationLabel, "inventoryPanel.favoritesConsumptionConfirmationLabel");
            HudLabel(panel.favoritesConsumptionConfirmLabel, "inventoryPanel.favoritesConsumptionConfirmLabel");
            HudLabel(panel.favoritesConsumptionCancelLabel, "inventoryPanel.favoritesConsumptionCancelLabel");
            CombatPrototypeMapInventoryRecipeFilter.ResolveMode(panel.defaultRecipeFilterMode);
            HudLabel(panel.recipeFilterLabel, "inventoryPanel.recipeFilterLabel");
            HudLabel(panel.allRecipesLabel, "inventoryPanel.allRecipesLabel");
            HudLabel(panel.craftRecipesLabel, "inventoryPanel.craftRecipesLabel");
            HudLabel(panel.repairRecipesLabel, "inventoryPanel.repairRecipesLabel");
            HudLabel(panel.upgradeRecipesLabel, "inventoryPanel.upgradeRecipesLabel");
            if (panel.recipeSearchMaxLength < 1 || panel.recipeSearchMaxLength > 64)
                throw new InvalidOperationException("inventoryPanel.recipeSearchMaxLength requires an integer from 1 to 64.");
            HudLabel(panel.recipeSearchLabel, "inventoryPanel.recipeSearchLabel");
            HudLabel(panel.recipeSearchPlaceholderLabel, "inventoryPanel.recipeSearchPlaceholderLabel");
            HudLabel(panel.clearRecipeSearchLabel, "inventoryPanel.clearRecipeSearchLabel");
            HudLabel(panel.noRecipeSearchResultsLabel, "inventoryPanel.noRecipeSearchResultsLabel");
            if (panel.recipeFavoritesMaxCount < 1 || panel.recipeFavoritesMaxCount > CombatPrototypeMapInventoryRecipeFavorites.MaximumStoredCount)
                throw new InvalidOperationException("inventoryPanel.recipeFavoritesMaxCount requires an integer from 1 to 7.");
            HudLabel(panel.recipeFavoriteButtonLabel, "inventoryPanel.recipeFavoriteButtonLabel");
            HudLabel(panel.recipeUnfavoriteButtonLabel, "inventoryPanel.recipeUnfavoriteButtonLabel");
            HudLabel(panel.recipeFavoriteTagLabel, "inventoryPanel.recipeFavoriteTagLabel");
            HudLabel(panel.recipeFavoritesFullLabel, "inventoryPanel.recipeFavoritesFullLabel");
            HudLabel(panel.favoriteRecipesLabel, "inventoryPanel.favoriteRecipesLabel");
            HudLabel(panel.otherRecipesLabel, "inventoryPanel.otherRecipesLabel");
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
