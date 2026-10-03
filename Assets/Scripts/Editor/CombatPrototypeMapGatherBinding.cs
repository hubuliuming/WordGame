using System;
using System.Collections.Generic;
using Code_01.CombatPrototype.Map;
using Code_01.CombatPrototype.Networking;
using UnityEditor;
using Unity.NetCode;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code_01.CombatPrototype.Editor
{
    public static class CombatPrototypeMapGatherBinding
    {
        private const string SubScenePath = "Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity";
        private const string GatherKey = "gather_apple";

        [MenuItem("Tools/CombatPrototype/地图/绑定第四阶段采集物")]
        public static void BindNetworkRoot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Gather binding requires EditMode.");
            var gather = AssetDatabase.LoadAssetAtPath<GameObject>(CombatPrototypeMapGatherAssetBuilder.PrefabPath);
            if (gather == null)
                throw new InvalidOperationException("Required gather prefab is missing: " + CombatPrototypeMapGatherAssetBuilder.PrefabPath);
            var filter = gather.GetComponent<MeshFilter>();
            var renderer = gather.GetComponent<MeshRenderer>();
            if (filter == null || filter.sharedMesh == null || renderer == null || renderer.sharedMaterial == null)
                throw new InvalidOperationException("Gather prefab requires root mesh and material: " + CombatPrototypeMapGatherAssetBuilder.PrefabPath);
            var ghost = gather.GetComponent<GhostAuthoringComponent>();
            if (gather.transform.childCount != 0 || gather.GetComponent<CombatPrototypeMapGatherAuthoring>() == null ||
                ghost == null || ghost.HasOwner || ghost.SupportAutoCommandTarget ||
                ghost.DefaultGhostMode != GhostMode.Interpolated || ghost.SupportedGhostModes != GhostModeMask.Interpolated)
                throw new InvalidOperationException("Gather prefab requires one root and interpolated gather Ghost authoring.");
            var active = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(SubScenePath);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SubScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("SubScene has unsaved changes; gather binding aborted.");
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
                    if (binding.ResourceKey == GatherKey)
                        throw new InvalidOperationException("Gatherable is already bound; repeated binding is not permitted.");
                    updated[i] = binding;
                }
                keys.Add(GatherKey);
                updated[updated.Length - 1] = new CombatPrototypeMapPrefabBinding { ResourceKey = GatherKey, Prefab = gather };
                foreach (var preset in new[] { CombatPrototypeMapPreset.Grassland, CombatPrototypeMapPreset.Forest })
                {
                    var config = LoadConfig(map, preset);
                    CombatPrototypeMapLayoutBuilder.Build(config, spawner.EnemyColumns, spawner.EnemySpacing);
                    foreach (var item in config.objects)
                        if (!keys.Contains(item.visualResourceKey))
                            throw new InvalidOperationException("Map prefab key is not explicitly bound; objectId=" +
                                item.objectId + ", visualResourceKey=" + item.visualResourceKey);
                }
                map.DecorationPrefabs = updated;
                EditorUtility.SetDirty(map);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Failed to save gather binding: " + SubScenePath);
                Debug.Log("[CombatPrototype.Map] Gather binding saved; scene=" + SubScenePath +
                    ", resource=" + CombatPrototypeMapGatherAssetBuilder.PrefabPath + ", preset=" + map.Preset + ".");
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
