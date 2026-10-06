using System;
using System.Globalization;
using Unity.Entities;
using UnityEngine;
using ConsumptionOperation = Code_01.CombatPrototype.Map.CombatPrototypeMapInventoryConsumptionConfirmation.Operation;

namespace Code_01.CombatPrototype.Map
{
    // Read-only tool previews and one-shot requests hosted by the original B scroll view.
    internal sealed class CombatPrototypeMapGatherToolUpgradePanel
    {
        public const int RowCount = 14;
        private readonly ToolPreview _axe = new ToolPreview();
        private readonly ToolPreview _pickaxe = new ToolPreview();
        private readonly CombatPrototypeMapGatherToolUpgradeFeedbackClient _feedback = new CombatPrototypeMapGatherToolUpgradeFeedbackClient();
        private string _heading, _levelLabel, _maxLevel, _buttonLabel, _woodLabel, _stoneLabel, _missing, _disabled, _notOwned, _ready;
        public int ConsumptionHintRowCount => (_axe.ConsumptionHint.Length != 0 ? 1 : 0) + (_pickaxe.ConsumptionHint.Length != 0 ? 1 : 0);
        private bool _enabled;

        private sealed class ToolPreview
        {
            internal CombatPrototypeMapGatherToolUpgradeDefinition Second, Third;
            internal string Name, Key, Title, DurabilityText, DurationText, Recipe, Missing, Button, ConsumptionHint;
            internal uint FavoritesRevision;
            internal float BaseDuration;
            internal int Level = -1, Durability = int.MinValue, Wood = -1, Stone = -1;
            internal bool InventoryValid, CanUpgrade, Pending;
        }

        public void Configure(CombatPrototypeMapInventoryPanelSettings panel, CombatPrototypeMapGatherToolSettings tools,
            CombatPrototypeMapGatherToolUpgradeSettings settings, DynamicBuffer<CombatPrototypeMapGatherToolUpgradeDefinition> definitions,
            CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe,
            float treeDuration, float mineDuration, string mapId)
        {
            Reset();
            _enabled = tools.Enabled != 0 && settings.Enabled != 0;
            _heading = settings.UpgradeLabel.ToString(); _levelLabel = settings.LevelLabel.ToString();
            _maxLevel = settings.MaxLevelLabel.ToString(); _buttonLabel = settings.UpgradeButtonLabel.ToString();
            _woodLabel = panel.WoodLabel.ToString(); _stoneLabel = panel.StoneLabel.ToString();
            _missing = panel.MissingLabel.ToString(); _disabled = panel.DisabledLabel.ToString();
            _notOwned = panel.NotOwnedLabel.ToString(); _ready = panel.ReadyLabel.ToString();
            ConfigureTool(_axe, definitions, axe, treeDuration, "6");
            ConfigureTool(_pickaxe, definitions, pickaxe, mineDuration, "7");
            _feedback.Configure(settings, axe, pickaxe, mapId);
        }

        private static void ConfigureTool(ToolPreview preview, DynamicBuffer<CombatPrototypeMapGatherToolUpgradeDefinition> definitions,
            CombatPrototypeMapGatherToolDefinition tool, float duration, string key)
        {
            preview.Second = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(definitions, tool.ToolId, 2);
            preview.Third = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(definitions, tool.ToolId, 3);
            preview.Name = tool.DisplayName.ToString(); preview.BaseDuration = duration; preview.Key = key;
        }

        public void Capture(CombatPrototypeMapInventoryPanelSnapshot snapshot, int axeLevel, int pickaxeLevel,
            CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe,
            CombatPrototypeMapToolUpgradeFeedback feedback, CombatPrototypeMapInventoryPanelFavorites favorites, CombatPrototypeMapInventoryConsumptionConfirmation confirmation)
        {
            _feedback.Observe(feedback);
            CaptureTool(_axe, snapshot, axeLevel, snapshot.AxeDurability, axe, favorites, confirmation, ConsumptionOperation.UpgradeAxe);
            CaptureTool(_pickaxe, snapshot, pickaxeLevel, snapshot.PickaxeDurability, pickaxe, favorites, confirmation, ConsumptionOperation.UpgradePickaxe);
        }

        private void CaptureTool(ToolPreview preview, CombatPrototypeMapInventoryPanelSnapshot snapshot, int level, int durability,
            CombatPrototypeMapGatherToolDefinition current, CombatPrototypeMapInventoryPanelFavorites favorites, CombatPrototypeMapInventoryConsumptionConfirmation confirmation, ConsumptionOperation operation)
        {
            if (preview.Level == level && preview.Durability == durability && preview.Wood == snapshot.WoodQuantity &&
                preview.Stone == snapshot.StoneQuantity && preview.InventoryValid == snapshot.InventoryValid &&
                preview.FavoritesRevision == favorites.Revision) return;
            preview.Level = level; preview.Durability = durability;
            preview.Wood = snapshot.WoodQuantity; preview.Stone = snapshot.StoneQuantity; preview.InventoryValid = snapshot.InventoryValid;
            var owned = durability >= 0;
            var max = level == CombatPrototypeMapGatherToolUtility.MaximumLevel;
            var next = level == 1 ? preview.Second : preview.Third;
            preview.FavoritesRevision = favorites.Revision;
            preview.ConsumptionHint = owned && !max && _enabled ?
                favorites.ConsumptionHint(next.WoodQuantity, next.StoneQuantity) : string.Empty;
            preview.CanUpgrade = owned && !max && _enabled && snapshot.InventoryValid &&
                preview.Wood >= next.WoodQuantity && preview.Stone >= next.StoneQuantity;
            confirmation.Capture(operation, preview.CanUpgrade, next.WoodQuantity, next.StoneQuantity, preview.Wood, preview.Stone,
                level, durability, current.MaxDurability, favorites);
            var state = !_enabled || !snapshot.InventoryValid ? _disabled : !owned ? _notOwned :
                max ? _maxLevel : preview.CanUpgrade ? _ready : _missing;
            preview.Title = preview.Name + (owned ? " " + _levelLabel + " " + level +
                (max ? string.Empty : " -> " + _levelLabel + " " + next.Level) : string.Empty) + "  " + state;
            preview.DurabilityText = !owned ? _notOwned : durability + "/" + current.MaxDurability +
                (max ? string.Empty : " -> " + durability + "/" + next.MaxDurability);
            preview.DurationText = !owned ? _notOwned : Seconds(preview.BaseDuration * current.DurationMultiplier) +
                (max ? string.Empty : " -> " + Seconds(preview.BaseDuration * next.DurationMultiplier));
            preview.Recipe = !owned ? _notOwned : max ? _maxLevel : _woodLabel + " " + preview.Wood + "/" + next.WoodQuantity +
                "   " + _stoneLabel + " " + preview.Stone + "/" + next.StoneQuantity;
            preview.Missing = !owned || max ? string.Empty : _missing + ": " + _woodLabel + " " + Math.Max(0, next.WoodQuantity - preview.Wood) +
                "   " + _stoneLabel + " " + Math.Max(0, next.StoneQuantity - preview.Stone);
            preview.Button = preview.Key + ": " + _buttonLabel + " " + preview.Name +
                (!owned || max ? string.Empty : " " + _levelLabel + " " + next.Level);
        }

        private static string Seconds(float duration) => duration.ToString("0.###", CultureInfo.InvariantCulture) + " s";

        public void Draw(float width, ref float y, float rowHeight, GUIStyle labelStyle, GUIStyle buttonStyle, bool mousePressAccepted, CombatPrototypeMapInventoryConsumptionConfirmation confirmation)
        {
            Label(width, ref y, rowHeight, labelStyle, _heading);
            DrawTool(_axe, width, ref y, rowHeight, labelStyle, buttonStyle, mousePressAccepted, confirmation, ConsumptionOperation.UpgradeAxe);
            DrawTool(_pickaxe, width, ref y, rowHeight, labelStyle, buttonStyle, mousePressAccepted, confirmation, ConsumptionOperation.UpgradePickaxe);
            Label(width, ref y, rowHeight, labelStyle, _feedback.Feedback);
        }

        private static void DrawTool(ToolPreview preview, float width, ref float y, float rowHeight, GUIStyle labelStyle,
            GUIStyle buttonStyle, bool mousePressAccepted, CombatPrototypeMapInventoryConsumptionConfirmation confirmation, ConsumptionOperation operation)
        {
            Label(width, ref y, rowHeight, labelStyle, preview.Title);
            Label(width, ref y, rowHeight, labelStyle, preview.DurabilityText);
            Label(width, ref y, rowHeight, labelStyle, preview.DurationText);
            Label(width, ref y, rowHeight, labelStyle, preview.Recipe);
            if (preview.ConsumptionHint.Length != 0) Label(width, ref y, rowHeight, labelStyle, preview.ConsumptionHint);
            confirmation.DrawPrompt(operation, width, ref y, rowHeight, labelStyle);
            Label(width, ref y, rowHeight, labelStyle, preview.Missing);
            if (confirmation.DrawButtons(operation, width, y, rowHeight, buttonStyle, mousePressAccepted))
            {
                y += rowHeight;
                return;
            }
            var oldEnabled = GUI.enabled;
            try
            {
                GUI.enabled = oldEnabled && preview.CanUpgrade;
                if (GUI.Button(new Rect(0f, y, width, rowHeight - 4f), preview.Button, buttonStyle) && mousePressAccepted) preview.Pending = true;
            }
            finally { GUI.enabled = oldEnabled; }
            y += rowHeight;
        }

        private static void Label(float width, ref float y, float rowHeight, GUIStyle style, string text)
        {
            GUI.Label(new Rect(0f, y, width, rowHeight), text, style);
            y += rowHeight;
        }

        public void ReadRequest(out bool axe, out bool pickaxe) { axe = _axe.Pending; pickaxe = _pickaxe.Pending; ClearPending(); }
        public void ClearPending() { _axe.Pending = _pickaxe.Pending = false; }
        public void Reset()
        {
            _feedback.Reset(); ClearPending();
            ResetTool(_axe); ResetTool(_pickaxe);
            _heading = _levelLabel = _maxLevel = _buttonLabel = _woodLabel = _stoneLabel = _missing = _disabled = _notOwned = _ready = string.Empty;
            _enabled = false;
        }

        private static void ResetTool(ToolPreview preview)
        {
            preview.Second = preview.Third = default;
            preview.Name = preview.Key = preview.Title = preview.DurabilityText = preview.DurationText = preview.Recipe = preview.Missing = preview.Button = preview.ConsumptionHint = string.Empty;
            preview.FavoritesRevision = 0;
            preview.BaseDuration = 0f; preview.Level = preview.Wood = preview.Stone = -1; preview.Durability = int.MinValue;
            preview.InventoryValid = preview.CanUpgrade = preview.Pending = false;
        }
    }
}
