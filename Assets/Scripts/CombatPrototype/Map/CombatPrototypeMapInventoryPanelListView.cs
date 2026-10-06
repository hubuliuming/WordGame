using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapInventorySortMode : byte
    {
        Original = 0,
        Type = 1,
        Quantity = 2
    }

    public enum CombatPrototypeMapInventoryFilterMode : byte
    {
        All = 0,
        Resources = 1,
        Supplies = 2,
        Other = 3
    }

    // Local display rows only. Full inventory validation/counts stay in the original snapshot.
    internal sealed class CombatPrototypeMapInventoryPanelListView
    {
        private static readonly FixedString64Bytes Wood = new FixedString64Bytes(Msg.ItemName.木材);
        private static readonly FixedString64Bytes Stone = new FixedString64Bytes(Msg.ItemName.石材);
        private static readonly FixedString64Bytes Apple = new FixedString64Bytes(Msg.ItemName.活力苹果);
        private static readonly FixedString64Bytes Meat = new FixedString64Bytes(Msg.ItemName.小块肉);
        private static readonly Comparison<CombatPrototypeMapInventoryPanelSnapshot.Row> TypeComparison = CompareType;
        private static readonly Comparison<CombatPrototypeMapInventoryPanelSnapshot.Row> QuantityComparison = CompareQuantity;
        private readonly List<CombatPrototypeMapInventoryPanelSnapshot.Row> _items =
            new List<CombatPrototypeMapInventoryPanelSnapshot.Row>();
        private readonly List<FixedString64Bytes> _previousNames = new List<FixedString64Bytes>();
        private CombatPrototypeMapInventorySortMode _sortMode;
        private CombatPrototypeMapInventoryFilterMode _filterMode;
        private uint _revision, _searchRevision;
        private bool _hasSnapshot, _sortPending, _filterPending;
        private string _sortLabel, _originalLabel, _typeLabel, _quantityLabel;
        private string _filterLabel, _allLabel, _resourcesLabel, _suppliesLabel, _otherLabel;

        public IReadOnlyList<CombatPrototypeMapInventoryPanelSnapshot.Row> Items => _items;
        public bool SortEnabled { get; private set; }
        public bool FilterEnabled { get; private set; }
        public string SortText { get; private set; }
        public string FilterText { get; private set; }
        public string NoMatchingItemsText { get; private set; }
        public CombatPrototypeMapInventorySortMode SortMode => _sortMode;
        public CombatPrototypeMapInventoryFilterMode FilterMode => _filterMode;
        public int ControlRowCount => (SortEnabled ? 1 : 0) + (FilterEnabled ? 1 : 0);

        internal static CombatPrototypeMapInventorySortMode ResolveSortMode(string value)
        {
            switch (value)
            {
                case "original": return CombatPrototypeMapInventorySortMode.Original;
                case "type": return CombatPrototypeMapInventorySortMode.Type;
                case "quantity": return CombatPrototypeMapInventorySortMode.Quantity;
                default: throw new InvalidOperationException("inventoryPanel.defaultSortMode requires original, type or quantity; value=" + value + ".");
            }
        }

        internal static CombatPrototypeMapInventoryFilterMode ResolveFilterMode(string value)
        {
            switch (value)
            {
                case "all": return CombatPrototypeMapInventoryFilterMode.All;
                case "resources": return CombatPrototypeMapInventoryFilterMode.Resources;
                case "supplies": return CombatPrototypeMapInventoryFilterMode.Supplies;
                case "other": return CombatPrototypeMapInventoryFilterMode.Other;
                default: throw new InvalidOperationException("inventoryPanel.defaultFilterMode requires all, resources, supplies or other; value=" + value + ".");
            }
        }

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings)
        {
            Reset();
            SortEnabled = settings.SortEnabled != 0;
            FilterEnabled = settings.FilterEnabled != 0;
            _sortMode = SortEnabled ? settings.DefaultSortMode : CombatPrototypeMapInventorySortMode.Original;
            _filterMode = FilterEnabled ? settings.DefaultFilterMode : CombatPrototypeMapInventoryFilterMode.All;
            _sortLabel = settings.SortLabel.ToString();
            _originalLabel = settings.OriginalOrderLabel.ToString();
            _typeLabel = settings.TypeOrderLabel.ToString();
            _quantityLabel = settings.QuantityOrderLabel.ToString();
            _filterLabel = settings.FilterLabel.ToString();
            _allLabel = settings.AllFilterLabel.ToString();
            _resourcesLabel = settings.ResourcesFilterLabel.ToString();
            _suppliesLabel = settings.SuppliesFilterLabel.ToString();
            _otherLabel = settings.OtherFilterLabel.ToString();
            NoMatchingItemsText = settings.NoMatchingItemsLabel.ToString();
            UpdateLabels();
        }

        // 只在Configure后的首次快照前恢复有效能力，关闭项保持原配置规则。
        public void Restore(CombatPrototypeMapInventorySortMode sortMode, CombatPrototypeMapInventoryFilterMode filterMode)
        {
            if (SortEnabled) _sortMode = sortMode;
            if (FilterEnabled) _filterMode = filterMode;
            UpdateLabels();
        }

        // 已确认的显示重置在Panel.Show中应用，不改库存或重建配置。
        public void ResetDisplay(CombatPrototypeMapInventorySortMode sortMode, CombatPrototypeMapInventoryFilterMode filterMode)
        {
            ClearPending();
            _sortMode = SortEnabled ? sortMode : CombatPrototypeMapInventorySortMode.Original;
            _filterMode = FilterEnabled ? filterMode : CombatPrototypeMapInventoryFilterMode.All;
            _hasSnapshot = false;
            UpdateLabels();
        }

        public bool Capture(CombatPrototypeMapInventoryPanelSnapshot snapshot, CombatPrototypeMapInventoryPanelSearch search, out bool selectionChanged)
        {
            var searchChanged = search.ApplyPending();
            selectionChanged = _sortPending || _filterPending || searchChanged;
            if (_sortPending)
                _sortMode = _sortMode == CombatPrototypeMapInventorySortMode.Type ? CombatPrototypeMapInventorySortMode.Quantity :
                    _sortMode == CombatPrototypeMapInventorySortMode.Quantity ? CombatPrototypeMapInventorySortMode.Original :
                    CombatPrototypeMapInventorySortMode.Type;
            if (_filterPending)
                _filterMode = _filterMode == CombatPrototypeMapInventoryFilterMode.All ? CombatPrototypeMapInventoryFilterMode.Resources :
                    _filterMode == CombatPrototypeMapInventoryFilterMode.Resources ? CombatPrototypeMapInventoryFilterMode.Supplies :
                    _filterMode == CombatPrototypeMapInventoryFilterMode.Supplies ? CombatPrototypeMapInventoryFilterMode.Other :
                    CombatPrototypeMapInventoryFilterMode.All;
            ClearPending();
            if (selectionChanged) UpdateLabels();
            if (_hasSnapshot && _revision == snapshot.Revision && _searchRevision == search.Revision && !selectionChanged) return false;

            _previousNames.Clear();
            foreach (var row in _items) _previousNames.Add(row.Name);
            _items.Clear();
            foreach (var row in snapshot.Items)
                if (Matches(row.Name) && search.Matches(row)) _items.Add(row);
            if (_sortMode == CombatPrototypeMapInventorySortMode.Type) _items.Sort(TypeComparison);
            else if (_sortMode == CombatPrototypeMapInventorySortMode.Quantity) _items.Sort(QuantityComparison);

            var identitiesChanged = _previousNames.Count != _items.Count;
            if (!identitiesChanged)
                for (var index = 0; index < _items.Count; index++)
                    if (!_previousNames[index].Equals(_items[index].Name)) { identitiesChanged = true; break; }
            _revision = snapshot.Revision;
            _searchRevision = search.Revision;
            _hasSnapshot = true;
            return identitiesChanged;
        }

        public void QueueSort() { if (SortEnabled) _sortPending = true; }
        public void QueueFilter() { if (FilterEnabled) _filterPending = true; }
        public void ClearPending() { _sortPending = _filterPending = false; }

        private bool Matches(FixedString64Bytes name)
        {
            var rank = TypeRank(name);
            switch (_filterMode)
            {
                case CombatPrototypeMapInventoryFilterMode.All: return true;
                case CombatPrototypeMapInventoryFilterMode.Resources: return rank < 2;
                case CombatPrototypeMapInventoryFilterMode.Supplies: return rank == 2 || rank == 3;
                case CombatPrototypeMapInventoryFilterMode.Other: return rank == 4;
                default: throw new InvalidOperationException("Unsupported inventory filter mode: " + _filterMode + ".");
            }
        }

        private static int TypeRank(FixedString64Bytes name)
        {
            if (name.Equals(Wood)) return 0;
            if (name.Equals(Stone)) return 1;
            if (name.Equals(Apple)) return 2;
            if (name.Equals(Meat)) return 3;
            return 4;
        }

        private static int CompareType(CombatPrototypeMapInventoryPanelSnapshot.Row left,
            CombatPrototypeMapInventoryPanelSnapshot.Row right)
        {
            var comparison = TypeRank(left.Name).CompareTo(TypeRank(right.Name));
            return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left.OriginalName, right.OriginalName);
        }

        private static int CompareQuantity(CombatPrototypeMapInventoryPanelSnapshot.Row left,
            CombatPrototypeMapInventoryPanelSnapshot.Row right)
        {
            var comparison = right.Quantity.CompareTo(left.Quantity);
            return comparison != 0 ? comparison : CompareType(left, right);
        }

        private void UpdateLabels()
        {
            switch (_sortMode)
            {
                case CombatPrototypeMapInventorySortMode.Original: SortText = _sortLabel + ": " + _originalLabel; break;
                case CombatPrototypeMapInventorySortMode.Type: SortText = _sortLabel + ": " + _typeLabel; break;
                case CombatPrototypeMapInventorySortMode.Quantity: SortText = _sortLabel + ": " + _quantityLabel; break;
                default: throw new InvalidOperationException("Unsupported inventory sort mode: " + _sortMode + ".");
            }
            switch (_filterMode)
            {
                case CombatPrototypeMapInventoryFilterMode.All: FilterText = _filterLabel + ": " + _allLabel; break;
                case CombatPrototypeMapInventoryFilterMode.Resources: FilterText = _filterLabel + ": " + _resourcesLabel; break;
                case CombatPrototypeMapInventoryFilterMode.Supplies: FilterText = _filterLabel + ": " + _suppliesLabel; break;
                case CombatPrototypeMapInventoryFilterMode.Other: FilterText = _filterLabel + ": " + _otherLabel; break;
                default: throw new InvalidOperationException("Unsupported inventory filter mode: " + _filterMode + ".");
            }
        }

        public void Reset()
        {
            _items.Clear();
            _previousNames.Clear();
            SortEnabled = FilterEnabled = _hasSnapshot = false;
            ClearPending();
            _revision = _searchRevision = 0;
            _sortMode = CombatPrototypeMapInventorySortMode.Original;
            _filterMode = CombatPrototypeMapInventoryFilterMode.All;
            _sortLabel = _originalLabel = _typeLabel = _quantityLabel = string.Empty;
            _filterLabel = _allLabel = _resourcesLabel = _suppliesLabel = _otherLabel = string.Empty;
            SortText = FilterText = NoMatchingItemsText = string.Empty;
        }
    }
}
