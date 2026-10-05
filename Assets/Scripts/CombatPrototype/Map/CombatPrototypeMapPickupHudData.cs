using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapPickupHudMode : byte { Hidden, Ready }
    public enum CombatPrototypeMapPickupLifetimeHudMode : byte { None, Timed, ExpiringSoon, Permanent }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapPickupHudState : IComponentData
    {
        [GhostField] public CombatPrototypeMapPickupHudMode Mode;
        [GhostField] public int DropId;
        [GhostField] public FixedString64Bytes ItemId;
        [GhostField] public int Quantity;
        [GhostField] public CombatPrototypeMapPickupLifetimeHudMode LifetimeMode;
        [GhostField(Quantization = 0)] public float RemainingSeconds;

        public static CombatPrototypeMapPickupHudState Hidden => default;
    }

    public struct CombatPrototypeMapPickupHudSettings : IComponentData
    {
        public byte Enabled;
        public float PanelWidthPixels;
        public float PanelHeightPixels;
        public float BottomMarginPixels;
        public int FontSize;
        public FixedString64Bytes PickupLabel;
        public FixedString64Bytes AppleLabel;
        public FixedString64Bytes WoodLabel;
        public FixedString64Bytes StoneLabel;
        public byte LifetimeEnabled;
        public byte ExpiryWarningEnabled;
        public float ExpiryWarningSeconds;
        public FixedString64Bytes ExpiresInLabel;
        public FixedString64Bytes PermanentLabel;
        public FixedString64Bytes ExpiringSoonLabel;
        public FixedString64Bytes SecondsLabel;
        public float3 ExpiryWarningColor;
    }
}
