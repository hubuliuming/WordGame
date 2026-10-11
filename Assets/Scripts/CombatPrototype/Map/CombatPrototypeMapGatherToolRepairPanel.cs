using System;
using UnityEngine;
using ConsumptionOperation = Code_01.CombatPrototype.Map.CombatPrototypeMapInventoryConsumptionConfirmation.Operation;

namespace Code_01.CombatPrototype.Map
{
    // Read-only repair preview and one-shot button requests inside the original inventory scroll view.
    internal sealed class CombatPrototypeMapGatherToolRepairPanel
    {
        public const int RowCount = 11;
        private string _axeConsumptionHint, _pickaxeConsumptionHint;
        private uint _favoritesRevision;
        public int ConsumptionHintRowCount => (_axeConsumptionHint.Length != 0 ? 1 : 0) + (_pickaxeConsumptionHint.Length != 0 ? 1 : 0);
        private string _levelLabel;
        private int _axeLevel = -1, _pickaxeLevel = -1;
        private CombatPrototypeMapGatherToolSettings _settings;
        private CombatPrototypeMapGatherToolDefinition _axe, _pickaxe;
        private string _heading, _woodLabel, _stoneLabel, _missing, _full, _notOwned, _disabled, _ready;
        private string _axeName, _pickaxeName, _axeButton, _pickaxeButton;
        private string _axeTitle, _pickaxeTitle, _axePreview, _pickaxePreview, _axeRecipe, _pickaxeRecipe, _axeMissing, _pickaxeMissing;
        private int _wood = -1, _stone = -1, _axeDurability = int.MinValue, _pickaxeDurability = int.MinValue;
        private bool _inventoryValid, _canAxe, _canPickaxe, _requestAxe, _requestPickaxe;

        public void Configure(CombatPrototypeMapInventoryPanelSettings panel, CombatPrototypeMapGatherToolSettings settings,
            CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe, string levelLabel)
        {
            Reset();
            _settings = settings; _levelLabel = levelLabel;
            _axe = axe; _pickaxe = pickaxe;
            _heading = panel.RepairLabel.ToString();
            _woodLabel = panel.WoodLabel.ToString(); _stoneLabel = panel.StoneLabel.ToString();
            _missing = panel.MissingLabel.ToString(); _full = panel.FullDurabilityLabel.ToString();
            _notOwned = panel.NotOwnedLabel.ToString(); _disabled = panel.DisabledLabel.ToString(); _ready = panel.ReadyLabel.ToString();
            _axeName = axe.DisplayName.ToString(); _pickaxeName = pickaxe.DisplayName.ToString();
            _axeButton = "3: " + panel.RepairButtonLabel + " " + _axeName;
            _pickaxeButton = "4: " + panel.RepairButtonLabel + " " + _pickaxeName;
        }

        public void Capture(int wood, int stone, bool inventoryValid, int axeDurability, int pickaxeDurability,
            int axeLevel, int pickaxeLevel, CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe,
            CombatPrototypeMapInventoryPanelFavorites favorites, CombatPrototypeMapInventoryConsumptionConfirmation confirmation)
        {
            if (_wood == wood && _stone == stone && _inventoryValid == inventoryValid &&
                _axeDurability == axeDurability && _pickaxeDurability == pickaxeDurability && _axeLevel == axeLevel && _pickaxeLevel == pickaxeLevel &&
                _favoritesRevision == favorites.Revision && _axe.RepairWoodQuantity == axe.RepairWoodQuantity &&
                _axe.RepairStoneQuantity == axe.RepairStoneQuantity && _axe.MaxDurability == axe.MaxDurability &&
                _pickaxe.RepairWoodQuantity == pickaxe.RepairWoodQuantity && _pickaxe.RepairStoneQuantity == pickaxe.RepairStoneQuantity &&
                _pickaxe.MaxDurability == pickaxe.MaxDurability) return;
            _wood = wood; _stone = stone; _inventoryValid = inventoryValid;
            _axeDurability = axeDurability; _pickaxeDurability = pickaxeDurability;
            _axeLevel = axeLevel; _pickaxeLevel = pickaxeLevel; _axe = axe; _pickaxe = pickaxe;
            _favoritesRevision = favorites.Revision;
            _axeConsumptionHint = _settings.Enabled != 0 && _settings.RepairEnabled != 0 && axeDurability >= 0 &&
                axeDurability < _axe.MaxDurability ? favorites.ConsumptionHint(_axe.RepairWoodQuantity, _axe.RepairStoneQuantity) : string.Empty;
            _pickaxeConsumptionHint = _settings.Enabled != 0 && _settings.RepairEnabled != 0 && pickaxeDurability >= 0 &&
                pickaxeDurability < _pickaxe.MaxDurability ? favorites.ConsumptionHint(_pickaxe.RepairWoodQuantity, _pickaxe.RepairStoneQuantity) : string.Empty;
            _canAxe = CanRepair(_axe, axeDurability); _canPickaxe = CanRepair(_pickaxe, pickaxeDurability);
            confirmation.Capture(ConsumptionOperation.RepairAxe, _canAxe, _axe.RepairWoodQuantity, _axe.RepairStoneQuantity,
                wood, stone, axeLevel, axeDurability, _axe.MaxDurability, favorites);
            confirmation.Capture(ConsumptionOperation.RepairPickaxe, _canPickaxe, _pickaxe.RepairWoodQuantity, _pickaxe.RepairStoneQuantity,
                wood, stone, pickaxeLevel, pickaxeDurability, _pickaxe.MaxDurability, favorites);
            _axeTitle = _axeName + (axeLevel > 0 ? " " + _levelLabel + " " + axeLevel : string.Empty) + "  " + Availability(_axe, axeDurability);
            _pickaxeTitle = _pickaxeName + (pickaxeLevel > 0 ? " " + _levelLabel + " " + pickaxeLevel : string.Empty) + "  " + Availability(_pickaxe, pickaxeDurability);
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

        public static int VisibleRowCount(bool axe, bool pickaxe) => !axe && !pickaxe ? 0 : 1 + (axe ? 5 : 0) + (pickaxe ? 5 : 0);
        public int VisibleConsumptionHintRowCount(bool axe, bool pickaxe) =>
            (axe && _axeConsumptionHint.Length != 0 ? 1 : 0) + (pickaxe && _pickaxeConsumptionHint.Length != 0 ? 1 : 0);

        public void Draw(float width, ref float y, float rowHeight, GUIStyle labelStyle, GUIStyle buttonStyle, bool mousePressAccepted,
            CombatPrototypeMapInventoryConsumptionConfirmation confirmation, bool axe, bool pickaxe, CombatPrototypeMapInventoryRecipeFavorites recipeFavorites)
        {
            if (!axe && !pickaxe) return;
            Label(width, ref y, rowHeight, labelStyle, _heading);
            if (axe) DrawTool(width, ref y, rowHeight, labelStyle, buttonStyle, mousePressAccepted,
                _axeTitle, _axePreview, _axeRecipe, _axeConsumptionHint, _axeMissing, _axeButton, _canAxe, true, confirmation, recipeFavorites);
            if (pickaxe) DrawTool(width, ref y, rowHeight, labelStyle, buttonStyle, mousePressAccepted,
                _pickaxeTitle, _pickaxePreview, _pickaxeRecipe, _pickaxeConsumptionHint, _pickaxeMissing, _pickaxeButton, _canPickaxe, false, confirmation, recipeFavorites);
        }

        private void DrawTool(float width, ref float y, float rowHeight, GUIStyle labelStyle, GUIStyle buttonStyle,
            bool mousePressAccepted, string title, string preview, string recipe, string consumptionHint, string missing, string button, bool enabled, bool axe, CombatPrototypeMapInventoryConsumptionConfirmation confirmation, CombatPrototypeMapInventoryRecipeFavorites recipeFavorites)
        {
            var operation = axe ? ConsumptionOperation.RepairAxe : ConsumptionOperation.RepairPickaxe;
            Label(width, ref y, rowHeight, labelStyle, recipeFavorites.Title(operation, title));
            Label(width, ref y, rowHeight, labelStyle, preview);
            Label(width, ref y, rowHeight, labelStyle, recipe);
            if (consumptionHint.Length != 0) Label(width, ref y, rowHeight, labelStyle, consumptionHint);
            confirmation.DrawPrompt(operation, width, ref y, rowHeight, labelStyle);
            Label(width, ref y, rowHeight, labelStyle, missing);
            if (confirmation.DrawButtons(operation, width, y, rowHeight, buttonStyle, mousePressAccepted))
            {
                y += rowHeight;
                recipeFavorites.DrawRow(operation, width, ref y, rowHeight, buttonStyle, mousePressAccepted);
                return;
            }
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
            recipeFavorites.DrawRow(operation, width, ref y, rowHeight, buttonStyle, mousePressAccepted);
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
            _axeConsumptionHint = _pickaxeConsumptionHint = string.Empty; _favoritesRevision = 0;
            _wood = _stone = -1; _axeDurability = _pickaxeDurability = int.MinValue;
            _inventoryValid = _canAxe = _canPickaxe = false;
            _levelLabel = string.Empty; _axeLevel = _pickaxeLevel = -1;
            _heading = _woodLabel = _stoneLabel = _missing = _full = _notOwned = _disabled = _ready = string.Empty;
            _axeName = _pickaxeName = _axeButton = _pickaxeButton = string.Empty;
            _axeTitle = _pickaxeTitle = _axePreview = _pickaxePreview = _axeRecipe = _pickaxeRecipe = _axeMissing = _pickaxeMissing = string.Empty;
        }
    }
}
