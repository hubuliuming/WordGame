using Unity.Collections;
using Unity.Entities;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapInventoryPanelSettings : IComponentData
    {
        public byte Enabled;
        public byte InitiallyOpen;
        public float PanelWidthPixels;
        public float PanelHeightPixels;
        public float RightMarginPixels;
        public float TopMarginPixels;
        public int FontSize;
        public float RowHeightPixels;
        public FixedString64Bytes PanelTitle;
        public FixedString64Bytes MaterialsLabel;
        public FixedString64Bytes CapacityLabel;
        public FixedString64Bytes UnlimitedLabel;
        public FixedString64Bytes ToolsLabel;
        public FixedString64Bytes CraftLabel;
        public FixedString64Bytes CraftButtonLabel;
        public FixedString64Bytes EmptyInventoryLabel;
        public FixedString64Bytes WoodLabel;
        public FixedString64Bytes StoneLabel;
        public FixedString64Bytes AppleLabel;
        public FixedString64Bytes MeatLabel;
        public FixedString64Bytes CloseLabel;
        public FixedString64Bytes MissingLabel;
        public FixedString64Bytes UsableLabel;
        public FixedString64Bytes BrokenLabel;
        public FixedString64Bytes NotOwnedLabel;
        public FixedString64Bytes DisabledLabel;
        public FixedString64Bytes ReadyLabel;
        public FixedString64Bytes RepairLabel;
        public FixedString64Bytes RepairButtonLabel;
        public FixedString64Bytes FullDurabilityLabel;
        public byte SortEnabled;
        public byte FilterEnabled;
        public CombatPrototypeMapInventorySortMode DefaultSortMode;
        public CombatPrototypeMapInventoryFilterMode DefaultFilterMode;
        public FixedString64Bytes SortLabel;
        public FixedString64Bytes OriginalOrderLabel;
        public FixedString64Bytes TypeOrderLabel;
        public FixedString64Bytes QuantityOrderLabel;
        public FixedString64Bytes FilterLabel;
        public FixedString64Bytes AllFilterLabel;
        public FixedString64Bytes ResourcesFilterLabel;
        public FixedString64Bytes SuppliesFilterLabel;
        public FixedString64Bytes OtherFilterLabel;
        public FixedString64Bytes NoMatchingItemsLabel;
    }
}
