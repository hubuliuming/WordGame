using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    public partial class CombatPrototypeMapPresentationSystem : SystemBase
    {
        private EntityQuery _mapQuery;
        private Entity _source;
        private readonly List<Entity> _ownedEntities = new List<Entity>();
        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();

        protected override void OnCreate()
        {
            _mapQuery = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapData>());
            RequireForUpdate(_mapQuery);
        }

        protected override void OnUpdate()
        {
            var source = _mapQuery.GetSingletonEntity();
            if (source == _source) return;
            ReleaseOwned();
            _source = source;
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            // Snapshot buffers before structural changes invalidate their handles.
            using var chunks = EntityManager.GetBuffer<CombatPrototypeMapChunk>(source, true).ToNativeArray(Allocator.Temp);
            using var cells = EntityManager.GetBuffer<CombatPrototypeMapCell>(source, true).ToNativeArray(Allocator.Temp);
            using var grounds = EntityManager.GetBuffer<CombatPrototypeMapGround>(source, true).ToNativeArray(Allocator.Temp);
            using var objects = EntityManager.GetBuffer<CombatPrototypeMapObject>(source, true).ToNativeArray(Allocator.Temp);
            using var decorations = EntityManager.GetBuffer<CombatPrototypeMapDecoration>(source, true).ToNativeArray(Allocator.Temp);
            var materials = new Material[grounds.Length];
            if (World.GetExistingSystemManaged<EntitiesGraphicsSystem>() == null || grounds.Length == 0)
            {
                Debug.LogError("[CombatPrototype.Map] Presentation prerequisites failed; map=" + map.MapDefinitionId +
                    ", required Entities Graphics system or ground definitions are missing.");
                return;
            }
            for (var i = 0; i < grounds.Length; i++)
            {
                materials[i] = grounds[i].Material.Value;
                if (materials[i] == null)
                {
                    Debug.LogError("[CombatPrototype.Map] Presentation prerequisites failed; map=" + map.MapDefinitionId +
                        ", groundIndex=" + i + ", resource=" + grounds[i].ResourceKey + ", material is missing.");
                    return;
                }
            }
            var validObjects = new bool[objects.Length];
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i].Gatherable != 0) continue;
                var prefab = objects[i].Prefab;
                validObjects[i] = EntityManager.HasComponent<Prefab>(prefab) && EntityManager.HasComponent<LocalTransform>(prefab) &&
                    EntityManager.HasComponent<LocalToWorld>(prefab) && EntityManager.HasComponent<MaterialMeshInfo>(prefab);
                if (!validObjects[i])
                    Debug.LogError("[CombatPrototype.Map] Decoration prerequisites failed; objectIndex=" + i +
                        ", resource=" + objects[i].ResourceKey + ", prefab=" + prefab + ", required prefab components are missing.");
            }
            foreach (var chunk in chunks) CreateChunk(map, chunk, cells, materials);
            for (var i = 0; i < decorations.Length; i++)
            {
                var decoration = decorations[i];
                if (objects[decoration.ObjectIndex].Gatherable != 0) continue;
                if (!validObjects[decoration.ObjectIndex]) continue;
                var item = objects[decoration.ObjectIndex];
                var entity = Entity.Null;
                try
                {
                    entity = EntityManager.Instantiate(item.Prefab);
                    var transform = LocalTransform.FromPositionRotation(decoration.Position, quaternion.RotateY(decoration.YawRadians));
                    EntityManager.SetComponentData(entity, transform);
                    // Presentation runs after transform simulation; initialize this frame's render transform too.
                    EntityManager.SetComponentData(entity, new LocalToWorld { Value = transform.ToMatrix() });
                    _ownedEntities.Add(entity);
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Decoration instantiate/initialize failed; index=" + i +
                        ", resource=" + item.ResourceKey + ", prefab=" + item.Prefab + ". " + exception);
                    DestroyOwnedEntity(entity, "DecorationCleanup", i.ToString());
                }
            }
            Debug.Log("[CombatPrototype.Map] Presentation batch complete; map=" + map.MapDefinitionId +
                ", generatedChunks=" + _ownedMeshes.Count + "/" + chunks.Length +
                ", entities=" + _ownedEntities.Count + ", requestedDecorations=" + decorations.Length + ".");
        }

        private void CreateChunk(CombatPrototypeMapData map, CombatPrototypeMapChunk chunk,
            NativeArray<CombatPrototypeMapCell> cells, Material[] materials)
        {
            Mesh mesh = null;
            var created = new List<Entity>(materials.Length);
            var stage = "BuildMesh";
            try
            {
                mesh = CombatPrototypeMapChunkMeshBuilder.Build(map, chunk, cells, materials.Length);
                var renderArray = new RenderMeshArray(materials, new[] { mesh });
                var description = new RenderMeshDescription(ShadowCastingMode.Off, receiveShadows: false);
                stage = "CreateRenderEntities";
                for (var submesh = 0; submesh < materials.Length; submesh++)
                {
                    if (mesh.GetIndexCount(submesh) == 0) continue;
                    var entity = EntityManager.CreateEntity();
                    created.Add(entity);
                    RenderMeshUtility.AddComponents(entity, EntityManager, description, renderArray,
                        MaterialMeshInfo.FromRenderMeshArrayIndices(submesh, 0, (ushort)submesh));
                    EntityManager.SetComponentData(entity, new LocalToWorld { Value = float4x4.identity });
                }
                _ownedEntities.AddRange(created);
                _ownedMeshes.Add(mesh);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Chunk " + stage + " failed; chunk=" + chunk.Coordinate +
                    ", resource=generated_chunk_mesh. " + exception);
                foreach (var entity in created) DestroyOwnedEntity(entity, "ChunkCleanup", chunk.Coordinate.ToString());
                if (mesh != null) DestroyOwnedMesh(mesh, chunk.Coordinate.ToString());
            }
        }

        private void DestroyOwnedEntity(Entity entity, string stage, string item)
        {
            try
            {
                if (entity != Entity.Null && EntityManager.Exists(entity)) EntityManager.DestroyEntity(entity);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] " + stage + " failed; item=" + item + ", entity=" + entity + ". " + exception);
            }
        }

        private static void DestroyOwnedMesh(Mesh mesh, string item)
        {
            try { CombatPrototypeMapChunkMeshBuilder.DestroyOwnedMesh(mesh); }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Mesh cleanup failed; chunk=" + item + ", resource=" + mesh.name + ". " + exception);
            }
        }

        private void ReleaseOwned()
        {
            foreach (var entity in _ownedEntities) DestroyOwnedEntity(entity, "WorldCleanup", entity.ToString());
            _ownedEntities.Clear();
            foreach (var mesh in _ownedMeshes) DestroyOwnedMesh(mesh, mesh.name);
            _ownedMeshes.Clear();
            _source = Entity.Null;
        }

        protected override void OnStopRunning() { ReleaseOwned(); }
        protected override void OnDestroy() { ReleaseOwned(); }
    }
}
