using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    // Local selection and read-only details, hosted by the existing inventory panel.
    internal sealed class CombatPrototypeMapInventoryPanelDetails
    {
        private static readonly FixedString64Bytes Wood = new FixedString64Bytes(Msg.ItemName.木材);
        private static readonly FixedString64Bytes Stone = new FixedString64Bytes(Msg.ItemName.石材);
        private static readonly FixedString64Bytes Apple = new FixedString64Bytes(Msg.ItemName.活力苹果);
        private static readonly FixedString64Bytes Meat = new FixedString64Bytes(Msg.ItemName.小块肉);
        private readonly List<string> _lines = new List<string>(13);
        private readonly List<float> _heights = new List<float>(13);
        private CombatPrototypeMapGatherToolDefinition _axe, _pickaxe;
        private CombatPrototypeMapGatherToolUpgradeDefinition _axeSecond, _axeThird, _pickaxeSecond, _pickaxeThird;
        private CombatPrototypeMapInventoryCapacityUpgradeDefinition _capacitySecond, _capacityThird;
        private FixedString64Bytes _selected, _pendingName;
        private string _button, _title, _close, _quantity, _description, _uses, _noUses, _unknownDescription;
        private string _woodDescription, _stoneDescription, _appleDescription, _meatDescription, _meatUse;
        private string _type, _unlimited, _craft, _repair, _toolUpgrade, _capacityUpgrade, _toolLevel, _capacityLevelLabel;
        private string _axeName, _pickaxeName;
        private bool _toolsEnabled, _repairEnabled, _toolUpgradeEnabled, _capacityUpgradeEnabled;
        private bool _selectPending, _closePending, _hasCapture, _axeOwned, _pickaxeOwned, _heightDirty;
        private uint _snapshotRevision;
        private int _axeLevel, _pickaxeLevel, _capacityLevel;
        private float _measuredWidth, _measuredRowHeight, _headerHeight, _extraHeight;
        private GUIStyle _wrappedStyle;

        public bool Enabled { get; private set; }
        private bool Visible => Enabled && _selected.Length != 0;

        public void Configure(CombatPrototypeMapInventoryPanelSettings panel, CombatPrototypeMapGatherToolSettings tools,
            CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe,
            CombatPrototypeMapInventoryCapacitySettings capacity, CombatPrototypeMapInventoryCapacityUpgradeSettings capacityUpgrade,
            DynamicBuffer<CombatPrototypeMapInventoryCapacityUpgradeDefinition> capacityDefinitions,
            CombatPrototypeMapGatherToolUpgradeSettings toolUpgrade,
            DynamicBuffer<CombatPrototypeMapGatherToolUpgradeDefinition> toolDefinitions)
        {
            Reset();
            Enabled = panel.DetailsEnabled != 0;
            _toolsEnabled = tools.Enabled != 0;
            _repairEnabled = tools.RepairEnabled != 0;
            _toolUpgradeEnabled = toolUpgrade.Enabled != 0;
            _capacityUpgradeEnabled = capacity.Enabled != 0 && capacityUpgrade.Enabled != 0;
            _axe = axe; _pickaxe = pickaxe;
            _axeSecond = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(toolDefinitions, axe.ToolId, 2);
            _axeThird = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(toolDefinitions, axe.ToolId, 3);
            _pickaxeSecond = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(toolDefinitions, pickaxe.ToolId, 2);
            _pickaxeThird = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(toolDefinitions, pickaxe.ToolId, 3);
            _capacitySecond = CombatPrototypeMapInventoryCapacityUtility.RequireUpgradeDefinition(capacityDefinitions, 2);
            _capacityThird = CombatPrototypeMapInventoryCapacityUtility.RequireUpgradeDefinition(capacityDefinitions, 3);
            _button = panel.DetailsButtonLabel.ToString(); _title = panel.DetailsTitleLabel.ToString();
            _close = panel.DetailsCloseLabel.ToString(); _quantity = panel.DetailsQuantityLabel.ToString();
            _description = panel.DetailsDescriptionLabel.ToString(); _uses = panel.DetailsUsageLabel.ToString();
            _noUses = panel.DetailsNoUsageLabel.ToString(); _unknownDescription = panel.DetailsUnknownDescriptionLabel.ToString();
            _woodDescription = panel.WoodDescriptionLabel.ToString(); _stoneDescription = panel.StoneDescriptionLabel.ToString();
            _appleDescription = panel.AppleDescriptionLabel.ToString(); _meatDescription = panel.MeatDescriptionLabel.ToString();
            _meatUse = panel.MeatUsageLabel.ToString(); _type = panel.TypeOrderLabel.ToString();
            _unlimited = panel.UnlimitedLabel.ToString(); _craft = panel.CraftLabel.ToString(); _repair = panel.RepairLabel.ToString();
            _toolUpgrade = toolUpgrade.UpgradeLabel.ToString(); _capacityUpgrade = capacityUpgrade.UpgradeLabel.ToString();
            _toolLevel = toolUpgrade.LevelLabel.ToString(); _capacityLevelLabel = capacityUpgrade.LevelLabel.ToString();
            _axeName = axe.DisplayName.ToString(); _pickaxeName = pickaxe.DisplayName.ToString();
        }

        // Apply queued GUI choices only after the full snapshot and visible list have refreshed.
        public bool Capture(CombatPrototypeMapInventoryPanelSnapshot snapshot, CombatPrototypeMapInventoryPanelListView list,
            int axeLevel, int pickaxeLevel, CombatPrototypeMapGatherToolDefinition effectiveAxe,
            CombatPrototypeMapGatherToolDefinition effectivePickaxe, int capacityLevel)
        {
            if (!Enabled) return false;
            var changed = false;
            if (_closePending) { changed = Visible; Close(); }
            if (_selectPending)
            {
                if (!_selected.Equals(_pendingName))
                {
                    _selected = _pendingName;
                    _hasCapture = false;
                    changed = true;
                }
                _selectPending = false;
                _pendingName = default;
            }
            if (!Visible) return changed;
            var found = false;
            var row = default(CombatPrototypeMapInventoryPanelSnapshot.Row);
            foreach (var item in list.Items)
                if (item.Name.Equals(_selected)) { row = item; found = true; break; }
            if (!found) { Close(); return true; }
            var axeOwned = snapshot.AxeDurability >= 0;
            var pickaxeOwned = snapshot.PickaxeDurability >= 0;
            if (_hasCapture && _snapshotRevision == snapshot.Revision && _axeLevel == axeLevel &&
                _pickaxeLevel == pickaxeLevel && _capacityLevel == capacityLevel &&
                _axeOwned == axeOwned && _pickaxeOwned == pickaxeOwned) return changed;
            _snapshotRevision = snapshot.Revision; _axeLevel = axeLevel; _pickaxeLevel = pickaxeLevel;
            _capacityLevel = capacityLevel; _axeOwned = axeOwned; _pickaxeOwned = pickaxeOwned;
            BuildLines(row, snapshot.DisplayMaximum(row.Name), list.CategoryLabel(row.Name), effectiveAxe, effectivePickaxe);
            _hasCapture = true;
            _heightDirty = true;
            return true;
        }

        private void BuildLines(CombatPrototypeMapInventoryPanelSnapshot.Row row, int maximum, string category,
            CombatPrototypeMapGatherToolDefinition effectiveAxe, CombatPrototypeMapGatherToolDefinition effectivePickaxe)
        {
            _lines.Clear();
            _lines.Add(row.DisplayName);
            _lines.Add(_type + ": " + category);
            _lines.Add(_quantity + "  " + row.Quantity + "/" + (maximum > 0 ? maximum.ToString() : _unlimited));
            _lines.Add(_description + ": " + Description(row.Name));
            _lines.Add(_uses + ":");
            var before = _lines.Count;
            if (row.Name.Equals(Wood) || row.Name.Equals(Stone))
            {
                if (_toolsEnabled)
                {
                    AddUse(row, _craft + " " + _axeName + " " + _toolLevel + " 1", _axe.CraftWoodQuantity, _axe.CraftStoneQuantity);
                    AddUse(row, _craft + " " + _pickaxeName + " " + _toolLevel + " 1", _pickaxe.CraftWoodQuantity, _pickaxe.CraftStoneQuantity);
                    if (_repairEnabled)
                    {
                        if (_axeOwned) AddUse(row, _repair + " " + _axeName + " " + _toolLevel + " " + _axeLevel,
                            effectiveAxe.RepairWoodQuantity, effectiveAxe.RepairStoneQuantity);
                        if (_pickaxeOwned) AddUse(row, _repair + " " + _pickaxeName + " " + _toolLevel + " " + _pickaxeLevel,
                            effectivePickaxe.RepairWoodQuantity, effectivePickaxe.RepairStoneQuantity);
                    }
                    if (_toolUpgradeEnabled)
                    {
                        if (_axeOwned && _axeLevel < CombatPrototypeMapGatherToolUtility.MaximumLevel)
                            AddToolUpgrade(row, _axeName, _axeLevel == 1 ? _axeSecond : _axeThird);
                        if (_pickaxeOwned && _pickaxeLevel < CombatPrototypeMapGatherToolUtility.MaximumLevel)
                            AddToolUpgrade(row, _pickaxeName, _pickaxeLevel == 1 ? _pickaxeSecond : _pickaxeThird);
                    }
                }
                if (_capacityUpgradeEnabled && _capacityLevel < 3)
                {
                    var next = _capacityLevel == 1 ? _capacitySecond : _capacityThird;
                    AddUse(row, _capacityUpgrade + " " + _capacityLevelLabel + " " + next.Level, next.WoodQuantity, next.StoneQuantity);
                }
            }
            else if (row.Name.Equals(Meat)) _lines.Add(_meatUse);
            if (_lines.Count == before) _lines.Add(_noUses);
        }

        private void AddToolUpgrade(CombatPrototypeMapInventoryPanelSnapshot.Row row, string name,
            CombatPrototypeMapGatherToolUpgradeDefinition next) =>
            AddUse(row, _toolUpgrade + " " + name + " " + _toolLevel + " " + next.Level, next.WoodQuantity, next.StoneQuantity);

        private void AddUse(CombatPrototypeMapInventoryPanelSnapshot.Row row, string operation, int wood, int stone)
        {
            var required = row.Name.Equals(Wood) ? wood : stone;
            if (required > 0) _lines.Add(operation + ": " + row.DisplayName + " x" + required);
        }

        private string Description(FixedString64Bytes name)
        {
            if (name.Equals(Wood)) return _woodDescription;
            if (name.Equals(Stone)) return _stoneDescription;
            if (name.Equals(Apple)) return _appleDescription;
            if (name.Equals(Meat)) return _meatDescription;
            return _unknownDescription;
        }

        public void DrawItemLabel(float width, ref float y, float rowHeight, GUIStyle labelStyle, GUIStyle buttonStyle,
            CombatPrototypeMapInventoryPanelSnapshot.Row row, string itemText, bool mousePressAccepted)
        {
            if (Enabled)
            {
                var buttonWidth = width * 0.3f;
                GUI.Label(new Rect(0f, y, width - buttonWidth - 4f, rowHeight), itemText, labelStyle);
                if (GUI.Button(new Rect(width - buttonWidth, y, buttonWidth, rowHeight - 4f), _button, buttonStyle) && mousePressAccepted)
                {
                    _pendingName = row.Name;
                    _selectPending = true;
                    _closePending = false;
                }
            }
            else GUI.Label(new Rect(0f, y, width, rowHeight), itemText, labelStyle);
            y += rowHeight;
        }

        public float ExtraHeight(float width, float rowHeight, GUIStyle labelStyle)
        {
            if (!Visible) return 0f;
            if (_wrappedStyle == null)
            {
                _wrappedStyle = new GUIStyle(labelStyle) { wordWrap = true, alignment = TextAnchor.UpperLeft };
                _heightDirty = true;
            }
            if (_heightDirty || _measuredWidth != width || _measuredRowHeight != rowHeight)
            {
                _heights.Clear();
                _headerHeight = Mathf.Max(rowHeight, _wrappedStyle.CalcHeight(new GUIContent(_title), width));
                _extraHeight = _headerHeight + rowHeight;
                foreach (var line in _lines)
                {
                    var height = Mathf.Max(rowHeight, _wrappedStyle.CalcHeight(new GUIContent(line), width));
                    _heights.Add(height);
                    _extraHeight += height;
                }
                _measuredWidth = width; _measuredRowHeight = rowHeight;
                _heightDirty = false;
            }
            return _extraHeight;
        }

        public void DrawExpanded(float width, ref float y, float rowHeight, GUIStyle labelStyle, GUIStyle buttonStyle,
            FixedString64Bytes name, bool mousePressAccepted)
        {
            if (!Visible || !name.Equals(_selected)) return;
            ExtraHeight(width, rowHeight, labelStyle);
            GUI.Label(new Rect(0f, y, width, _headerHeight), _title, _wrappedStyle);
            y += _headerHeight;
            if (GUI.Button(new Rect(0f, y, width, rowHeight - 4f), _close, buttonStyle) && mousePressAccepted)
            {
                _closePending = true;
                _selectPending = false;
                _pendingName = default;
            }
            y += rowHeight;
            for (var index = 0; index < _lines.Count; index++)
            {
                GUI.Label(new Rect(0f, y, width, _heights[index]), _lines[index], _wrappedStyle);
                y += _heights[index];
            }
        }

        public void Close()
        {
            _selected = _pendingName = default;
            _selectPending = _closePending = _hasCapture = false;
            _lines.Clear(); _heights.Clear();
            _headerHeight = _extraHeight = 0f;
            _heightDirty = true;
        }

        public void Reset()
        {
            Close();
            Enabled = _toolsEnabled = _repairEnabled = _toolUpgradeEnabled = _capacityUpgradeEnabled = false;
            _axeOwned = _pickaxeOwned = false;
            _snapshotRevision = 0;
            _axeLevel = _pickaxeLevel = _capacityLevel = 0;
            _axe = _pickaxe = default;
            _axeSecond = _axeThird = _pickaxeSecond = _pickaxeThird = default;
            _capacitySecond = _capacityThird = default;
            _wrappedStyle = null;
            _measuredWidth = _measuredRowHeight = 0f;
            _button = _title = _close = _quantity = _description = _uses = _noUses = _unknownDescription = string.Empty;
            _woodDescription = _stoneDescription = _appleDescription = _meatDescription = _meatUse = string.Empty;
            _type = _unlimited = _craft = _repair = _toolUpgrade = _capacityUpgrade = _toolLevel = _capacityLevelLabel = string.Empty;
            _axeName = _pickaxeName = string.Empty;
        }
    }
}