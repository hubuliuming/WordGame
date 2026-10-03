using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Code_01.CombatPrototype.Editor
{
    public static class CombatPrototypeMapTreeAssetBuilder
    {
        public const string MaterialPath = "Assets/Art/Map/CombatPrototype/Materials/TreeNormal.mat";
        public const string MeshPath = "Assets/Art/Map/CombatPrototype/Meshes/TreeNormal.asset";
        public const string PrefabPath = "Assets/Prefabs/CombatPrototype/Map/TreeNormal.prefab";
        private const string ShaderPath = "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader";

        [MenuItem("Tools/CombatPrototype/地图/生成第三阶段树木资源")]
        public static void CreateResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Tree resources require EditMode.");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) throw new InvalidOperationException("Required URP Lit shader is missing: " + ShaderPath);
            foreach (var path in new[]
            {
                "Assets/Art/Map/CombatPrototype/Materials", "Assets/Art/Map/CombatPrototype/Meshes",
                "Assets/Prefabs/CombatPrototype/Map"
            })
                if (!AssetDatabase.IsValidFolder(path))
                    throw new InvalidOperationException("Existing map resource directory is missing: " + path);

            CreateAsset(MaterialPath, () =>
            {
                var material = new Material(shader) { name = "TreeNormal", enableInstancing = true };
                material.SetColor("_BaseColor", new Color(0.15f, 0.28f, 0.10f));
                material.SetFloat("_Smoothness", 0f);
                material.SetFloat("_Cull", (float)CullMode.Back);
                return material;
            });
            CreateAsset(MeshPath, TreeMesh);
            CreatePrefab();
        }

        private static void CreateAsset<T>(string path, Func<T> create) where T : UnityEngine.Object
        {
            T item = null;
            try
            {
                var existing = AssetDatabase.LoadMainAssetAtPath(path);
                if (existing != null)
                {
                    if (!(existing is T)) throw new InvalidOperationException("Resource path has a different asset type.");
                    return;
                }
                item = create();
                AssetDatabase.CreateAsset(item, path);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Tree resource creation failed; stage=CreateAsset, resource=" +
                    path + ". " + exception);
                if (item != null && !AssetDatabase.Contains(item)) DestroyTemporary(item, path);
            }
        }

        private static void CreatePrefab()
        {
            Scene staging = default;
            GameObject root = null;
            var active = SceneManager.GetActiveScene();
            try
            {
                var existing = AssetDatabase.LoadMainAssetAtPath(PrefabPath);
                if (existing != null)
                {
                    if (!(existing is GameObject)) throw new InvalidOperationException("Tree prefab path has a different asset type.");
                    return;
                }
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (mesh == null || material == null)
                    throw new InvalidOperationException("Tree mesh and material are required; mesh=" + MeshPath + ", material=" + MaterialPath);
                staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(staging);
                root = new GameObject("TreeNormal");
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                    throw new InvalidOperationException("Tree prefab save returned no asset.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Tree resource creation failed; stage=CreatePrefab, resource=" +
                    PrefabPath + ". " + exception);
            }
            finally
            {
                if (root != null) DestroyTemporary(root, PrefabPath);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                if (staging.IsValid())
                {
                    try { EditorSceneManager.CloseScene(staging, true); }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Tree resource cleanup failed; stage=CloseStagingScene, resource=" +
                            PrefabPath + ". " + exception);
                    }
                }
            }
        }

        private static void DestroyTemporary(UnityEngine.Object item, string resource)
        {
            try { UnityEngine.Object.DestroyImmediate(item); }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Tree resource cleanup failed; stage=DestroyTemporary, resource=" +
                    resource + ". " + exception);
            }
        }

        private static Mesh TreeMesh()
        {
            const int sides = 8;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices.Add(direction * 0.18f);
                vertices.Add(direction * 0.12f + Vector3.up * 1.6f);
            }
            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                triangles.AddRange(new[] { i * 2, i * 2 + 1, next * 2, next * 2, i * 2 + 1, next * 2 + 1 });
            }
            Cone(vertices, triangles, 1.25f, 1.1f, 2.8f, sides);
            Cone(vertices, triangles, 0.95f, 1.9f, 3.5f, sides);
            Cone(vertices, triangles, 0.65f, 2.7f, 4f, sides);
            var mesh = new Mesh { name = "TreeNormal" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void Cone(List<Vector3> vertices, List<int> triangles, float radius, float bottom, float top, int sides)
        {
            var first = vertices.Count;
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, bottom, Mathf.Sin(angle) * radius));
            }
            var tip = vertices.Count;
            vertices.Add(Vector3.up * top);
            var center = vertices.Count;
            vertices.Add(Vector3.up * bottom);
            for (var i = 0; i < sides; i++)
            {
                var next = first + (i + 1) % sides;
                triangles.AddRange(new[] { first + i, tip, next, first + i, next, center });
            }
        }
    }
}
