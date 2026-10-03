using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapSpawnUtility
    {
        public static float3 PlayerPosition(in CombatPrototypeMapData map, int networkId) =>
            map.PlayerSpawnOrigin + new float3(networkId * map.PlayerSpawnSpacing, 0f, 0f);
    }
}
