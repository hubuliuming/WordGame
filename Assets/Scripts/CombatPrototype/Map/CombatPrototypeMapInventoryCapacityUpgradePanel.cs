using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ConsumptionOperation = Code_01.CombatPrototype.Map.CombatPrototypeMapInventoryConsumptionConfirmation.Operation;

namespace Code_01.CombatPrototype.Map
{
    // A read-only tier/recipe preview and one-shot request inside the existing inventory scroll view.
    internal sealed class CombatPrototypeMapInventoryCapacityUpgradePanel
    {
        public const int RowCount = 10;
        private readonly CombatPrototypeMapInventoryCapacityUpgradeFeedbackClient _feedback = new CombatPrototypeMapInventoryCapacityUpgradeFeedbackClient();
        private CombatPrototypeMapInventoryCapacityUpgradeDefinition _second, _third;
        private string _heading, _levelLabel, _maxLevel, _capacity, _unlimited, _appleLabel, _woodLabel, _stoneLabel;
        private string _missingLabel, _disabledLabel, _readyLabel, _buttonLabel;
        private string _levelText, _totalText, _appleText, _woodText, _stoneText, _recipeText, _missingText, _button;
        private string _consumptionHint;
        private uint _favoritesRevision;
        public int ConsumptionHintRowCount => _consumptionHint.Length != 0 ? 1 : 0;
        private int _level = -1, _wood = -1, _stone = -1;
        private bool _capacityEnabled, _upgradeEnabled, _inventoryValid, _canUpgrade, _request;

        public void Configure(CombatPrototypeMapInventoryPanelSettings panel, CombatPrototypeMapInventoryCapacitySettings capacity,
            CombatPrototypeMapInventoryCapacityUpgradeSettings settings,
            DynamicBuffer<CombatPrototypeMapInventoryCapacityUpgradeDefinition> definitions, string mapId)
        {
            Reset();
            _capacityEnabled = capacity.Enabled != 0;
            _upgradeEnabled = settings.Enabled != 0;
            _second = CombatPrototypeMapInventoryCapacityUtility.RequireUpgradeDefinition(definitions, 2);
            _third = CombatPrototypeMapInventoryCapacityUtility.RequireUpgradeDefinition(definitions, 3);
            _heading = settings.UpgradeLabel.ToString();
            _levelLabel = settings.LevelLabel.ToString();
            _maxLevel = settings.MaxLevelLabel.ToString();
            _buttonLabel = settings.UpgradeButtonLabel.ToString();
            _capacity = panel.CapacityLabel.ToString(); _unlimited = panel.UnlimitedLabel.ToString();
            _appleLabel = panel.AppleLabel.ToString(); _woodLabel = panel.WoodLabel.ToString(); _stoneLabel = panel.StoneLabel.ToString();
            _missingLabel = panel.MissingLabel.ToString(); _disabledLabel = panel.DisabledLabel.ToString(); _readyLabel = panel.ReadyLabel.ToString();
            _feedback.Configure(settings, mapId);
        }

        public void Capture(CombatPrototypeMapInventoryPanelSnapshot snapshot, int level,
            CombatPrototypeMapInventoryCapacityUpgradeFeedback feedback, CombatPrototypeMapInventoryPanelFavorites favorites, CombatPrototypeMapInventoryConsumptionConfirmation confirmation)
        {
            _feedback.Observe(feedback);
            if (_level == level && _wood == snapshot.WoodQuantity && _stone == snapshot.StoneQuantity &&
                _inventoryValid == snapshot.InventoryValid && _favoritesRevision == favorites.Revision) return;
            CombatPrototypeMapInventoryCapacityUtility.ValidateLevel(level);
            _level = level; _wood = snapshot.WoodQuantity; _stone = snapshot.StoneQuantity; _inventoryValid = snapshot.InventoryValid;
            var max = level == CombatPrototypeMapInventoryCapacityUtility.MaximumLevel;
            var next = level == 1 ? _second : _third;
            _favoritesRevision = favorites.Revision;
            _consumptionHint = !max && _capacityEnabled && _upgradeEnabled ?
                favorites.ConsumptionHint(next.WoodQuantity, next.StoneQuantity) : string.Empty;
            _canUpgrade = !max && _capacityEnabled && _upgradeEnabled && _inventoryValid &&
                _wood >= next.WoodQuantity && _stone >= next.StoneQuantity;
            confirmation.Capture(ConsumptionOperation.CapacityUpgrade, _canUpgrade, next.WoodQuantity, next.StoneQuantity,
                _wood, _stone, level, 0, 0, favorites);
            var state = !_capacityEnabled || !_upgradeEnabled || !_inventoryValid ? _disabledLabel :
                max ? _maxLevel : _canUpgrade ? _readyLabel : _missingLabel;
            _levelText = _levelLabel + " " + level + (max ? string.Empty : " -> " + _levelLabel + " " + next.Level) + "  " + state;
            _totalText = Limit(_capacity, snapshot.TotalMaximum, next.MaxTotalQuantity, max);
            _appleText = Limit(_appleLabel, snapshot.AppleMaximum, next.AppleMaxQuantity, max);
            _woodText = Limit(_woodLabel, snapshot.WoodMaximum, next.WoodMaxQuantity, max);
            _stoneText = Limit(_stoneLabel, snapshot.StoneMaximum, next.StoneMaxQuantity, max);
            _recipeText = max ? _maxLevel : _woodLabel + " " + _wood + "/" + next.WoodQuantity + "   " +
                _stoneLabel + " " + _stone + "/" + next.StoneQuantity;
            _missingText = max ? string.Empty : _missingLabel + ": " + _woodLabel + " " + Math.Max(0, next.WoodQuantity - _wood) +
                "   " + _stoneLabel + " " + Math.Max(0, next.StoneQuantity - _stone);
            _button = "5: " + _buttonLabel + (max ? string.Empty : " " + _levelLabel + " " + next.Level);
        }

        private string Limit(string label, int current, int next, bool max) =>
            label + "  " + (!_capacityEnabled ? _unlimited : current + (max ? string.Empty : " -> " + next));

        public void Draw(float width, ref float y, float rowHeight, GUIStyle labelStyle, GUIStyle buttonStyle, bool mousePressAccepted, CombatPrototypeMapInventoryConsumptionConfirmation confirmation, CombatPrototypeMapInventoryRecipeFavorites recipeFavorites)
        {
            Label(width, ref y, rowHeight, labelStyle, recipeFavorites.Title(ConsumptionOperation.CapacityUpgrade, _heading));
            Label(width, ref y, rowHeight, labelStyle, _levelText);
            Label(width, ref y, rowHeight, labelStyle, _totalText);
            Label(width, ref y, rowHeight, labelStyle, _appleText);
            Label(width, ref y, rowHeight, labelStyle, _woodText);
            Label(width, ref y, rowHeight, labelStyle, _stoneText);
            Label(width, ref y, rowHeight, labelStyle, _recipeText);
            if (_consumptionHint.Length != 0) Label(width, ref y, rowHeight, labelStyle, _consumptionHint);
            confirmation.DrawPrompt(ConsumptionOperation.CapacityUpgrade, width, ref y, rowHeight, labelStyle);
            Label(width, ref y, rowHeight, labelStyle, _missingText);
            if (!confirmation.DrawButtons(ConsumptionOperation.CapacityUpgrade, width, y, rowHeight, buttonStyle, mousePressAccepted))
            {
                var oldEnabled = GUI.enabled;
                try
                {
                    GUI.enabled = oldEnabled && _canUpgrade;
                    if (GUI.Button(new Rect(0f, y, width, rowHeight - 4f), _button, buttonStyle) && mousePressAccepted) _request = true;
                }
                finally { GUI.enabled = oldEnabled; }
            }
            y += rowHeight;
            recipeFavorites.DrawRow(ConsumptionOperation.CapacityUpgrade, width, ref y, rowHeight, buttonStyle, mousePressAccepted);
            Label(width, ref y, rowHeight, labelStyle, _feedback.Feedback);
        }

        private static void Label(float width, ref float y, float rowHeight, GUIStyle style, string text)
        {
            GUI.Label(new Rect(0f, y, width, rowHeight), text, style);
            y += rowHeight;
        }

        public bool ReadRequest() { var request = _request; _request = false; return request; }
        public void ClearPending() { _request = false; }
        public void Reset()
        {
            _feedback.Reset();
            _second = _third = default;
            _heading = _levelLabel = _maxLevel = _capacity = _unlimited = _appleLabel = _woodLabel = _stoneLabel = string.Empty;
            _missingLabel = _disabledLabel = _readyLabel = _buttonLabel = string.Empty;
            _levelText = _totalText = _appleText = _woodText = _stoneText = _recipeText = _missingText = _button = string.Empty;
            _consumptionHint = string.Empty; _favoritesRevision = 0;
            _level = _wood = _stone = -1;
            _capacityEnabled = _upgradeEnabled = _inventoryValid = _canUpgrade = _request = false;
        }
    }
}
