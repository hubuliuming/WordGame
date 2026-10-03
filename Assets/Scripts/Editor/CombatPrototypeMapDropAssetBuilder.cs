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
    public static class CombatPrototypeMapDropAssetBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/CombatPrototype/Map/DroppedApple.prefab";
        private const string MeshPath = "Assets/Art/Map/CombatPrototype/Meshes/GatherApple.asset";
        private const string MaterialPath = "Assets/Art/Map/CombatPrototype/Materials/GatherApple.mat";

        [MenuItem("Tools/CombatPrototype/地图/生成第六阶段掉落资源")]
        public static void CreateResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Drop resources require EditMode.");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/CombatPrototype/Map"))
                throw new InvalidOperationException("Existing map prefab directory is missing.");
            if (AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
                throw new InvalidOperationException("Drop prefab already exists; overwriting is not permitted: " + PrefabPath);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mesh == null || material == null)
                throw new InvalidOperationException("Drop requires the existing mesh and material; mesh=" + MeshPath +
                    ", material=" + MaterialPath);
            var active = SceneManager.GetActiveScene();
            Scene staging = default;
            GameObject root = null;
            try
            {
                staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(staging);
                root = new GameObject("DroppedApple");
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
                root.AddComponent<CombatPrototypeMapDropAuthoring>();
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                    throw new InvalidOperationException("Drop prefab save returned no asset: " + PrefabPath);
                Debug.Log("[CombatPrototype.Map] Drop prefab created; resource=" + PrefabPath + ".");
            }
            finally
            {
                if (root != null)
                {
                    try { UnityEngine.Object.DestroyImmediate(root); }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Drop resource cleanup failed; stage=DestroyTemporary, resource=" +
                            PrefabPath + ". " + exception);
                    }
                }
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                if (staging.IsValid())
                {
                    try { EditorSceneManager.CloseScene(staging, true); }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Drop resource cleanup failed; stage=CloseStagingScene, resource=" +
                            PrefabPath + ". " + exception);
                    }
                }
            }
        }
    }
}
