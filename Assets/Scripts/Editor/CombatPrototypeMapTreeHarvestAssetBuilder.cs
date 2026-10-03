using System;
using Code_01.CombatPrototype.Map;
using Unity.NetCode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Code_01.CombatPrototype.Editor
{
    public static class CombatPrototypeMapTreeHarvestAssetBuilder
    {
        public const string TreePrefabPath = "Assets/Prefabs/CombatPrototype/Map/HarvestableTree.prefab";
        public const string WoodPrefabPath = "Assets/Prefabs/CombatPrototype/Map/DroppedWood.prefab";
        public const string WoodMeshPath = "Assets/Art/Map/CombatPrototype/Meshes/DroppedWood.asset";
        public const string WoodMaterialPath = "Assets/Art/Map/CombatPrototype/Materials/DroppedWood.mat";
        private const string TreeMeshPath = "Assets/Art/Map/CombatPrototype/Meshes/TreeNormal.asset";
        private const string TreeMaterialPath = "Assets/Art/Map/CombatPrototype/Materials/TreeNormal.mat";

        [MenuItem("Tools/CombatPrototype/地图/生成第七阶段砍伐资源")]
        public static void CreateResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Tree harvest resources require EditMode.");
            foreach (var folder in new[] { "Assets/Prefabs/CombatPrototype/Map",
                         "Assets/Art/Map/CombatPrototype/Meshes", "Assets/Art/Map/CombatPrototype/Materials" })
                if (!AssetDatabase.IsValidFolder(folder))
                    throw new InvalidOperationException("Existing map resource directory is missing: " + folder);
            foreach (var path in new[] { TreePrefabPath, WoodPrefabPath, WoodMeshPath, WoodMaterialPath })
                if (global::System.IO.File.Exists(path) || global::System.IO.File.Exists(path + ".meta"))
                    throw new InvalidOperationException("Tree harvest resources already exist; overwriting is not permitted: " + path);
            var treeMesh = AssetDatabase.LoadAssetAtPath<Mesh>(TreeMeshPath);
            var treeMaterial = AssetDatabase.LoadAssetAtPath<Material>(TreeMaterialPath);
            if (treeMesh == null || treeMaterial == null || treeMaterial.shader == null)
                throw new InvalidOperationException("Existing tree mesh/material/shader are required; mesh=" +
                    TreeMeshPath + ", material=" + TreeMaterialPath);
            CreateAsset(WoodMaterialPath, () =>
            {
                var material = new Material(treeMaterial.shader) { name = "DroppedWood", enableInstancing = true };
                material.SetColor("_BaseColor", new Color(0.40f, 0.24f, 0.11f));
                material.SetFloat("_Smoothness", 0f);
                material.SetFloat("_Cull", (float)CullMode.Back);
                return material;
            });
            CreateAsset(WoodMeshPath, WoodMesh);
            CreatePrefab(TreePrefabPath, "HarvestableTree", treeMesh, treeMaterial, true);
            CreatePrefab(WoodPrefabPath, "DroppedWood", AssetDatabase.LoadAssetAtPath<Mesh>(WoodMeshPath),
                AssetDatabase.LoadAssetAtPath<Material>(WoodMaterialPath), false);
        }

        private static void CreateAsset<T>(string path, Func<T> create) where T : UnityEngine.Object
        {
            T item = null;
            try
            {
                item = create();
                AssetDatabase.CreateAsset(item, path);
                Debug.Log("[CombatPrototype.Map] Tree harvest resource created; stage=CreateAsset, resource=" + path + ".");
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Tree harvest resource failed; stage=CreateAsset, resource=" +
                    path + ". " + exception);
                if (item != null && !AssetDatabase.Contains(item))
                {
                    try { UnityEngine.Object.DestroyImmediate(item); }
                    catch (Exception cleanup)
                    {
                        Debug.LogError("[CombatPrototype.Map] Tree harvest resource cleanup failed; stage=DestroyUnregistered, resource=" +
                            path + ". " + cleanup);
                    }
                }
            }
        }

        private static void CreatePrefab(string path, string name, Mesh mesh, Material material, bool tree)
        {
            if (mesh == null || material == null)
            {
                Debug.LogError("[CombatPrototype.Map] Tree harvest prefab failed; stage=ValidateResources, resource=" +
                    path + ", required mesh or material is missing.");
                return;
            }
            var active = SceneManager.GetActiveScene();
            Scene staging = default;
            GameObject root = null;
            try
            {
                staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(staging);
                root = new GameObject(name);
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                var ghost = root.AddComponent<GhostAuthoringComponent>();
                ghost.HasOwner = false;
                ghost.SupportAutoCommandTarget = false;
                ghost.DefaultGhostMode = GhostMode.Interpolated;
                ghost.SupportedGhostModes = GhostModeMask.Interpolated;
                ghost.OptimizationMode = GhostOptimizationMode.Dynamic;
                if (tree) root.AddComponent<CombatPrototypeMapTreeAuthoring>();
                else root.AddComponent<CombatPrototypeMapDropAuthoring>();
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new InvalidOperationException("Prefab save returned no asset: " + path);
                Debug.Log("[CombatPrototype.Map] Tree harvest prefab created; resource=" + path + ".");
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Tree harvest prefab failed; stage=CreatePrefab, resource=" +
                    path + ". " + exception);
            }
            finally
            {
                if (root != null)
                {
                    try { UnityEngine.Object.DestroyImmediate(root); }
                    catch (Exception cleanup)
                    {
                        Debug.LogError("[CombatPrototype.Map] Tree harvest prefab cleanup failed; stage=DestroyTemporary, resource=" +
                            path + ". " + cleanup);
                    }
                }
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                if (staging.IsValid())
                {
                    try { EditorSceneManager.CloseScene(staging, true); }
                    catch (Exception cleanup)
                    {
                        Debug.LogError("[CombatPrototype.Map] Tree harvest prefab cleanup failed; stage=CloseStaging, resource=" +
                            path + ". " + cleanup);
                    }
                }
            }
        }

        private static Mesh WoodMesh()
        {
            const int sides = 8;
            var vertices = new Vector3[sides * 2 + 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[sides * 12];
            for (var i = 0; i < sides; i++)
            {
                var angle = 2f * Mathf.PI * i / sides;
                var y = 0.22f + Mathf.Sin(angle) * 0.22f;
                var z = Mathf.Cos(angle) * 0.22f;
                vertices[i] = new Vector3(-0.6f, y, z);
                vertices[i + sides] = new Vector3(0.6f, y, z);
                uv[i] = new Vector2(0f, (float)i / sides);
                uv[i + sides] = new Vector2(1f, (float)i / sides);
                var next = (i + 1) % sides;
                var t = i * 12;
                triangles[t] = i; triangles[t + 1] = i + sides; triangles[t + 2] = next;
                triangles[t + 3] = next; triangles[t + 4] = i + sides; triangles[t + 5] = next + sides;
                triangles[t + 6] = sides * 2; triangles[t + 7] = i; triangles[t + 8] = next;
                triangles[t + 9] = sides * 2 + 1; triangles[t + 10] = next + sides; triangles[t + 11] = i + sides;
            }
            vertices[sides * 2] = new Vector3(-0.6f, 0.22f, 0f);
            vertices[sides * 2 + 1] = new Vector3(0.6f, 0.22f, 0f);
            var mesh = new Mesh { name = "DroppedWood", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
