using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapInteractionHudMode : byte { Hidden, Ready, Working }

    [GhostComponent(OwnerSendType = SendToOwnerType.SendToOwner)]
    public struct CombatPrototypeMapInteractionHudState : IComponentData
    {
        [GhostField] public CombatPrototypeMapInteractionHudMode Mode;
        [GhostField] public byte Kind;
        [GhostField] public int PlacementIndex;
        [GhostField] public ushort ProgressPermille;

        public static CombatPrototypeMapInteractionHudState Hidden =>
            new CombatPrototypeMapInteractionHudState { PlacementIndex = -1 };
    }

    public struct CombatPrototypeMapInteractionHudSettings : IComponentData
    {
        public byte Enabled;
        public float PanelWidthPixels;
        public float PanelHeightPixels;
        public float BottomMarginPixels;
        public int FontSize;
        public float ProgressBarHeightPixels;
        public FixedString64Bytes GatherLabel;
        public FixedString64Bytes TreeLabel;
        public FixedString64Bytes MineLabel;
    }
}
