using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapResourceStatusHudMode : byte
    {
        Hidden, Available, Working, Occupied, Regrowing, Waiting, Depleted
    }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapResourceStatusHudState : IComponentData
    {
        [GhostField] public CombatPrototypeMapResourceStatusHudMode Mode;
        [GhostField] public byte Kind;
        [GhostField] public int PlacementIndex;
        [GhostField(Quantization = 0)] public float RemainingSeconds;

        public static CombatPrototypeMapResourceStatusHudState Hidden =>
            new CombatPrototypeMapResourceStatusHudState { PlacementIndex = -1 };
    }

    public struct CombatPrototypeMapResourceStatusHudSettings : IComponentData
    {
        public byte Enabled;
        public float PanelWidthPixels;
        public float PanelHeightPixels;
        public float BottomMarginPixels;
        public int FontSize;
        public FixedString64Bytes AvailableLabel;
        public FixedString64Bytes WorkingLabel;
        public FixedString64Bytes OccupiedLabel;
        public FixedString64Bytes RegrowingLabel;
        public FixedString64Bytes WaitingLabel;
        public FixedString64Bytes DepletedLabel;
    }
}
