using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapInteractionFailureHudSettings : IComponentData
    {
        public byte Enabled;
        public float FeedbackSeconds;
        public float3 ErrorColor;
        public FixedString64Bytes AlreadyInteractingLabel;
        public FixedString64Bytes MovingLabel;
        public FixedString64Bytes AttackingLabel;
        public FixedString64Bytes NoTargetLabel;
        public FixedString64Bytes TargetUnavailableLabel;
        public FixedString64Bytes FailedLabel;
    }
}
