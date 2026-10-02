using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypePlayerReward : IComponentData
    {
        [GhostField] public int Coin;
        [GhostField] public int Experience;
    }
}
