using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapChunkMeshBuilder
    {
        public static Mesh Build(in CombatPrototypeMapData map, in CombatPrototypeMapChunk chunk,
            NativeArray<CombatPrototypeMapCell> cells, int groundCount)
        {
            var vertices = new Vector3[chunk.CellCount * 4];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new List<int>[groundCount];
            for (var i = 0; i < triangles.Length; i++) triangles[i] = new List<int>();
            for (var i = 0; i < chunk.CellCount; i++)
            {
                var cell = cells[chunk.FirstCell + i];
                var lower = map.Origin + new float2(cell.Coordinate.x, cell.Coordinate.y) * map.CellSize;
                var vertex = i * 4;
                vertices[vertex] = new Vector3(lower.x, map.BaseHeight, lower.y);
                vertices[vertex + 1] = new Vector3(lower.x + map.CellSize, map.BaseHeight, lower.y);
                vertices[vertex + 2] = new Vector3(lower.x + map.CellSize, map.BaseHeight, lower.y + map.CellSize);
                vertices[vertex + 3] = new Vector3(lower.x, map.BaseHeight, lower.y + map.CellSize);
                for (var n = 0; n < 4; n++) normals[vertex + n] = Vector3.up;
                uv[vertex] = new Vector2(0f, 0f); uv[vertex + 1] = new Vector2(1f, 0f);
                uv[vertex + 2] = new Vector2(1f, 1f); uv[vertex + 3] = new Vector2(0f, 1f);
                var indices = triangles[cell.GroundIndex];
                indices.Add(vertex); indices.Add(vertex + 2); indices.Add(vertex + 1);
                indices.Add(vertex); indices.Add(vertex + 3); indices.Add(vertex + 2);
            }
            var mesh = new Mesh { name = "CombatMapChunk_" + chunk.Coordinate, hideFlags = HideFlags.DontSave };
            try
            {
                mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv;
                mesh.subMeshCount = groundCount;
                for (var i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i, false);
                mesh.RecalculateBounds();
                return mesh;
            }
            catch
            {
                DestroyOwnedMesh(mesh);
                throw;
            }
        }

        public static void DestroyOwnedMesh(Mesh mesh)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
            else UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}
