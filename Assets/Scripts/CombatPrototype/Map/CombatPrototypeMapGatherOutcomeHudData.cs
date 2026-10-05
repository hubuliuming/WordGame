using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapGatherOutcomeHudSettings : IComponentData
    {
        public byte Enabled;
        public float FeedbackSeconds;
        public float3 CompletedColor;
        public float3 InterruptedColor;
        public float3 FailedColor;
        public FixedString64Bytes GatherCompletedLabel;
        public FixedString64Bytes TreeCompletedLabel;
        public FixedString64Bytes MineCompletedLabel;
        public FixedString64Bytes MovingLabel;
        public FixedString64Bytes AttackingLabel;
        public FixedString64Bytes HitLabel;
        public FixedString64Bytes OutOfRangeLabel;
        public FixedString64Bytes FailedLabel;
    }
}
