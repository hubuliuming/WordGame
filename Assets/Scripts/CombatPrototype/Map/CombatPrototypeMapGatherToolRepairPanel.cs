using System;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    // Read-only repair preview and one-shot button requests inside the original inventory scroll view.
    internal sealed class CombatPrototypeMapGatherToolRepairPanel
    {
        public const int RowCount = 11;
        private CombatPrototypeMapGatherToolSettings _settings;
        private CombatPrototypeMapGatherToolDefinition _axe, _pickaxe;
        private string _heading, _woodLabel, _stoneLabel, _missing, _full, _notOwned, _disabled, _ready;
        private string _axeName, _pickaxeName, _axeButton, _pickaxeButton;
        private string _axeTitle, _pickaxeTitle, _axePreview, _pickaxePreview, _axeRecipe, _pickaxeRecipe, _axeMissing, _pickaxeMissing;
        private int _wood = -1, _stone = -1, _axeDurability = int.MinValue, _pickaxeDurability = int.MinValue;
        private bool _inventoryValid, _canAxe, _canPickaxe, _requestAxe, _requestPickaxe;

        public void Configure(CombatPrototypeMapInventoryPanelSettings panel, CombatPrototypeMapGatherToolSettings settings,
            CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe)
        {
            Reset();
            _settings = settings;
            _axe = axe; _pickaxe = pickaxe;
            _heading = panel.RepairLabel.ToString();
            _woodLabel = panel.WoodLabel.ToString(); _stoneLabel = panel.StoneLabel.ToString();
            _missing = panel.MissingLabel.ToString(); _full = panel.FullDurabilityLabel.ToString();
            _notOwned = panel.NotOwnedLabel.ToString(); _disabled = panel.DisabledLabel.ToString(); _ready = panel.ReadyLabel.ToString();
            _axeName = axe.DisplayName.ToString(); _pickaxeName = pickaxe.DisplayName.ToString();
            _axeButton = "3: " + panel.RepairButtonLabel + " " + _axeName;
            _pickaxeButton = "4: " + panel.RepairButtonLabel + " " + _pickaxeName;
        }

        public void Capture(int wood, int stone, bool inventoryValid, int axeDurability, int pickaxeDurability)
        {
            if (_wood == wood && _stone == stone && _inventoryValid == inventoryValid &&
                _axeDurability == axeDurability && _pickaxeDurability == pickaxeDurability) return;
            _wood = wood; _stone = stone; _inventoryValid = inventoryValid;
            _axeDurability = axeDurability; _pickaxeDurability = pickaxeDurability;
            _canAxe = CanRepair(_axe, axeDurability); _canPickaxe = CanRepair(_pickaxe, pickaxeDurability);
            _axeTitle = _axeName + "  " + Availability(_axe, axeDurability);
            _pickaxeTitle = _pickaxeName + "  " + Availability(_pickaxe, pickaxeDurability);
            _axePreview = Preview(_axe, axeDurability); _pickaxePreview = Preview(_pickaxe, pickaxeDurability);
            _axeRecipe = Recipe(_axe); _pickaxeRecipe = Recipe(_pickaxe);
            _axeMissing = Missing(_axe); _pickaxeMissing = Missing(_pickaxe);
        }

        private bool CanRepair(CombatPrototypeMapGatherToolDefinition definition, int durability) =>
            _settings.Enabled != 0 && _settings.RepairEnabled != 0 && _inventoryValid && durability >= 0 &&
            durability < definition.MaxDurability && _wood >= definition.RepairWoodQuantity && _stone >= definition.RepairStoneQuantity;

        private string Availability(CombatPrototypeMapGatherToolDefinition definition, int durability)
        {
            if (_settings.Enabled == 0 || _settings.RepairEnabled == 0 || !_inventoryValid) return _disabled;
            if (durability < 0) return _notOwned;
            if (durability == definition.MaxDurability) return _full;
            return CanRepair(definition, durability) ? _ready : _missing;
        }

        private string Preview(CombatPrototypeMapGatherToolDefinition definition, int durability)
        {
            if (durability < 0) return _notOwned;
            var gain = Math.Min(definition.RepairDurability, definition.MaxDurability - durability);
            return durability + "/" + definition.MaxDurability + " -> " + (durability + gain) + "/" +
                definition.MaxDurability + " (+" + gain + ")";
        }

        private string Recipe(CombatPrototypeMapGatherToolDefinition definition) =>
            _woodLabel + " " + _wood + "/" + definition.RepairWoodQuantity + "   " +
            _stoneLabel + " " + _stone + "/" + definition.RepairStoneQuantity;

        private string Missing(CombatPrototypeMapGatherToolDefinition definition) =>
            _missing + ": " + _woodLabel + " " + Math.Max(0, definition.RepairWoodQuantity - _wood) + "   " +
            _stoneLabel + " " + Math.Max(0, definition.RepairStoneQuantity - _stone);

        public void Draw(float width, ref float y, float rowHeight, GUIStyle labelStyle, GUIStyle buttonStyle, bool mousePressAccepted)
        {
            Label(width, ref y, rowHeight, labelStyle, _heading);
            DrawTool(width, ref y, rowHeight, labelStyle, buttonStyle, mousePressAccepted,
                _axeTitle, _axePreview, _axeRecipe, _axeMissing, _axeButton, _canAxe, true);
            DrawTool(width, ref y, rowHeight, labelStyle, buttonStyle, mousePressAccepted,
                _pickaxeTitle, _pickaxePreview, _pickaxeRecipe, _pickaxeMissing, _pickaxeButton, _canPickaxe, false);
        }

        private void DrawTool(float width, ref float y, float rowHeight, GUIStyle labelStyle, GUIStyle buttonStyle,
            bool mousePressAccepted, string title, string preview, string recipe, string missing, string button, bool enabled, bool axe)
        {
            Label(width, ref y, rowHeight, labelStyle, title);
            Label(width, ref y, rowHeight, labelStyle, preview);
            Label(width, ref y, rowHeight, labelStyle, recipe);
            Label(width, ref y, rowHeight, labelStyle, missing);
            var oldEnabled = GUI.enabled;
            try
            {
                GUI.enabled = oldEnabled && enabled;
                if (GUI.Button(new Rect(0f, y, width, rowHeight - 4f), button, buttonStyle) && mousePressAccepted)
                {
                    if (axe) _requestAxe = true;
                    else _requestPickaxe = true;
                }
            }
            finally { GUI.enabled = oldEnabled; }
            y += rowHeight;
        }

        private static void Label(float width, ref float y, float rowHeight, GUIStyle style, string text)
        {
            GUI.Label(new Rect(0f, y, width, rowHeight), text, style);
            y += rowHeight;
        }

        public void ReadRequest(out bool axe, out bool pickaxe)
        {
            axe = _requestAxe; pickaxe = _requestPickaxe;
            ClearPending();
        }

        public void ClearPending() { _requestAxe = _requestPickaxe = false; }

        public void Reset()
        {
            ClearPending();
            _settings = default; _axe = _pickaxe = default;
            _wood = _stone = -1; _axeDurability = _pickaxeDurability = int.MinValue;
            _inventoryValid = _canAxe = _canPickaxe = false;
            _heading = _woodLabel = _stoneLabel = _missing = _full = _notOwned = _disabled = _ready = string.Empty;
            _axeName = _pickaxeName = _axeButton = _pickaxeButton = string.Empty;
            _axeTitle = _pickaxeTitle = _axePreview = _pickaxePreview = _axeRecipe = _pickaxeRecipe = _axeMissing = _pickaxeMissing = string.Empty;
        }
    }
}
