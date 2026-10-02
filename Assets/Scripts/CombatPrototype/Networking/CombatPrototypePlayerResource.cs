using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypePlayerResource : IComponentData
    {
        [GhostField] public int CurrentPower;
        [GhostField] public int UpperPower;
    }
}
