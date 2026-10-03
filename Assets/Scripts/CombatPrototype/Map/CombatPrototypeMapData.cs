using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapData : IComponentData
    {
        public FixedString64Bytes MapDefinitionId;
        public int ConfigRevision;
        public uint Seed;
        public float2 Origin;
        public float2 Size;
        public float CellSize;
        public float BaseHeight;
        public int CellsPerChunk;
        public float3 PlayerSpawnOrigin;
        public float PlayerSpawnSpacing;
        public float3 EnemySpawnOrigin;
        public int EnemyCount;
        public float PlayerRadius;
        public float EnemyRadius;
        public float CollisionSkin;
        public int MaxSlideIterations;
    }

    [InternalBufferCapacity(0)]
    public struct CombatPrototypeMapChunk : IBufferElementData
    {
        public int2 Coordinate;
        public int FirstCell;
        public int CellCount;
    }

    [InternalBufferCapacity(0)]
    public struct CombatPrototypeMapCell : IBufferElementData
    {
        public int2 Coordinate;
        public int BiomeIndex;
        public int GroundIndex;
        public byte Reserved;
    }

    [InternalBufferCapacity(0)]
    public struct CombatPrototypeMapDecoration : IBufferElementData
    {
        public int ObjectIndex;
        public float3 Position;
        public float YawRadians;
    }

    [InternalBufferCapacity(0)]
    public struct CombatPrototypeMapObstacle : IBufferElementData
    {
        public int PlacementIndex;
        public int ObjectIndex;
        public float2 Position;
        public float Radius;
    }

    [InternalBufferCapacity(0)]
    public struct CombatPrototypeMapGround : IBufferElementData
    {
        public FixedString64Bytes GroundId;
        public FixedString64Bytes ResourceKey;
        public UnityObjectRef<Material> Material;
        public byte Walkable;
        public float MovementMultiplier;
    }

    [InternalBufferCapacity(0)]
    public struct CombatPrototypeMapBiome : IBufferElementData
    {
        public FixedString64Bytes BiomeId;
        public float TreeDensityPer100m2;
        public float GatherableDensityPer100m2;
    }

    [InternalBufferCapacity(0)]
    public struct CombatPrototypeMapObject : IBufferElementData
    {
        public FixedString64Bytes ObjectId;
        public FixedString64Bytes ResourceKey;
        public Entity Prefab;
    }
}
