using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    public sealed class CombatPrototypeMapLayout
    {
        public CombatPrototypeMapData Data;
        public readonly List<CombatPrototypeMapChunk> Chunks = new List<CombatPrototypeMapChunk>();
        public readonly List<CombatPrototypeMapCell> Cells = new List<CombatPrototypeMapCell>();
        public readonly List<CombatPrototypeMapDecoration> Decorations = new List<CombatPrototypeMapDecoration>();
        public readonly List<CombatPrototypeMapObstacle> Obstacles = new List<CombatPrototypeMapObstacle>();
    }

    public static class CombatPrototypeMapLayoutBuilder
    {
        public static CombatPrototypeMapLayout Build(CombatMapConfigSet config, int enemyColumns, float enemySpacing)
        {
            var definition = config.map;
            var geometry = definition.geometry;
            var width = geometry.cellSizeMeters * geometry.cellsPerChunk * geometry.chunkCountX;
            var depth = geometry.cellSizeMeters * geometry.cellsPerChunk * geometry.chunkCountZ;
            var height = geometry.baseHeightMeters + definition.spawn.actorHeightOffsetMeters;
            var result = new CombatPrototypeMapLayout
            {
                Data = new CombatPrototypeMapData
                {
                    MapDefinitionId = definition.mapDefinitionId, ConfigRevision = definition.configRevision,
                    Seed = (uint)definition.defaultSeed, Origin = new float2(-width * 0.5f, -depth * 0.5f),
                    Size = new float2(width, depth), CellSize = geometry.cellSizeMeters,
                    BaseHeight = geometry.baseHeightMeters, CellsPerChunk = geometry.cellsPerChunk,
                    PlayerSpawnOrigin = new float3(definition.spawn.playerOriginX, height, definition.spawn.playerOriginZ),
                    PlayerSpawnSpacing = definition.spawn.playerSpacingMeters,
                    EnemySpawnOrigin = new float3(definition.spawn.enemyOriginX, height, definition.spawn.enemyOriginZ),
                    EnemyCount = definition.population.initialEnemyCount,
                    PlayerRadius = definition.movement.playerRadiusMeters,
                    EnemyRadius = definition.movement.enemyRadiusMeters,
                    CollisionSkin = definition.movement.collisionSkinMeters,
                    MaxSlideIterations = definition.movement.maxSlideIterations
                }
            };
            ValidateEnemyGrid(result.Data, definition.layout, enemyColumns, enemySpacing);
            var biomeIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            var groundIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            var objectIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < config.biomes.Length; i++) biomeIndices.Add(config.biomes[i].biomeId, i);
            for (var i = 0; i < config.grounds.Length; i++) groundIndices.Add(config.grounds[i].groundId, i);
            for (var i = 0; i < config.objects.Length; i++) objectIndices.Add(config.objects[i].objectId, i);
            var defaultBiome = biomeIndices[definition.defaultBiomeId];
            var pathGround = groundIndices[definition.layout.mainPathGroundId];
            var random = new Unity.Mathematics.Random(result.Data.Seed);
            var spacing = new PlacementSpacing[config.objects.Length];
            for (var i = 0; i < spacing.Length; i++)
                spacing[i] = new PlacementSpacing(config.objects[i].minimumSameTypeSpacingMeters);
            var candidates = new List<int>();
            var occupancy = new PlacementOccupancy(result.Data.CellSize);
            var countPerChunk = geometry.cellsPerChunk * geometry.cellsPerChunk;

            for (var chunkZ = 0; chunkZ < geometry.chunkCountZ; chunkZ++)
            for (var chunkX = 0; chunkX < geometry.chunkCountX; chunkX++)
            {
                var chunk = new CombatPrototypeMapChunk
                {
                    Coordinate = new int2(chunkX, chunkZ), FirstCell = result.Cells.Count, CellCount = countPerChunk
                };
                result.Chunks.Add(chunk);
                for (var z = 0; z < geometry.cellsPerChunk; z++)
                for (var x = 0; x < geometry.cellsPerChunk; x++)
                {
                    var coordinate = new int2(chunkX * geometry.cellsPerChunk + x, chunkZ * geometry.cellsPerChunk + z);
                    var center = result.Data.Origin + (new float2(coordinate.x, coordinate.y) + 0.5f) * result.Data.CellSize;
                    var normalized = (center - result.Data.Origin) / result.Data.Size;
                    var biomeIndex = defaultBiome;
                    foreach (var region in definition.biomeRegions)
                        if (normalized.x >= region.minX && normalized.x < region.maxX &&
                            normalized.y >= region.minZ && normalized.y < region.maxZ)
                            biomeIndex = biomeIndices[region.biomeId];
                    var road = IsRoad(center, result.Data, definition.layout.mainPathWidthMeters * 0.5f);
                    result.Cells.Add(new CombatPrototypeMapCell
                    {
                        Coordinate = coordinate, BiomeIndex = biomeIndex,
                        GroundIndex = road ? pathGround : groundIndices[config.biomes[biomeIndex].groundId],
                        Reserved = (byte)(IsReserved(center, 0f, result.Data, definition.layout, enemyColumns, enemySpacing) ? 1 : 0)
                    });
                }
            }
            // Trees and gatherables keep their original placements; mines reserve before decorations.
            foreach (var chunk in result.Chunks)
            for (var biome = 0; biome < config.biomes.Length; biome++)
            {
                ReadCandidates(result, chunk, biome, candidates);
                var configBiome = config.biomes[biome];
                Place(configBiome.treeDensityPer100m2, objectIndices[configBiome.treeObjectId],
                    candidates, config, result, spacing, occupancy, definition.layout, enemyColumns, enemySpacing, ref random);
            }
            foreach (var chunk in result.Chunks)
            for (var biome = 0; biome < config.biomes.Length; biome++)
            {
                ReadCandidates(result, chunk, biome, candidates);
                var configBiome = config.biomes[biome];
                Place(configBiome.gatherableDensityPer100m2, objectIndices[configBiome.gatherObjectId],
                    candidates, config, result, spacing, occupancy, definition.layout, enemyColumns, enemySpacing, ref random);
            }
            if (definition.mining.enabled)
                foreach (var chunk in result.Chunks)
                for (var biome = 0; biome < config.biomes.Length; biome++)
                {
                    ReadCandidates(result, chunk, biome, candidates);
                    var configBiome = config.biomes[biome];
                    Place(configBiome.mineDensityPer100m2, objectIndices[configBiome.mineObjectId],
                        candidates, config, result, spacing, occupancy, definition.layout, enemyColumns, enemySpacing, ref random);
                }
            foreach (var chunk in result.Chunks)
            for (var biome = 0; biome < config.biomes.Length; biome++)
            {
                ReadCandidates(result, chunk, biome, candidates);
                var configBiome = config.biomes[biome];
                Place(configBiome.decorationDensityPer100m2, objectIndices[configBiome.decorationObjectId],
                    candidates, config, result, spacing, occupancy, definition.layout, enemyColumns, enemySpacing, ref random);
                Place(configBiome.rockDensityPer100m2, objectIndices[configBiome.rockObjectId],
                    candidates, config, result, spacing, occupancy, definition.layout, enemyColumns, enemySpacing, ref random);
            }
            return result;
        }

        private static void ReadCandidates(CombatPrototypeMapLayout result, CombatPrototypeMapChunk chunk,
            int biome, List<int> candidates)
        {
            candidates.Clear();
            for (var i = chunk.FirstCell; i < chunk.FirstCell + chunk.CellCount; i++)
                if (result.Cells[i].BiomeIndex == biome && result.Cells[i].Reserved == 0)
                    candidates.Add(i);
        }

        private static void ValidateEnemyGrid(CombatPrototypeMapData map, MapLayoutConfig layout, int columns, float spacing)
        {
            if (columns <= 0 || !math.isfinite(spacing) || spacing <= 0f)
                throw new InvalidOperationException("Map requires a positive enemy column count and spacing.");
            var origin = map.EnemySpawnOrigin.xz;
            var lower = map.Origin + layout.edgeKeepoutMeters;
            var upper = map.Origin + map.Size - layout.edgeKeepoutMeters;
            for (var i = 0; i < map.EnemyCount; i++)
            {
                var position = origin + new float2(i % columns, i / columns) * spacing;
                if (math.any(position < lower) || math.any(position > upper) ||
                    math.distance(position, map.PlayerSpawnOrigin.xz) < layout.enemySpawnMinDistanceMeters)
                    throw new InvalidOperationException("Enemy grid point is outside map bounds or too close to player spawn; index=" + i);
            }
        }

        private static bool IsRoad(float2 point, CombatPrototypeMapData map, float halfWidth)
        {
            var offset = math.abs(point - map.PlayerSpawnOrigin.xz);
            return offset.x <= halfWidth || offset.y <= halfWidth;
        }

        private static bool IsReserved(float2 point, float radius, CombatPrototypeMapData map,
            MapLayoutConfig layout, int columns, float spacing)
        {
            var lower = map.Origin + layout.edgeKeepoutMeters + radius;
            var upper = map.Origin + map.Size - layout.edgeKeepoutMeters - radius;
            if (math.any(point < lower) || math.any(point > upper) ||
                IsRoad(point, map, layout.mainPathWidthMeters * 0.5f + radius) ||
                math.distance(point, map.PlayerSpawnOrigin.xz) <=
                math.max(layout.spawnSafeRadiusMeters, layout.combatClearRadiusMeters) + radius)
                return true;
            var enemyLower = map.EnemySpawnOrigin.xz - spacing * 0.5f - radius;
            var enemyUpper = map.EnemySpawnOrigin.xz + new float2(
                math.min(columns, map.EnemyCount) - 1, (map.EnemyCount - 1) / columns) * spacing +
                spacing * 0.5f + radius;
            return math.all(point >= enemyLower) && math.all(point <= enemyUpper);
        }

        private static void Place(float density, int objectIndex, List<int> candidates, CombatMapConfigSet config,
            CombatPrototypeMapLayout result, PlacementSpacing[] spacing, PlacementOccupancy occupancy, MapLayoutConfig layout,
            int columns, float enemySpacing, ref Unity.Mathematics.Random random)
        {
            var expected = candidates.Count * result.Data.CellSize * result.Data.CellSize * density / 100f;
            var target = (int)math.floor(expected);
            if (random.NextFloat() < expected - target) target++;
            // Each content category can place at most one candidate per cell.
            target = math.min(target, candidates.Count);
            for (var i = candidates.Count - 1; i > 0; i--)
            {
                var j = random.NextInt(i + 1);
                var value = candidates[i]; candidates[i] = candidates[j]; candidates[j] = value;
            }
            var placed = 0;
            var definition = config.objects[objectIndex];
            foreach (var cellIndex in candidates)
            {
                if (placed >= target) break;
                var coordinate = result.Cells[cellIndex].Coordinate;
                var point = result.Data.Origin + (new float2(coordinate.x, coordinate.y) +
                    0.5f + random.NextFloat2(-0.35f, 0.35f)) * result.Data.CellSize;
                var clearance = definition.footprintRadiusMeters;
                if (definition.blocksMovement)
                    clearance += math.max(result.Data.PlayerRadius, result.Data.EnemyRadius) + result.Data.CollisionSkin;
                if (IsReserved(point, clearance, result.Data, layout, columns, enemySpacing) ||
                    !occupancy.CanPlace(point, definition.footprintRadiusMeters) || !spacing[objectIndex].TryAdd(point))
                    continue;
                var placementIndex = result.Decorations.Count;
                result.Decorations.Add(new CombatPrototypeMapDecoration
                {
                    ObjectIndex = objectIndex, Position = new float3(point.x, result.Data.BaseHeight, point.y),
                    YawRadians = random.NextFloat(0f, math.PI * 2f)
                });
                occupancy.Add(point, definition.footprintRadiusMeters);
                if (definition.blocksMovement)
                    result.Obstacles.Add(new CombatPrototypeMapObstacle
                    {
                        PlacementIndex = placementIndex, ObjectIndex = objectIndex,
                        Position = point, Radius = definition.footprintRadiusMeters
                    });
                placed++;
            }
        }

        private sealed class PlacementOccupancy
        {
            private struct Footprint
            {
                public float2 Position;
                public float Radius;
            }
            private readonly float _cellSize;
            private float _maximumRadius;
            private readonly Dictionary<int2, List<Footprint>> _buckets = new Dictionary<int2, List<Footprint>>();
            public PlacementOccupancy(float cellSize) { _cellSize = cellSize; }

            public bool CanPlace(float2 point, float radius)
            {
                if (radius == 0f) return true;
                var reach = radius + _maximumRadius;
                var lower = (int2)math.floor((point - reach) / _cellSize);
                var upper = (int2)math.floor((point + reach) / _cellSize);
                for (var z = lower.y; z <= upper.y; z++)
                for (var x = lower.x; x <= upper.x; x++)
                    if (_buckets.TryGetValue(new int2(x, z), out var footprints))
                        foreach (var footprint in footprints)
                        {
                            var separation = radius + footprint.Radius;
                            if (math.distancesq(point, footprint.Position) < separation * separation) return false;
                        }
                return true;
            }

            public void Add(float2 point, float radius)
            {
                if (radius == 0f) return;
                var key = (int2)math.floor(point / _cellSize);
                if (!_buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new List<Footprint>();
                    _buckets.Add(key, bucket);
                }
                bucket.Add(new Footprint { Position = point, Radius = radius });
                _maximumRadius = math.max(_maximumRadius, radius);
            }
        }

        private sealed class PlacementSpacing
        {
            private readonly float _distance;
            private readonly Dictionary<int2, List<float2>> _buckets = new Dictionary<int2, List<float2>>();
            public PlacementSpacing(float distance) { _distance = distance; }
            public bool TryAdd(float2 point)
            {
                var key = (int2)math.floor(point / _distance);
                for (var z = -1; z <= 1; z++)
                for (var x = -1; x <= 1; x++)
                    if (_buckets.TryGetValue(key + new int2(x, z), out var positions))
                        foreach (var position in positions)
                            if (math.distancesq(position, point) < _distance * _distance) return false;
                if (!_buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new List<float2>();
                    _buckets.Add(key, bucket);
                }
                bucket.Add(point);
                return true;
            }
        }
    }
}
