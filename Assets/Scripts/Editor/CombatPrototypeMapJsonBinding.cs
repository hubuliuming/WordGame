using System;
using Code_01.CombatPrototype.Map;
using Code_01.CombatPrototype.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code_01.CombatPrototype.Editor
{
    public static class CombatPrototypeMapJsonBinding
    {
        private const string ConfigDirectory = "Assets/Config/CombatPrototype/Map/";
        private const string SubScenePath = "Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity";

        [MenuItem("Tools/CombatPrototype/地图/绑定第二阶段 JSON")]
        public static void BindNetworkRoot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Map JSON binding requires EditMode.");
            var grassland = Required("battle_grassland_01.json");
            var forest = Required("battle_forest_01.json");
            var biomes = Required("biomes.json");
            var grounds = Required("grounds.json");
            var objects = Required("objects.json");
            var grasslandConfig = new CombatPrototypeJsonMapConfigSource(grassland, biomes, grounds, objects)
                .LoadValidated(CombatPrototypeDefaultMapConfigSource.GrasslandId);
            var forestConfig = new CombatPrototypeJsonMapConfigSource(forest, biomes, grounds, objects)
                .LoadValidated(CombatPrototypeDefaultMapConfigSource.ForestId);
            var activeScene = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(SubScenePath);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SubScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("SubScene has unsaved changes; JSON binding aborted.");
                GameObject root = null;
                foreach (var candidate in scene.GetRootGameObjects())
                {
                    if (candidate.name != "CombatPrototypeNetworkRoot") continue;
                    if (root != null) throw new InvalidOperationException("Duplicate CombatPrototypeNetworkRoot.");
                    root = candidate;
                }
                if (root == null) throw new InvalidOperationException("Required CombatPrototypeNetworkRoot is missing.");
                var spawner = root.GetComponent<CombatPrototypePlayerSpawnerAuthoring>();
                var map = root.GetComponent<CombatPrototypeMapAuthoring>();
                if (spawner == null || map == null)
                    throw new InvalidOperationException("Existing map authoring and spawner are required on the network root.");
                CombatPrototypeMapLayoutBuilder.Build(grasslandConfig, spawner.EnemyColumns, spawner.EnemySpacing);
                CombatPrototypeMapLayoutBuilder.Build(forestConfig, spawner.EnemyColumns, spawner.EnemySpacing);
                map.GrasslandJson = grassland;
                map.ForestJson = forest;
                map.BiomesJson = biomes;
                map.GroundsJson = grounds;
                map.ObjectsJson = objects;
                map.SourceMode = CombatPrototypeMapConfigSourceMode.Json;
                EditorUtility.SetDirty(map);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Failed to save map JSON bindings: " + SubScenePath);
                Debug.Log("[CombatPrototype.Map] JSON bindings saved; scene=" + SubScenePath +
                    ", preset=" + map.Preset + ", JSON files=5.");
            }
            finally
            {
                if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static TextAsset Required(string fileName)
        {
            var path = ConfigDirectory + fileName;
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null) throw new InvalidOperationException("Required map JSON is missing: " + path);
            return asset;
        }
    }
}
