using System;
using System.Collections.Generic;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    // Presentation projection only. Neither the list nor its material counts write gameplay state.
    internal sealed class CombatPrototypeMapInventoryPanelSnapshot
    {
        internal readonly struct Row
        {
            public readonly FixedString64Bytes Name;
            public readonly string OriginalName;
            public readonly string DisplayName;
            public readonly int Quantity;
            public readonly string Text;

            public Row(FixedString64Bytes name, string originalName, int quantity, string label, int maximum)
            {
                Name = name;
                OriginalName = originalName;
                DisplayName = label;
                Quantity = quantity;
                Text = maximum > 0 ? label + "  " + quantity + "/" + maximum : label + "  x" + quantity;
            }
        }

        private static readonly FixedString64Bytes Wood = new FixedString64Bytes(Msg.ItemName.木材);
        private static readonly FixedString64Bytes Stone = new FixedString64Bytes(Msg.ItemName.石材);
        private static readonly FixedString64Bytes Apple = new FixedString64Bytes(Msg.ItemName.活力苹果);
        private static readonly FixedString64Bytes Meat = new FixedString64Bytes(Msg.ItemName.小块肉);
        private readonly List<Row> _items = new List<Row>();
        private readonly HashSet<FixedString64Bytes> _names = new HashSet<FixedString64Bytes>();
        private string _woodLabel;
        private string _stoneLabel;
        private string _appleLabel;
        private string _meatLabel;
        private string _capacityLabel;
        private string _unlimitedLabel;
        private string _capacityText;
        private int _woodMaximum, _stoneMaximum, _appleMaximum, _totalMaximum;
        private bool _capacityEnabled;
        private int _capacityLevel, _baseTotal, _baseApple, _baseWood, _baseStone;
        private CombatPrototypeMapInventoryCapacityUpgradeDefinition _second, _third;
        private long _lastTotal = -1;

        public IReadOnlyList<Row> Items => _items;
        public uint Revision { get; private set; }
        public int WoodQuantity { get; private set; }
        public int StoneQuantity { get; private set; }
        public int AxeDurability { get; private set; } = -1;
        public int PickaxeDurability { get; private set; } = -1;
        public bool InventoryValid { get; private set; }
        public string CapacityText => _capacityText;
        public int TotalMaximum => _totalMaximum;
        public int AppleMaximum => _appleMaximum;
        public int WoodMaximum => _woodMaximum;
        public int StoneMaximum => _stoneMaximum;

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings,
            CombatPrototypeMapInventoryCapacitySettings capacity, DynamicBuffer<CombatPrototypeMapInventoryCapacityDefinition> definitions,
            DynamicBuffer<CombatPrototypeMapInventoryCapacityUpgradeDefinition> upgradeDefinitions)
        {
            Reset();
            _woodLabel = settings.WoodLabel.ToString();
            _stoneLabel = settings.StoneLabel.ToString();
            _appleLabel = settings.AppleLabel.ToString();
            _meatLabel = settings.MeatLabel.ToString();
            _capacityLabel = settings.CapacityLabel.ToString();
            _unlimitedLabel = settings.UnlimitedLabel.ToString();
            _capacityEnabled = capacity.Enabled != 0;
            _totalMaximum = capacity.MaxTotalQuantity;
            _woodMaximum = CombatPrototypeMapInventoryCapacityUtility.RequireDefinition(definitions, Wood).MaxQuantity;
            _stoneMaximum = CombatPrototypeMapInventoryCapacityUtility.RequireDefinition(definitions, Stone).MaxQuantity;
            _appleMaximum = CombatPrototypeMapInventoryCapacityUtility.RequireDefinition(definitions, Apple).MaxQuantity;
            _baseTotal = _totalMaximum; _baseApple = _appleMaximum; _baseWood = _woodMaximum; _baseStone = _stoneMaximum;
            _second = CombatPrototypeMapInventoryCapacityUtility.RequireUpgradeDefinition(upgradeDefinitions, 2);
            _third = CombatPrototypeMapInventoryCapacityUtility.RequireUpgradeDefinition(upgradeDefinitions, 3);
            _capacityLevel = 1;
        }

        public void Capture(DynamicBuffer<CombatPrototypeInventoryItem> inventory, int axeDurability,
            int pickaxeDurability, Entity source, Entity player, int capacityLevel)
        {
            var changed = ApplyLevel(capacityLevel);
            WoodQuantity = StoneQuantity = 0;
            AxeDurability = axeDurability;
            PickaxeDurability = pickaxeDurability;
            InventoryValid = true;
            _names.Clear();
            var count = 0;
            long total = 0;
            for (var index = 0; index < inventory.Length; index++)
            {
                var item = inventory[index];
                var cached = count < _items.Count && _items[count].Name.Equals(item.ItemName);
                var originalName = cached ? _items[count].OriginalName : item.ItemName.ToString();
                if (!ValidName(originalName) || item.Quantity < 0 || !_names.Add(item.ItemName))
                {
                    InventoryValid = false;
                    Debug.LogError("[CombatPrototype.InventoryPanel] Invalid item; stage=Snapshot, map=" + source +
                        ", player=" + player + ", index=" + index + ", item=" + originalName + ", quantity=" +
                        item.Quantity + ". Expected a unique nonblank name without control characters and nonnegative quantity.");
                    continue;
                }
                // An absent/zero row means zero material, as in the existing crafting system.
                if (item.Quantity == 0) continue;
                if (item.ItemName.Equals(Wood)) WoodQuantity = item.Quantity;
                if (item.ItemName.Equals(Stone)) StoneQuantity = item.Quantity;
                var maximum = Maximum(item.ItemName);
                if (maximum > 0) total += item.Quantity;
                if (!cached || _items[count].Quantity != item.Quantity)
                {
                    var row = new Row(item.ItemName, originalName, item.Quantity, DisplayName(item.ItemName, originalName),
                        _capacityEnabled ? maximum : 0);
                    if (count < _items.Count) _items[count] = row;
                    else _items.Add(row);
                    changed = true;
                }
                count++;
            }
            if (count < _items.Count)
            {
                _items.RemoveRange(count, _items.Count - count);
                changed = true;
            }
            if (changed) Revision = unchecked(Revision + 1);
            if (total != _lastTotal)
            {
                _lastTotal = total;
                _capacityText = _capacityLabel + "  " + total + "/" + (_capacityEnabled ? _totalMaximum.ToString() : _unlimitedLabel);
            }
        }

        private bool ApplyLevel(int level)
        {
            if (_capacityLevel == level) return false;
            CombatPrototypeMapInventoryCapacityUtility.ValidateLevel(level);
            var upgrade = level == 2 ? _second : _third;
            _totalMaximum = level == 1 ? _baseTotal : upgrade.MaxTotalQuantity;
            _appleMaximum = level == 1 ? _baseApple : upgrade.AppleMaxQuantity;
            _woodMaximum = level == 1 ? _baseWood : upgrade.WoodMaxQuantity;
            _stoneMaximum = level == 1 ? _baseStone : upgrade.StoneMaxQuantity;
            _capacityLevel = level;
            _items.Clear();
            _lastTotal = -1;
            return true;
        }

        private int Maximum(FixedString64Bytes name)
        {
            if (name.Equals(Wood)) return _woodMaximum;
            if (name.Equals(Stone)) return _stoneMaximum;
            if (name.Equals(Apple)) return _appleMaximum;
            return 0;
        }

        private static bool ValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            foreach (var character in name)
                if (char.IsControl(character)) return false;
            return true;
        }

        private string DisplayName(FixedString64Bytes name, string originalName)
        {
            if (name.Equals(Wood)) return _woodLabel;
            if (name.Equals(Stone)) return _stoneLabel;
            if (name.Equals(Apple)) return _appleLabel;
            if (name.Equals(Meat)) return _meatLabel;
            return originalName;
        }

        public void Reset()
        {
            _items.Clear();
            _names.Clear();
            Revision = 0;
            WoodQuantity = StoneQuantity = 0;
            AxeDurability = PickaxeDurability = -1;
            InventoryValid = false;
            _lastTotal = -1;
            _capacityText = string.Empty;
            _capacityEnabled = false;
            _woodMaximum = _stoneMaximum = _appleMaximum = _totalMaximum = 0;
            _capacityLevel = _baseTotal = _baseApple = _baseWood = _baseStone = 0;
            _second = _third = default;
        }
    }
}
