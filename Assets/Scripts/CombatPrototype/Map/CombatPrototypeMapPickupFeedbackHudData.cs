using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapPickupFeedbackHudSettings : IComponentData
    {
        public byte Enabled;
        public float FeedbackSeconds;
        public float3 SuccessColor;
        public float3 FailureColor;
        public FixedString64Bytes SuccessLabel;
        public FixedString64Bytes MovingLabel;
        public FixedString64Bytes AttackingLabel;
        public FixedString64Bytes NoTargetLabel;
        public FixedString64Bytes FailedLabel;
    }
}
