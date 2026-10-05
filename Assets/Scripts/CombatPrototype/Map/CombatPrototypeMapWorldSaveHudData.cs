using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapWorldSaveHudMode : byte
    {
        Hidden, Disabled, NotSaved, Saved, CaptureFailed, SaveFailed
    }

    public enum CombatPrototypeMapWorldSaveManualResult : byte
    {
        None, Success, Disabled, Cooldown, Unavailable, Failed
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapWorldSaveHudState : IComponentData
    {
        [GhostField] public CombatPrototypeMapWorldSaveHudMode Mode;
        [GhostField] public uint ManualSequence;
        [GhostField] public CombatPrototypeMapWorldSaveManualResult ManualResult;

        public static CombatPrototypeMapWorldSaveHudState Hidden => default;
    }

    public struct CombatPrototypeMapWorldSaveHudSettings : IComponentData
    {
        public byte Enabled;
        public float PanelWidthPixels;
        public float PanelHeightPixels;
        public float BottomMarginPixels;
        public int FontSize;
        public float FeedbackSeconds;
        public FixedString64Bytes DisabledLabel;
        public FixedString64Bytes NotSavedLabel;
        public FixedString64Bytes SavedLabel;
        public FixedString64Bytes CaptureFailedLabel;
        public FixedString64Bytes SaveFailedLabel;
        public FixedString64Bytes ManualSaveLabel;
        public FixedString64Bytes ManualDisabledLabel;
        public FixedString64Bytes CooldownLabel;
        public FixedString64Bytes UnavailableLabel;
        public float3 ErrorColor;
    }
}
