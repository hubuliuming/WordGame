using Unity.Collections;
using Unity.Entities;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapInventoryCapacitySettings : IComponentData
    {
        public byte Enabled;
        public int MaxTotalQuantity;
    }

    public struct CombatPrototypeMapInventoryCapacityDefinition : IBufferElementData
    {
        public FixedString64Bytes ItemId;
        public FixedString64Bytes ItemName;
        public int MaxQuantity;
    }
}
