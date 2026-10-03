using System;
using System.Collections.Generic;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapPreset { Grassland, Forest }

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
            // Configuration data and Unity resource bindings remain separate.
            ICombatMapConfigSource source = new CombatPrototypeDefaultMapConfigSource();
            return source.LoadValidated(id);
        }

        private sealed class Baker : Baker<CombatPrototypeMapAuthoring>
        {
            public override void Bake(CombatPrototypeMapAuthoring authoring)
            {
                var spawner = GetComponent<CombatPrototypePlayerSpawnerAuthoring>();
                if (spawner == null)
                    throw new InvalidOperationException("Map authoring requires the existing player spawner on the same root.");
                var config = authoring.LoadMapConfig();
                var materials = ReadMaterials(authoring.GroundMaterials);
                var prefabs = ReadPrefabs(authoring.DecorationPrefabs);
                var layout = CombatPrototypeMapLayoutBuilder.Build(config, spawner.EnemyColumns, spawner.EnemySpacing);
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, layout.Data);
                var chunks = AddBuffer<CombatPrototypeMapChunk>(entity);
                foreach (var chunk in layout.Chunks) chunks.Add(chunk);
                var cells = AddBuffer<CombatPrototypeMapCell>(entity);
                foreach (var cell in layout.Cells) cells.Add(cell);
                var decorations = AddBuffer<CombatPrototypeMapDecoration>(entity);
                foreach (var decoration in layout.Decorations) decorations.Add(decoration);
                var grounds = AddBuffer<CombatPrototypeMapGround>(entity);
                foreach (var ground in config.grounds)
                {
                    if (!materials.TryGetValue(ground.visualResourceKey, out var material))
                        throw new InvalidOperationException("Missing map ground material: " + ground.visualResourceKey);
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
                        GatherableDensityPer100m2 = biome.gatherableDensityPer100m2
                    });
                var objects = AddBuffer<CombatPrototypeMapObject>(entity);
                foreach (var item in config.objects)
                {
                    if (!prefabs.TryGetValue(item.visualResourceKey, out var prefab))
                        throw new InvalidOperationException("Missing map decoration prefab: " + item.visualResourceKey);
                    objects.Add(new CombatPrototypeMapObject
                    {
                        ObjectId = item.objectId, ResourceKey = item.visualResourceKey,
                        Prefab = GetEntity(prefab, TransformUsageFlags.Dynamic)
                    });
                }
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
