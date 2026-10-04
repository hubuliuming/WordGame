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
    public static class CombatPrototypeMapMineBinding
    {
        private const string SubScenePath = "Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity";
        private const string MineKey = "mine_rock";
        private const string StoneKey = "drop_stone";

        [MenuItem("Tools/CombatPrototype/地图/绑定第九阶段采矿资源")]
        public static void BindNetworkRoot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Mining binding requires EditMode.");
            var mine = ReadPrefab(CombatPrototypeMapMineAssetBuilder.MinePrefabPath, typeof(CombatPrototypeMapMineAuthoring));
            var stone = ReadPrefab(CombatPrototypeMapMineAssetBuilder.StonePrefabPath, typeof(CombatPrototypeMapDropAuthoring));
            var active = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(SubScenePath);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SubScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("SubScene has unsaved changes; mining binding aborted.");
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
                var updated = new CombatPrototypeMapPrefabBinding[map.DecorationPrefabs.Length + 2];
                for (var i = 0; i < map.DecorationPrefabs.Length; i++)
                {
                    var binding = map.DecorationPrefabs[i];
                    if (binding == null || string.IsNullOrWhiteSpace(binding.ResourceKey) || binding.Prefab == null ||
                        !keys.Add(binding.ResourceKey))
                        throw new InvalidOperationException("Invalid or duplicate existing map prefab binding; index=" + i);
                    if (binding.ResourceKey == MineKey || binding.ResourceKey == StoneKey)
                        throw new InvalidOperationException("Mining resources are already bound; repeated binding is not permitted.");
                    updated[i] = binding;
                }
                keys.Add(MineKey);
                keys.Add(StoneKey);
                updated[updated.Length - 2] = new CombatPrototypeMapPrefabBinding { ResourceKey = MineKey, Prefab = mine };
                updated[updated.Length - 1] = new CombatPrototypeMapPrefabBinding { ResourceKey = StoneKey, Prefab = stone };
                foreach (var preset in new[] { CombatPrototypeMapPreset.Grassland, CombatPrototypeMapPreset.Forest })
                {
                    var config = LoadConfig(map, preset);
                    CombatPrototypeMapLayoutBuilder.Build(config, spawner.EnemyColumns, spawner.EnemySpacing);
                    foreach (var item in config.objects)
                        if (!keys.Contains(item.visualResourceKey))
                            throw new InvalidOperationException("Map prefab key is not explicitly bound; objectId=" +
                                item.objectId + ", visualResourceKey=" + item.visualResourceKey);
                    if (!keys.Contains(config.map.drops.visualResourceKey) ||
                        !keys.Contains(config.map.treeHarvest.visualResourceKey) ||
                        !keys.Contains(config.map.treeHarvest.dropVisualResourceKey) ||
                        !keys.Contains(config.map.mining.visualResourceKey) ||
                        !keys.Contains(config.map.mining.dropVisualResourceKey))
                        throw new InvalidOperationException("Required drop, tree harvest or mining resource key is not explicitly bound; map=" +
                            config.map.mapDefinitionId);
                }
                map.DecorationPrefabs = updated;
                EditorUtility.SetDirty(map);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Failed to save mining binding: " + SubScenePath);
                Debug.Log("[CombatPrototype.Map] Mining bindings saved; scene=" + SubScenePath +
                    ", mineResource=" + CombatPrototypeMapMineAssetBuilder.MinePrefabPath +
                    ", dropResource=" + CombatPrototypeMapMineAssetBuilder.StonePrefabPath + ", preset=" + map.Preset + ".");
            }
            finally
            {
                try { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
                catch (Exception cleanup)
                {
                    Debug.LogError("[CombatPrototype.Map] Mining binding cleanup failed; stage=RestoreActiveScene, scene=" + SubScenePath + ". " + cleanup);
                }
                try { if (opened) EditorSceneManager.CloseScene(scene, true); }
                catch (Exception cleanup)
                {
                    Debug.LogError("[CombatPrototype.Map] Mining binding cleanup failed; stage=CloseSubScene, scene=" + SubScenePath + ". " + cleanup);
                }
            }
        }

        private static GameObject ReadPrefab(string path, Type component)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Required mining prefab is missing: " + path);
            var filter = prefab.GetComponent<MeshFilter>();
            var renderer = prefab.GetComponent<MeshRenderer>();
            var ghost = prefab.GetComponent<GhostAuthoringComponent>();
            if (filter == null || filter.sharedMesh == null || renderer == null || renderer.sharedMaterial == null ||
                prefab.transform.childCount != 0 || prefab.GetComponent(component) == null || ghost == null ||
                ghost.HasOwner || ghost.SupportAutoCommandTarget || ghost.DefaultGhostMode != GhostMode.Interpolated ||
                ghost.SupportedGhostModes != GhostModeMask.Interpolated)
                throw new InvalidOperationException("Mining requires one-root mesh/material and interpolated Ghost; resource=" +
                    path + ", requiredComponent=" + component.Name);
            return prefab;
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
