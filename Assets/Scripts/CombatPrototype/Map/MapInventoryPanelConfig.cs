using System;

namespace Code_01.CombatPrototype.Map
{
    [Serializable]
    public sealed class MapInventoryPanelConfig
    {
        public bool enabled;
        public bool initiallyOpen;
        public float panelWidthPixels;
        public float panelHeightPixels;
        public float rightMarginPixels;
        public float topMarginPixels;
        public int fontSize;
        public float rowHeightPixels;
        public string panelTitle;
        public string materialsLabel;
        public string capacityLabel;
        public string unlimitedLabel;
        public string toolsLabel;
        public string craftLabel;
        public string craftButtonLabel;
        public string emptyInventoryLabel;
        public string woodLabel;
        public string stoneLabel;
        public string appleLabel;
        public string meatLabel;
        public string closeLabel;
        public string missingLabel;
        public string usableLabel;
        public string brokenLabel;
        public string notOwnedLabel;
        public string disabledLabel;
        public string readyLabel;
        public string repairLabel;
        public string repairButtonLabel;
        public string fullDurabilityLabel;
        public bool sortEnabled;
        public bool filterEnabled;
        public string defaultSortMode;
        public string defaultFilterMode;
        public string sortLabel;
        public string originalOrderLabel;
        public string typeOrderLabel;
        public string quantityOrderLabel;
        public string filterLabel;
        public string allFilterLabel;
        public string resourcesFilterLabel;
        public string suppliesFilterLabel;
        public string otherFilterLabel;
        public string noMatchingItemsLabel;
    }
}
