using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapGatherToolDurabilityHudSettings : IComponentData
    {
        public byte Enabled;
        public float WarningRatio;
        public float CriticalRatio;
        public float3 WarningColor;
        public float3 CriticalColor;
        public float3 BrokenColor;
        public FixedString64Bytes WarningLabel;
        public FixedString64Bytes CriticalLabel;
        public FixedString64Bytes BrokenLabel;
        public FixedString64Bytes RemainingUsesLabel;
        public FixedString64Bytes RepairHintLabel;
    }
}
