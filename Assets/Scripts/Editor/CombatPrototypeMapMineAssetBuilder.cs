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
    public static class CombatPrototypeMapMineAssetBuilder
    {
        public const string MinePrefabPath = "Assets/Prefabs/CombatPrototype/Map/MineableRock.prefab";
        public const string StonePrefabPath = "Assets/Prefabs/CombatPrototype/Map/DroppedStone.prefab";
        public const string MineMeshPath = "Assets/Art/Map/CombatPrototype/Meshes/MineableRock.asset";
        public const string MineMaterialPath = "Assets/Art/Map/CombatPrototype/Materials/MineableRock.mat";
        private const string ShaderMaterialPath = "Assets/Art/Map/CombatPrototype/Materials/GroundRock.mat";

        [MenuItem("Tools/CombatPrototype/地图/生成第九阶段采矿资源")]
        public static void CreateResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Mining resources require idle EditMode.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Unsaved scene changes prevent mining resource creation.");
            foreach (var folder in new[] { "Assets/Prefabs/CombatPrototype/Map",
                         "Assets/Art/Map/CombatPrototype/Meshes", "Assets/Art/Map/CombatPrototype/Materials" })
                if (!AssetDatabase.IsValidFolder(folder))
                    throw new InvalidOperationException("Existing map resource directory is missing: " + folder);
            foreach (var path in new[] { MinePrefabPath, StonePrefabPath, MineMeshPath, MineMaterialPath })
                if (global::System.IO.File.Exists(path) || global::System.IO.File.Exists(path + ".meta"))
                    throw new InvalidOperationException("Mining resources already exist; overwriting is not permitted: " + path);
            var reference = AssetDatabase.LoadAssetAtPath<Material>(ShaderMaterialPath);
            if (reference == null || reference.shader == null)
                throw new InvalidOperationException("Existing ground rock material/shader are required: " + ShaderMaterialPath);
            CreateAsset(MineMaterialPath, () =>
            {
                var material = new Material(reference.shader) { name = "MineableRock", enableInstancing = true };
                material.SetColor("_BaseColor", new Color(0.36f, 0.38f, 0.40f));
                material.SetFloat("_Smoothness", 0f);
                material.SetFloat("_Cull", (float)CullMode.Back);
                return material;
            });
            CreateAsset(MineMeshPath, RockMesh);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MineMeshPath);
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(MineMaterialPath);
            CreatePrefab(MinePrefabPath, "MineableRock", mesh, rockMaterial, true);
            CreatePrefab(StonePrefabPath, "DroppedStone", mesh, rockMaterial, false);
        }

        private static void CreateAsset<T>(string path, Func<T> create) where T : UnityEngine.Object
        {
            T item = null;
            try
            {
                item = create();
                AssetDatabase.CreateAsset(item, path);
                Debug.Log("[CombatPrototype.Map] Mining resource created; stage=CreateAsset, resource=" + path + ".");
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Mining resource failed; stage=CreateAsset, resource=" +
                    path + ". " + exception);
                if (item != null && !AssetDatabase.Contains(item))
                {
                    try { UnityEngine.Object.DestroyImmediate(item); }
                    catch (Exception cleanup)
                    {
                        Debug.LogError("[CombatPrototype.Map] Mining resource cleanup failed; stage=DestroyUnregistered, resource=" +
                            path + ". " + cleanup);
                    }
                }
            }
        }

        private static void CreatePrefab(string path, string name, Mesh mesh, Material material, bool mine)
        {
            if (mesh == null || material == null)
            {
                Debug.LogError("[CombatPrototype.Map] Mining prefab failed; stage=ValidateResources, resource=" +
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
                root.AddComponent<Unity.Entities.Hybrid.Baking.LinkedEntityGroupAuthoring>();
                var ghost = root.AddComponent<GhostAuthoringComponent>();
                ghost.HasOwner = false;
                ghost.SupportAutoCommandTarget = false;
                ghost.DefaultGhostMode = GhostMode.Interpolated;
                ghost.SupportedGhostModes = GhostModeMask.Interpolated;
                ghost.OptimizationMode = GhostOptimizationMode.Dynamic;
                if (mine) root.AddComponent<CombatPrototypeMapMineAuthoring>();
                else root.AddComponent<CombatPrototypeMapDropAuthoring>();
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new InvalidOperationException("Prefab save returned no asset: " + path);
                Debug.Log("[CombatPrototype.Map] Mining prefab created; resource=" + path + ".");
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Mining prefab failed; stage=CreatePrefab, resource=" +
                    path + ". " + exception);
            }
            finally
            {
                if (root != null)
                {
                    try { UnityEngine.Object.DestroyImmediate(root); }
                    catch (Exception cleanup)
                    {
                        Debug.LogError("[CombatPrototype.Map] Mining prefab cleanup failed; stage=DestroyTemporary, resource=" +
                            path + ". " + cleanup);
                    }
                }
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                if (staging.IsValid())
                {
                    try { EditorSceneManager.CloseScene(staging, true); }
                    catch (Exception cleanup)
                    {
                        Debug.LogError("[CombatPrototype.Map] Mining prefab cleanup failed; stage=CloseStaging, resource=" +
                            path + ". " + cleanup);
                    }
                }
            }
        }

        private static Mesh RockMesh()
        {
            const int sides = 12;
            var radii = new[] { 0.55f, 0.75f, 0.25f };
            var heights = new[] { 0f, 0.6f, 1.2f };
            var vertices = new Vector3[sides * 3 + 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[sides * 18];
            for (var ring = 0; ring < 3; ring++)
            for (var i = 0; i < sides; i++)
            {
                var angle = 2f * Mathf.PI * i / sides;
                var index = ring * sides + i;
                vertices[index] = new Vector3(Mathf.Cos(angle) * radii[ring], heights[ring], Mathf.Sin(angle) * radii[ring]);
                uv[index] = new Vector2((float)i / sides, heights[ring] / 1.2f);
            }
            vertices[sides * 3] = Vector3.zero;
            vertices[sides * 3 + 1] = new Vector3(0f, 1.2f, 0f);
            var offset = 0;
            for (var ring = 0; ring < 2; ring++)
            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                AddTriangle(vertices, triangles, ref offset, ring * sides + i, (ring + 1) * sides + i, ring * sides + next);
                AddTriangle(vertices, triangles, ref offset, ring * sides + next, (ring + 1) * sides + i, (ring + 1) * sides + next);
            }
            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                AddTriangle(vertices, triangles, ref offset, sides * 3, i, next);
                AddTriangle(vertices, triangles, ref offset, sides * 3 + 1, sides * 2 + next, sides * 2 + i);
            }
            var mesh = new Mesh { name = "MineableRock", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddTriangle(Vector3[] vertices, int[] triangles, ref int offset, int a, int b, int c)
        {
            var normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            var center = (vertices[a] + vertices[b] + vertices[c]) / 3f - new Vector3(0f, 0.6f, 0f);
            if (Vector3.Dot(normal, center) < 0f) { var swap = b; b = c; c = swap; }
            triangles[offset++] = a; triangles[offset++] = b; triangles[offset++] = c;
        }
    }
}
