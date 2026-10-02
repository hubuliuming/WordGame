using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypeInventoryItem : IBufferElementData
    {
        [GhostField] public FixedString64Bytes ItemName;
        [GhostField] public int Quantity;
    }
}
