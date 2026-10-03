using System;
using Code_01.CombatPrototype.Map;
using Code_01.CombatPrototype.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Code_01.CombatPrototype.Editor
{
    public static class CombatPrototypeMapAssetBuilder
    {
        private const string Art = "Assets/Art/Map/CombatPrototype";
        private const string Prefabs = "Assets/Prefabs/CombatPrototype/Map";
        private const string SubScenePath = "Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity";
        private const string ShaderPath = "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader";

        [MenuItem("Tools/CombatPrototype/地图/生成第一阶段资源")]
        public static void CreateResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Map resources must be generated in EditMode.");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) throw new InvalidOperationException("Required URP Lit shader is missing: " + ShaderPath);
            Folder("Assets/Art/Map"); Folder(Art); Folder(Art + "/Materials"); Folder(Art + "/Meshes");
            Folder(Prefabs);
            CreateItem(Art + "/Materials/GroundGrass.mat", () => CreateMaterial("GroundGrass", shader, new Color(0.23f, 0.36f, 0.14f), false));
            CreateItem(Art + "/Materials/GroundForest.mat", () => CreateMaterial("GroundForest", shader, new Color(0.12f, 0.20f, 0.11f), false));
            CreateItem(Art + "/Materials/GroundRock.mat", () => CreateMaterial("GroundRock", shader, new Color(0.36f, 0.36f, 0.32f), false));
            CreateItem(Art + "/Materials/DecorationGrass.mat", () => CreateMaterial("DecorationGrass", shader, new Color(0.30f, 0.52f, 0.18f), true));
            CreateItem(Art + "/Meshes/DecorationGrass.asset", GrassMesh);
            CreateItem(Art + "/Meshes/DecorationPebble.asset", PebbleMesh);
            CreatePrefab(Prefabs + "/DecorationGrass.prefab", Art + "/Meshes/DecorationGrass.asset", Art + "/Materials/DecorationGrass.mat");
            CreatePrefab(Prefabs + "/DecorationPebble.prefab", Art + "/Meshes/DecorationPebble.asset", Art + "/Materials/GroundRock.mat");
        }

        [MenuItem("Tools/CombatPrototype/地图/绑定第一阶段地图")]
        public static void BindNetworkRoot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Map binding must be performed in EditMode.");
            var materials = new[]
            {
                new CombatPrototypeMapMaterialBinding { ResourceKey = "ground_grass", Material = Required<Material>(Art + "/Materials/GroundGrass.mat") },
                new CombatPrototypeMapMaterialBinding { ResourceKey = "ground_forest", Material = Required<Material>(Art + "/Materials/GroundForest.mat") },
                new CombatPrototypeMapMaterialBinding { ResourceKey = "ground_rock", Material = Required<Material>(Art + "/Materials/GroundRock.mat") }
            };
            var prefabs = new[]
            {
                new CombatPrototypeMapPrefabBinding { ResourceKey = "decor_grass", Prefab = Required<GameObject>(Prefabs + "/DecorationGrass.prefab") },
                new CombatPrototypeMapPrefabBinding { ResourceKey = "decor_pebble", Prefab = Required<GameObject>(Prefabs + "/DecorationPebble.prefab") }
            };
            var activeScene = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(SubScenePath);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SubScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("SubScene has unsaved changes; map binding aborted.");
                GameObject root = null;
                foreach (var candidate in scene.GetRootGameObjects())
                {
                    if (candidate.name != "CombatPrototypeNetworkRoot") continue;
                    if (root != null) throw new InvalidOperationException("Duplicate CombatPrototypeNetworkRoot.");
                    root = candidate;
                }
                if (root == null || root.GetComponent<CombatPrototypePlayerSpawnerAuthoring>() == null)
                    throw new InvalidOperationException("Required CombatPrototypeNetworkRoot/spawner is missing.");
                if (root.GetComponent<CombatPrototypeMapAuthoring>() != null)
                    throw new InvalidOperationException("Map authoring is already attached; edit its existing configuration explicitly.");
                var map = root.AddComponent<CombatPrototypeMapAuthoring>();
                map.Preset = CombatPrototypeMapPreset.Forest;
                map.GroundMaterials = materials;
                map.DecorationPrefabs = prefabs;
                EditorUtility.SetDirty(map);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Failed to save map binding: " + SubScenePath);
                Debug.Log("[CombatPrototype.Map] Bound first-stage map; scene=" + SubScenePath + ", preset=Forest, materials=3, decorationPrefabs=2.");
            }
            finally
            {
                if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var split = path.LastIndexOf('/');
            var parent = path.Substring(0, split);
            if (!AssetDatabase.IsValidFolder(parent)) Folder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, path.Substring(split + 1))))
                throw new InvalidOperationException("Could not create map resource folder: " + path);
        }

        private static Material CreateMaterial(string name, Shader shader, Color color, bool doubleSided)
        {
            var material = new Material(shader) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
            return material;
        }

        private static void CreateItem<T>(string path, Func<T> create) where T : UnityEngine.Object
        {
            T item = null;
            try
            {
                var existing = AssetDatabase.LoadMainAssetAtPath(path);
                if (existing != null)
                {
                    if (!(existing is T)) throw new InvalidOperationException("Map resource path has a different asset type.");
                    return;
                }
                item = create();
                AssetDatabase.CreateAsset(item, path);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Resource creation failed; stage=CreateAsset, resource=" + path + ". " + exception);
                if (item != null && !AssetDatabase.Contains(item)) DestroyTemporary(item, path);
            }
        }

        private static void CreatePrefab(string path, string meshPath, string materialPath)
        {
            Scene staging = default;
            GameObject root = null;
            var activeScene = SceneManager.GetActiveScene();
            try
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) { Required<GameObject>(path); return; }
                var mesh = Required<Mesh>(meshPath);
                var material = Required<Material>(materialPath);
                staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(staging);
                root = new GameObject(global::System.IO.Path.GetFileNameWithoutExtension(path));
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new InvalidOperationException("Prefab save returned no asset.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Resource creation failed; stage=CreatePrefab, resource=" + path + ". " + exception);
            }
            finally
            {
                if (root != null) DestroyTemporary(root, path);
                if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
                if (staging.IsValid()) CloseStagingScene(staging, path);
            }
        }

        private static void DestroyTemporary(UnityEngine.Object item, string path)
        {
            try { UnityEngine.Object.DestroyImmediate(item); }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Resource cleanup failed; stage=DestroyTemporary, resource=" + path + ". " + exception);
            }
        }

        private static void CloseStagingScene(Scene scene, string path)
        {
            try { EditorSceneManager.CloseScene(scene, true); }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Resource cleanup failed; stage=CloseStagingScene, resource=" + path + ". " + exception);
            }
        }

        private static T Required<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required map resource is missing: " + path);
            return asset;
        }

        private static Mesh GrassMesh()
        {
            var vertices = new Vector3[18];
            var triangles = new int[18];
            for (var i = 0; i < 6; i++)
            {
                var angle = i * Mathf.PI / 3f;
                var width = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.11f;
                var center = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 0.08f;
                vertices[i * 3] = center - width;
                vertices[i * 3 + 1] = center + width;
                vertices[i * 3 + 2] = center + new Vector3(0.05f, 0.55f + (i % 3) * 0.08f, 0.04f);
                for (var j = 0; j < 3; j++) triangles[i * 3 + j] = i * 3 + j;
            }
            return FinishMesh("DecorationGrass", vertices, triangles);
        }

        private static Mesh PebbleMesh()
        {
            return FinishMesh("DecorationPebble", new[]
            {
                new Vector3(0f, 0.32f, 0f), Vector3.zero,
                new Vector3(-0.28f, 0.12f, 0f), new Vector3(0f, 0.12f, 0.22f),
                new Vector3(0.28f, 0.12f, 0f), new Vector3(0f, 0.12f, -0.22f)
            }, new[] { 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 2, 1, 3, 2, 1, 4, 3, 1, 5, 4, 1, 2, 5 });
        }

        private static Mesh FinishMesh(string name, Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
