using System;
using System.Collections.Generic;
using UnityEngine;
using Operation = Code_01.CombatPrototype.Map.CombatPrototypeMapInventoryConsumptionConfirmation.Operation;

namespace Code_01.CombatPrototype.Map
{
    // Stable local recipe identities, pending favorite selection and two cached display partitions.
    internal sealed class CombatPrototypeMapInventoryRecipeFavorites
    {
        public const int MaximumStoredCount = 7;
        private static readonly string[] Ids = { "", "craft_axe", "craft_pickaxe", "repair_axe", "repair_pickaxe", "capacity_upgrade", "upgrade_axe", "upgrade_pickaxe" };
        private readonly string[] _baseTitles = new string[8], _markedTitles = new string[8];
        private byte _favorites;
        private Operation _pending;
        private int _maximum, _count;
        private string _favorite, _unfavorite, _tag, _full;

        internal readonly struct Section
        {
            private readonly byte _mask;
            internal Section(byte mask) { _mask = mask; }
            public bool HasResults => _mask != 0;
            public bool CraftAxe => Contains(Operation.CraftAxe);
            public bool CraftPickaxe => Contains(Operation.CraftPickaxe);
            public bool RepairAxe => Contains(Operation.RepairAxe);
            public bool RepairPickaxe => Contains(Operation.RepairPickaxe);
            public bool CapacityUpgrade => Contains(Operation.CapacityUpgrade);
            public bool UpgradeAxe => Contains(Operation.UpgradeAxe);
            public bool UpgradePickaxe => Contains(Operation.UpgradePickaxe);
            public bool Contains(Operation operation) => (_mask & Bit(operation)) != 0;
            public int Count
            {
                get { var count = 0; for (var index = 1; index <= MaximumStoredCount; index++) if (Contains((Operation)index)) count++; return count; }
            }
            public int RowCount => (CraftAxe || CraftPickaxe ? 1 + (CraftAxe ? 4 : 0) + (CraftPickaxe ? 4 : 0) : 0) +
                CombatPrototypeMapGatherToolRepairPanel.VisibleRowCount(RepairAxe, RepairPickaxe) +
                (CapacityUpgrade ? CombatPrototypeMapInventoryCapacityUpgradePanel.RowCount : 0) +
                CombatPrototypeMapGatherToolUpgradePanel.VisibleRowCount(UpgradeAxe, UpgradePickaxe);
        }

        public bool Enabled { get; private set; }
        public uint Revision { get; private set; }
        public Section Favorites { get; private set; }
        public Section Others { get; private set; }
        public string FavoritesLabel { get; private set; }
        public string OthersLabel { get; private set; }
        public int RowCount => Favorites.RowCount + Others.RowCount +
            (Enabled ? Favorites.Count + Others.Count : 0) +
            (Favorites.HasResults ? 1 + (Others.HasResults ? 1 : 0) : 0);

        private static byte Bit(Operation operation) => (byte)(1 << ((int)operation - 1));
        public bool IsFavorite(Operation operation) => Enabled && (_favorites & Bit(operation)) != 0;

        internal static bool TryResolveId(string id, out Operation operation)
        {
            switch (id)
            {
                case "craft_axe": operation = Operation.CraftAxe; return true;
                case "craft_pickaxe": operation = Operation.CraftPickaxe; return true;
                case "repair_axe": operation = Operation.RepairAxe; return true;
                case "repair_pickaxe": operation = Operation.RepairPickaxe; return true;
                case "capacity_upgrade": operation = Operation.CapacityUpgrade; return true;
                case "upgrade_axe": operation = Operation.UpgradeAxe; return true;
                case "upgrade_pickaxe": operation = Operation.UpgradePickaxe; return true;
                default: operation = Operation.None; return false;
            }
        }

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings)
        {
            Reset();
            Enabled = settings.RecipeFavoritesEnabled != 0;
            _maximum = settings.RecipeFavoritesMaxCount;
            _favorite = settings.RecipeFavoriteButtonLabel.ToString();
            _unfavorite = settings.RecipeUnfavoriteButtonLabel.ToString();
            _tag = settings.RecipeFavoriteTagLabel.ToString();
            _full = settings.RecipeFavoritesFullLabel.ToString();
            FavoritesLabel = settings.FavoriteRecipesLabel.ToString();
            OthersLabel = settings.OtherRecipesLabel.ToString();
        }

        // Store validates the entire record before restoration; configured limits do not prune stored identities.
        public void Restore(IReadOnlyList<string> ids)
        {
            if (!Enabled) return;
            foreach (var id in ids)
            {
                TryResolveId(id, out var operation);
                _favorites |= Bit(operation);
            }
            _count = ids.Count;
            if (_count != 0) Revision++;
        }

        public bool Capture(CombatPrototypeMapInventoryRecipeSearch search)
        {
            var visible = (byte)((search.CraftAxe ? Bit(Operation.CraftAxe) : 0) |
                (search.CraftPickaxe ? Bit(Operation.CraftPickaxe) : 0) |
                (search.RepairAxe ? Bit(Operation.RepairAxe) : 0) |
                (search.RepairPickaxe ? Bit(Operation.RepairPickaxe) : 0) |
                (search.CapacityUpgrade ? Bit(Operation.CapacityUpgrade) : 0) |
                (search.UpgradeAxe ? Bit(Operation.UpgradeAxe) : 0) |
                (search.UpgradePickaxe ? Bit(Operation.UpgradePickaxe) : 0));
            var operation = _pending;
            ClearPending();
            var changed = false;
            if (Enabled && operation != Operation.None && (visible & Bit(operation)) != 0)
            {
                var bit = Bit(operation);
                if ((_favorites & bit) != 0) { _favorites &= (byte)~bit; _count--; changed = true; }
                else if (_count < _maximum) { _favorites |= bit; _count++; changed = true; }
                if (changed) Revision++;
            }
            Favorites = new Section(Enabled ? (byte)(visible & _favorites) : (byte)0);
            Others = new Section(Enabled ? (byte)(visible & ~_favorites) : visible);
            return changed;
        }

        public void CopyIds(List<string> destination)
        {
            destination.Clear();
            for (var index = 1; index <= MaximumStoredCount; index++)
                if ((_favorites & Bit((Operation)index)) != 0) destination.Add(Ids[index]);
            destination.Sort(StringComparer.Ordinal);
        }

        // Only caches title decoration; GUI never applies favorites, reorders the projection or writes a file.
        public string Title(Operation operation, string title)
        {
            if (!IsFavorite(operation)) return title;
            var index = (int)operation;
            if (_baseTitles[index] != title)
            {
                _baseTitles[index] = title;
                _markedTitles[index] = title + " [" + _tag + "]";
            }
            return _markedTitles[index];
        }

        public void DrawRow(Operation operation, float width, ref float y, float rowHeight, GUIStyle style, bool mousePressAccepted)
        {
            if (!Enabled) return;
            var favorite = IsFavorite(operation);
            var allowed = favorite || _count < _maximum;
            var oldEnabled = GUI.enabled;
            try
            {
                GUI.enabled = oldEnabled && allowed;
                if (GUI.Button(new Rect(0f, y, width, rowHeight - 4f), favorite ? _unfavorite : allowed ? _favorite : _full, style) && mousePressAccepted)
                    _pending = operation;
            }
            finally { GUI.enabled = oldEnabled; }
            y += rowHeight;
        }

        public void ClearPending() { _pending = Operation.None; }
        public bool ResetDisplay()
        {
            ClearPending();
            if (!Enabled || _favorites == 0) return false;
            _favorites = 0; _count = 0; Revision++;
            return true;
        }
        public void Reset()
        {
            ClearPending();
            Enabled = false; Revision = 0; _favorites = 0; _count = _maximum = 0;
            Favorites = Others = default;
            _favorite = _unfavorite = _tag = _full = FavoritesLabel = OthersLabel = string.Empty;
            for (var index = 0; index < _baseTitles.Length; index++) _baseTitles[index] = _markedTitles[index] = string.Empty;
        }
    }
}
