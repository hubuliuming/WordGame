using System;
using System.Collections.Generic;
using Code_01.CombatPrototype.Map;
using Code_01.CombatPrototype.Networking;
using Unity.NetCode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code_01.CombatPrototype.Editor
{
    public static class CombatPrototypeMapDropBinding
    {
        private const string SubScenePath = "Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity";
        private const string DropKey = "drop_apple";

        [MenuItem("Tools/CombatPrototype/地图/绑定第六阶段掉落物")]
        public static void BindNetworkRoot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Drop binding requires EditMode.");
            var drop = AssetDatabase.LoadAssetAtPath<GameObject>(CombatPrototypeMapDropAssetBuilder.PrefabPath);
            if (drop == null)
                throw new InvalidOperationException("Required drop prefab is missing: " + CombatPrototypeMapDropAssetBuilder.PrefabPath);
            var filter = drop.GetComponent<MeshFilter>();
            var renderer = drop.GetComponent<MeshRenderer>();
            var ghost = drop.GetComponent<GhostAuthoringComponent>();
            if (filter == null || filter.sharedMesh == null || renderer == null || renderer.sharedMaterial == null ||
                drop.transform.childCount != 0 || drop.GetComponent<CombatPrototypeMapDropAuthoring>() == null ||
                ghost == null || ghost.HasOwner || ghost.SupportAutoCommandTarget ||
                ghost.DefaultGhostMode != GhostMode.Interpolated || ghost.SupportedGhostModes != GhostModeMask.Interpolated)
                throw new InvalidOperationException("Drop prefab requires one root, mesh/material and interpolated drop Ghost authoring.");
            var active = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(SubScenePath);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SubScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("SubScene has unsaved changes; drop binding aborted.");
                GameObject root = null;
                foreach (var candidate in scene.GetRootGameObjects())
                {
                    if (candidate.name != "CombatPrototypeNetworkRoot") continue;
                    if (root != null) throw new InvalidOperationException("Duplicate CombatPrototypeNetworkRoot.");
                    root = candidate;
                }
                if (root == null) throw new InvalidOperationException("Required CombatPrototypeNetworkRoot is missing.");
                var map = root.GetComponent<CombatPrototypeMapAuthoring>();
                var spawner = root.GetComponent<CombatPrototypePlayerSpawnerAuthoring>();
                if (map == null || spawner == null)
                    throw new InvalidOperationException("Existing map authoring and spawner are required on the network root.");
                if (map.DecorationPrefabs == null) throw new InvalidOperationException("Existing map prefab bindings are required.");
                var keys = new HashSet<string>(StringComparer.Ordinal);
                var updated = new CombatPrototypeMapPrefabBinding[map.DecorationPrefabs.Length + 1];
                for (var i = 0; i < map.DecorationPrefabs.Length; i++)
                {
                    var binding = map.DecorationPrefabs[i];
                    if (binding == null || string.IsNullOrWhiteSpace(binding.ResourceKey) || binding.Prefab == null ||
                        !keys.Add(binding.ResourceKey))
                        throw new InvalidOperationException("Invalid or duplicate existing map prefab binding; index=" + i);
                    if (binding.ResourceKey == DropKey)
                        throw new InvalidOperationException("Drop is already bound; repeated binding is not permitted.");
                    updated[i] = binding;
                }
                keys.Add(DropKey);
                updated[updated.Length - 1] = new CombatPrototypeMapPrefabBinding { ResourceKey = DropKey, Prefab = drop };
                foreach (var preset in new[] { CombatPrototypeMapPreset.Grassland, CombatPrototypeMapPreset.Forest })
                {
                    var config = LoadConfig(map, preset);
                    CombatPrototypeMapLayoutBuilder.Build(config, spawner.EnemyColumns, spawner.EnemySpacing);
                    foreach (var item in config.objects)
                        if (!keys.Contains(item.visualResourceKey))
                            throw new InvalidOperationException("Map prefab key is not explicitly bound; objectId=" +
                                item.objectId + ", visualResourceKey=" + item.visualResourceKey);
                    if (!keys.Contains(config.map.drops.visualResourceKey))
                        throw new InvalidOperationException("Map drop prefab key is not explicitly bound: " + config.map.drops.visualResourceKey);
                }
                map.DecorationPrefabs = updated;
                EditorUtility.SetDirty(map);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Failed to save drop binding: " + SubScenePath);
                Debug.Log("[CombatPrototype.Map] Drop binding saved; scene=" + SubScenePath +
                    ", resource=" + CombatPrototypeMapDropAssetBuilder.PrefabPath + ", preset=" + map.Preset + ".");
            }
            finally
            {
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static CombatMapConfigSet LoadConfig(CombatPrototypeMapAuthoring map, CombatPrototypeMapPreset preset)
        {
            var id = preset == CombatPrototypeMapPreset.Grassland
                ? CombatPrototypeDefaultMapConfigSource.GrasslandId : CombatPrototypeDefaultMapConfigSource.ForestId;
            ICombatMapConfigSource source;
            switch (map.SourceMode)
            {
                case CombatPrototypeMapConfigSourceMode.BuiltIn:
                    source = new CombatPrototypeDefaultMapConfigSource();
                    break;
                case CombatPrototypeMapConfigSourceMode.Json:
                    source = new CombatPrototypeJsonMapConfigSource(
                        preset == CombatPrototypeMapPreset.Grassland ? map.GrasslandJson : map.ForestJson,
                        map.BiomesJson, map.GroundsJson, map.ObjectsJson);
                    break;
                default: throw new InvalidOperationException("Unknown map configuration source: " + map.SourceMode);
            }
            return source.LoadValidated(id);
        }
    }
}
