using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code_01.CombatPrototype.Map
{
    // Hosted by the existing camera HUD. Preferences are local; gameplay inventory/save state is not mutated.
    internal sealed class CombatPrototypeMapInventoryPanel
    {
        private readonly CombatPrototypeMapInventoryPanelSnapshot _snapshot = new CombatPrototypeMapInventoryPanelSnapshot();
        private readonly CombatPrototypeMapInventoryPanelListView _listView = new CombatPrototypeMapInventoryPanelListView();
        private readonly CombatPrototypeMapInventoryPanelSearch _search = new CombatPrototypeMapInventoryPanelSearch();
        private readonly CombatPrototypeMapInventoryPanelPreferences _preferences = new CombatPrototypeMapInventoryPanelPreferences();
        private readonly CombatPrototypeMapInventoryPanelFavorites _favorites = new CombatPrototypeMapInventoryPanelFavorites();
        private readonly CombatPrototypeMapInventoryPanelDetails _details = new CombatPrototypeMapInventoryPanelDetails();
        private readonly CombatPrototypeMapInventoryDropClient _drop = new CombatPrototypeMapInventoryDropClient();
        private readonly CombatPrototypeMapGatherToolRepairPanel _repair = new CombatPrototypeMapGatherToolRepairPanel();
        private readonly CombatPrototypeMapInventoryCapacityUpgradePanel _upgrade = new CombatPrototypeMapInventoryCapacityUpgradePanel();
        private readonly CombatPrototypeMapGatherToolUpgradePanel _toolUpgrade = new CombatPrototypeMapGatherToolUpgradePanel();
        private string _levelLabel, _recraftLabel;
        private int _axeLevel = -1, _pickaxeLevel = -1;
        private bool _durabilityEnabled;
        private CombatPrototypeMapGatherToolDurabilityHudClient.ToolStatus _axeDurabilityStatus, _pickaxeDurabilityStatus;
        private CombatPrototypeMapInventoryPanelSettings _settings;
        private CombatPrototypeMapGatherToolSettings _toolSettings;
        private CombatPrototypeMapGatherToolDefinition _axe;
        private CombatPrototypeMapGatherToolDefinition _pickaxe;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private Vector2 _scroll;
        private string _title, _materials, _tools, _craft, _craftButton, _empty, _close, _preferencesResetLabel;
        private string _wood, _stone, _missing, _usable, _broken, _notOwned, _disabled, _readyLabel;
        private string _axeName, _pickaxeName, _axeStatus, _pickaxeStatus, _axeRecipe, _pickaxeRecipe;
        private string _axeMissing, _pickaxeMissing, _axeCraftTitle, _pickaxeCraftTitle, _axeButton, _pickaxeButton, _feedback;
        private int _woodQuantity = -1, _stoneQuantity = -1, _axeDurability = int.MinValue, _pickaxeDurability = int.MinValue;
        private string _axeConsumptionHint, _pickaxeConsumptionHint;
        private uint _favoritesRevision;
        private int _consumptionHintRows;
        private int _lastInputFrame = -1;
        private bool _configured, _ready, _open, _craftAxe, _craftPickaxe, _mousePressAccepted;
        private bool _canCraftAxe, _canCraftPickaxe, _lastInventoryValid;
        private bool _rowMousePressAccepted, _preferencesResetPending;

        private bool PreferencesResetEnabled => _settings.PreferencesResetEnabled != 0;

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings, CombatPrototypeMapGatherToolSettings toolSettings,
            CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe,
            CombatPrototypeMapInventoryDropSettings dropSettings, DynamicBuffer<CombatPrototypeMapInventoryDropDefinition> dropDefinitions,
            CombatPrototypeMapInventoryCapacitySettings capacity, DynamicBuffer<CombatPrototypeMapInventoryCapacityDefinition> capacityDefinitions,
            CombatPrototypeMapInventoryCapacityUpgradeSettings upgradeSettings,
            DynamicBuffer<CombatPrototypeMapInventoryCapacityUpgradeDefinition> upgradeDefinitions,
            CombatPrototypeMapGatherToolUpgradeSettings toolUpgradeSettings,
            DynamicBuffer<CombatPrototypeMapGatherToolUpgradeDefinition> toolUpgradeDefinitions, bool durabilityEnabled, float treeDuration, float mineDuration, string mapId)
        {
            Reset();
            _settings = settings;
            _toolSettings = toolSettings;
            _durabilityEnabled = durabilityEnabled;
            _axe = axe;
            _pickaxe = pickaxe;
            _levelLabel = toolUpgradeSettings.LevelLabel.ToString(); _recraftLabel = toolUpgradeSettings.RecraftLabel.ToString();
            _title = settings.PanelTitle.ToString();
            _materials = settings.MaterialsLabel.ToString();
            _tools = settings.ToolsLabel.ToString();
            _craft = settings.CraftLabel.ToString();
            _craftButton = settings.CraftButtonLabel.ToString();
            _empty = settings.EmptyInventoryLabel.ToString();
            _close = settings.CloseLabel.ToString();
            _preferencesResetLabel = settings.PreferencesResetLabel.ToString();
            _wood = settings.WoodLabel.ToString();
            _stone = settings.StoneLabel.ToString();
            _missing = settings.MissingLabel.ToString();
            _usable = settings.UsableLabel.ToString();
            _broken = settings.BrokenLabel.ToString();
            _notOwned = settings.NotOwnedLabel.ToString();
            _disabled = settings.DisabledLabel.ToString();
            _readyLabel = settings.ReadyLabel.ToString();
            _axeName = axe.DisplayName.ToString();
            _pickaxeName = pickaxe.DisplayName.ToString();
            _axeButton = "1: " + _craftButton + " " + _axeName;
            _pickaxeButton = "2: " + _craftButton + " " + _pickaxeName;
            _snapshot.Configure(settings, capacity, capacityDefinitions, upgradeDefinitions);
            _listView.Configure(settings);
            _search.Configure(settings);
            _favorites.Configure(settings);
            _preferences.Configure(settings, mapId, _listView, _search, _favorites);
            _details.Configure(settings, toolSettings, axe, pickaxe, capacity, upgradeSettings, upgradeDefinitions,
                toolUpgradeSettings, toolUpgradeDefinitions);
            _upgrade.Configure(settings, capacity, upgradeSettings, upgradeDefinitions, mapId);
            _drop.Configure(dropSettings, dropDefinitions, settings);
            _repair.Configure(settings, toolSettings, axe, pickaxe, _levelLabel);
            _toolUpgrade.Configure(settings, toolSettings, toolUpgradeSettings, toolUpgradeDefinitions, axe, pickaxe, treeDuration, mineDuration, mapId);
            _configured = true;
            _open = settings.Enabled != 0 && settings.InitiallyOpen != 0;
        }

        public void Show(DynamicBuffer<CombatPrototypeInventoryItem> inventory, int axeDurability, int pickaxeDurability,
            int axeLevel, int pickaxeLevel, CombatPrototypeMapGatherToolDefinition effectiveAxe, CombatPrototypeMapGatherToolDefinition effectivePickaxe,
            CombatPrototypeMapGatherToolDurabilityHudClient.ToolStatus axeStatus, CombatPrototypeMapGatherToolDurabilityHudClient.ToolStatus pickaxeStatus,
            string feedback, CombatPrototypeMapInventoryDropFeedback dropFeedback, int capacityLevel,
            CombatPrototypeMapInventoryCapacityUpgradeFeedback upgradeFeedback, CombatPrototypeMapToolUpgradeFeedback toolUpgradeFeedback, Entity source, Entity player)
        {
            if (_settings.Enabled == 0) return;
            _axeDurabilityStatus = axeStatus; _pickaxeDurabilityStatus = pickaxeStatus;
            _snapshot.Capture(inventory, axeDurability, pickaxeDurability, source, player, capacityLevel);
            if (_preferencesResetPending)
            {
                _preferencesResetPending = false;
                _listView.ResetDisplay(_settings.DefaultSortMode, _settings.DefaultFilterMode, _settings.DefaultFavoritesOnly != 0);
                _search.ResetDisplay();
                _favorites.ResetDisplay();
                _details.Close();
                _scroll = Vector2.zero;
                _mousePressAccepted = _rowMousePressAccepted = false;
            }
            var favoritesRevision = _favorites.Revision;
            var favoritesOnly = _listView.FavoritesOnly;
            if (_listView.Capture(_snapshot, _search, _favorites, out var selectionChanged)) _rowMousePressAccepted = false;
            if (selectionChanged) _scroll = Vector2.zero;
            if (favoritesRevision != _favorites.Revision || favoritesOnly != _listView.FavoritesOnly) _mousePressAccepted = _rowMousePressAccepted = false;
            if (_details.Capture(_snapshot, _listView, axeLevel, pickaxeLevel, effectiveAxe, effectivePickaxe, capacityLevel))
                _mousePressAccepted = _rowMousePressAccepted = false;
            _preferences.Capture(_listView, _search, _favorites);
            _upgrade.Capture(_snapshot, capacityLevel, upgradeFeedback, _favorites);
            _canCraftAxe = CanCraft(_axe, axeDurability);
            _canCraftPickaxe = CanCraft(_pickaxe, pickaxeDurability);
            _repair.Capture(_snapshot.WoodQuantity, _snapshot.StoneQuantity, _snapshot.InventoryValid, axeDurability, pickaxeDurability,
                axeLevel, pickaxeLevel, effectiveAxe, effectivePickaxe, _favorites);
            _toolUpgrade.Capture(_snapshot, axeLevel, pickaxeLevel, effectiveAxe, effectivePickaxe, toolUpgradeFeedback, _favorites);
            if (_woodQuantity != _snapshot.WoodQuantity || _stoneQuantity != _snapshot.StoneQuantity ||
                _axeDurability != axeDurability || _pickaxeDurability != pickaxeDurability ||
                _axeLevel != axeLevel || _pickaxeLevel != pickaxeLevel || _lastInventoryValid != _snapshot.InventoryValid ||
                _favoritesRevision != _favorites.Revision)
            {
                _woodQuantity = _snapshot.WoodQuantity;
                _stoneQuantity = _snapshot.StoneQuantity;
                _axeDurability = axeDurability;
                _pickaxeDurability = pickaxeDurability;
                _lastInventoryValid = _snapshot.InventoryValid;
                _axeLevel = axeLevel; _pickaxeLevel = pickaxeLevel;
                _axeStatus = ToolStatus(effectiveAxe, _axeName, axeDurability, axeLevel, axeStatus);
                _pickaxeStatus = ToolStatus(effectivePickaxe, _pickaxeName, pickaxeDurability, pickaxeLevel, pickaxeStatus);
                _favoritesRevision = _favorites.Revision;
                _axeConsumptionHint = _toolSettings.Enabled != 0 && axeDurability < _axe.DurabilityCostPerCompletion ?
                    _favorites.ConsumptionHint(_axe.CraftWoodQuantity, _axe.CraftStoneQuantity) : string.Empty;
                _pickaxeConsumptionHint = _toolSettings.Enabled != 0 && pickaxeDurability < _pickaxe.DurabilityCostPerCompletion ?
                    _favorites.ConsumptionHint(_pickaxe.CraftWoodQuantity, _pickaxe.CraftStoneQuantity) : string.Empty;
                _axeRecipe = Recipe(_axe);
                _pickaxeRecipe = Recipe(_pickaxe);
                _axeMissing = Missing(_axe);
                _pickaxeMissing = Missing(_pickaxe);
                _axeCraftTitle = _axeName + "  " + Availability(_axe, axeDurability);
                _axeButton = "1: " + (axeDurability >= 0 && axeDurability < _axe.DurabilityCostPerCompletion ? _recraftLabel : _craftButton) + " " + _axeName;
                _pickaxeButton = "2: " + (pickaxeDurability >= 0 && pickaxeDurability < _pickaxe.DurabilityCostPerCompletion ? _recraftLabel : _craftButton) + " " + _pickaxeName;
                _pickaxeCraftTitle = _pickaxeName + "  " + Availability(_pickaxe, pickaxeDurability);
            }
            var hintRows = (_axeConsumptionHint.Length != 0 ? 1 : 0) + (_pickaxeConsumptionHint.Length != 0 ? 1 : 0) +
                _repair.ConsumptionHintRowCount + _upgrade.ConsumptionHintRowCount + _toolUpgrade.ConsumptionHintRowCount;
            if (_consumptionHintRows != hintRows) _mousePressAccepted = _rowMousePressAccepted = false;
            _consumptionHintRows = hintRows;
            _drop.Observe(dropFeedback);
            _feedback = string.IsNullOrEmpty(_drop.Feedback) ? feedback : _drop.Feedback;
            _ready = true;
        }

        private bool CanCraft(CombatPrototypeMapGatherToolDefinition definition, int durability) =>
            _toolSettings.Enabled != 0 && _snapshot.InventoryValid && durability < definition.DurabilityCostPerCompletion &&
            _snapshot.WoodQuantity >= definition.CraftWoodQuantity && _snapshot.StoneQuantity >= definition.CraftStoneQuantity;

        private string ToolStatus(CombatPrototypeMapGatherToolDefinition definition, string name, int durability, int level,
            CombatPrototypeMapGatherToolDurabilityHudClient.ToolStatus status)
        {
            var text = name + "  " + (durability < 0 ? _notOwned : _levelLabel + " " + level + " " + durability + "/" + definition.MaxDurability);
            if (_toolSettings.Enabled == 0) return text + "  " + _disabled;
            if (_durabilityEnabled && status.WarningLabel.Length != 0) return text + "  " + status.WarningLabel;
            return durability >= 0 && durability < definition.DurabilityCostPerCompletion ? text + "  " + _broken : text;
        }

        private string Recipe(CombatPrototypeMapGatherToolDefinition definition) =>
            _wood + " " + _snapshot.WoodQuantity + "/" + definition.CraftWoodQuantity + "   " +
            _stone + " " + _snapshot.StoneQuantity + "/" + definition.CraftStoneQuantity;

        private string Missing(CombatPrototypeMapGatherToolDefinition definition) =>
            _missing + ": " + _wood + " " + Mathf.Max(0, definition.CraftWoodQuantity - _snapshot.WoodQuantity) + "   " +
            _stone + " " + Mathf.Max(0, definition.CraftStoneQuantity - _snapshot.StoneQuantity);

        private string Availability(CombatPrototypeMapGatherToolDefinition definition, int durability)
        {
            if (_toolSettings.Enabled == 0 || !_snapshot.InventoryValid) return _disabled;
            if (durability >= definition.DurabilityCostPerCompletion) return _usable;
            return _snapshot.WoodQuantity >= definition.CraftWoodQuantity && _snapshot.StoneQuantity >= definition.CraftStoneQuantity ?
                _readyLabel : _missing;
        }

        // Called once the binding has rechecked the current World/map/local living player.
        public bool ReadInput(Keyboard keyboard, Mouse mouse, out bool craftAxe, out bool craftPickaxe, out bool repairAxe, out bool repairPickaxe,
            out CombatPrototypeMapInventoryDropRequest dropRequest, out bool upgrade, out bool upgradeAxe, out bool upgradePickaxe, out bool blocksKeyboard)
        {
            craftAxe = craftPickaxe = repairAxe = repairPickaxe = false;
            dropRequest = default;
            upgrade = upgradeAxe = upgradePickaxe = blocksKeyboard = false;
            if (!_configured || !_ready || _settings.Enabled == 0)
            {
                _craftAxe = _craftPickaxe = _mousePressAccepted = _rowMousePressAccepted = _preferencesResetPending = false;
                _search.ReleaseFocus();
                _details.Close();
                _favorites.ClearPending();
                _listView.ClearFavoritesFilterPending();
                _drop.ClearPending();
                _repair.ClearPending();
                _upgrade.ClearPending();
                _toolUpgrade.ClearPending();
                return false;
            }
            var wasInside = ContainsMouse(mouse);
            if (_lastInputFrame != Time.frameCount)
            {
                _lastInputFrame = Time.frameCount;
                _search.ReadInput(keyboard, mouse, ContainsSearchField(mouse), wasInside);
                if (!_search.BlocksKeyboard && keyboard != null && keyboard.bKey.wasPressedThisFrame)
                {
                    if (_open) Close();
                    else _open = true;
                }
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    _mousePressAccepted = _rowMousePressAccepted = ContainsMouse(mouse);
            }
            blocksKeyboard = _search.BlocksKeyboard;
            var inside = ContainsMouse(mouse);
            if (_open)
            {
                craftAxe = _craftAxe;
                craftPickaxe = _craftPickaxe;
                dropRequest = _drop.ReadRequest(_favorites);
                _repair.ReadRequest(out repairAxe, out repairPickaxe);
                upgrade = _upgrade.ReadRequest();
                _toolUpgrade.ReadRequest(out upgradeAxe, out upgradePickaxe);
            }
            _craftAxe = _craftPickaxe = false;
            // Closing by B must also consume the mouse press that began over this panel.
            return wasInside || inside || _search.BlocksMouse;
        }

        private bool ContainsMouse(Mouse mouse)
        {
            if (!_open || mouse == null || Screen.width <= 0 || Screen.height <= 0) return false;
            var scale = Scale();
            var point = mouse.position.ReadValue();
            return Rect(scale).Contains(new Vector2(point.x / scale, (Screen.height - point.y) / scale));
        }

        private bool ContainsSearchField(Mouse mouse)
        {
            if (!_search.Enabled || !ContainsMouse(mouse)) return false;
            var scale = Scale();
            var viewport = Viewport(Rect(scale));
            var point = mouse.position.ReadValue();
            var local = new Vector2(point.x / scale, (Screen.height - point.y) / scale);
            var field = CombatPrototypeMapInventoryPanelSearch.FieldRect(viewport.width - 18f,
                (3 + _favorites.CountRowCount + _listView.ControlRowCount) * _settings.RowHeightPixels, _settings.RowHeightPixels);
            field.position += viewport.position - _scroll;
            return viewport.Contains(local) && field.Contains(local);
        }

        private Rect Viewport(Rect panel) => new Rect(panel.x + 12f, panel.y + 12f + _settings.RowHeightPixels,
            panel.width - 24f, panel.height - 3f * _settings.RowHeightPixels - 24f);

        private static float Scale() => Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        private Rect Rect(float scale) => new Rect(Screen.width / scale - _settings.RightMarginPixels - _settings.PanelWidthPixels,
            _settings.TopMarginPixels, _settings.PanelWidthPixels, _settings.PanelHeightPixels);

        public void Draw()
        {
            _search.FlushGUIFocus();
            if (!_configured || !_ready || !_open || Screen.width <= 0 || Screen.height <= 0) return;
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = _settings.FontSize, richText = false,
                    alignment = TextAnchor.MiddleLeft, wordWrap = false };
                _labelStyle.normal.textColor = Color.white;
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = _settings.FontSize, richText = false };
            }
            var scale = Scale();
            var panel = Rect(scale);
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            var oldEnabled = GUI.enabled;
            var mouseUp = Event.current.rawType == EventType.MouseUp;
            try
            {
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, _settings.RowHeightPixels), _title, _labelStyle);
                var viewport = Viewport(panel);
                var rows = Mathf.Max(_listView.Items.Count * (_favorites.Enabled ? 3 : 2), 1) + _favorites.CountRowCount + _listView.ControlRowCount + _search.RowCount + (PreferencesResetEnabled ? 1 : 0) + 14 + CombatPrototypeMapGatherToolRepairPanel.RowCount + CombatPrototypeMapInventoryCapacityUpgradePanel.RowCount + CombatPrototypeMapGatherToolUpgradePanel.RowCount + (_durabilityEnabled ? 2 : 0) + _consumptionHintRows;
                var contentWidth = viewport.width - 18f;
                var content = new Rect(0f, 0f, contentWidth, rows * _settings.RowHeightPixels +
                    _details.ExtraHeight(contentWidth, _settings.RowHeightPixels, _labelStyle));
                _scroll = GUI.BeginScrollView(viewport, _scroll, content);
                try { DrawBody(content.width); }
                finally { GUI.EndScrollView(); }
                GUI.Label(new Rect(panel.x + 12f, panel.yMax - 12f - 2f * _settings.RowHeightPixels,
                    panel.width - 24f, _settings.RowHeightPixels), _feedback, _labelStyle);
                if (GUI.Button(new Rect(panel.x + 12f, panel.yMax - 12f - _settings.RowHeightPixels,
                    panel.width - 24f, _settings.RowHeightPixels), _close, _buttonStyle) && _mousePressAccepted) Close();
            }
            finally
            {
                if (mouseUp) _mousePressAccepted = _rowMousePressAccepted = false;
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
                GUI.enabled = oldEnabled;
            }
        }

        private void DrawBody(float width)
        {
            var y = 0f;
            Label(width, ref y, _materials);
            Label(width, ref y, _snapshot.CapacityText);
            if (_favorites.CountRowCount != 0) Label(width, ref y, _favorites.CountText);
            DrawListControls(width, ref y);
            if (_search.Enabled)
            {
                Label(width, ref y, _search.Label);
                _search.Draw(width, y, _settings.RowHeightPixels, _labelStyle, _buttonStyle, _mousePressAccepted);
                y += _settings.RowHeightPixels;
            }
            DrawPreferencesReset(width, ref y);
            if (_listView.Items.Count == 0)
                Label(width, ref y, _snapshot.Items.Count == 0 ? _empty : _listView.FavoritesOnly ? _listView.NoMatchingFavoritesText : _search.HasQuery ? _search.NoResultsText : _listView.NoMatchingItemsText);
            foreach (var item in _listView.Items)
            {
                _details.DrawItemLabel(width, ref y, _settings.RowHeightPixels, _labelStyle, _buttonStyle, item, _favorites.ItemText(item), _rowMousePressAccepted);
                _drop.DrawRow(width, y, _settings.RowHeightPixels, _labelStyle, _buttonStyle, item,
                    _snapshot.InventoryValid, _rowMousePressAccepted, _favorites);
                y += _settings.RowHeightPixels;
                _favorites.DrawRow(width, ref y, _settings.RowHeightPixels, _buttonStyle, item, _rowMousePressAccepted);
                _details.DrawExpanded(width, ref y, _settings.RowHeightPixels, _labelStyle, _buttonStyle, item.Name, _mousePressAccepted);
            }
            _upgrade.Draw(width, ref y, _settings.RowHeightPixels, _labelStyle, _buttonStyle, _mousePressAccepted);
            Label(width, ref y, _tools);
            ToolLabel(width, ref y, _axeStatus, _axeDurabilityStatus.TextColor);
            if (_durabilityEnabled) Label(width, ref y, _axeDurabilityStatus.Detail);
            ToolLabel(width, ref y, _pickaxeStatus, _pickaxeDurabilityStatus.TextColor);
            if (_durabilityEnabled) Label(width, ref y, _pickaxeDurabilityStatus.Detail);
            Label(width, ref y, _craft);
            DrawRecipe(width, ref y, _axeCraftTitle, _axeButton, _axeRecipe, _axeConsumptionHint, _axeMissing, _canCraftAxe, true);
            DrawRecipe(width, ref y, _pickaxeCraftTitle, _pickaxeButton, _pickaxeRecipe, _pickaxeConsumptionHint, _pickaxeMissing, _canCraftPickaxe, false);
            _repair.Draw(width, ref y, _settings.RowHeightPixels, _labelStyle, _buttonStyle, _mousePressAccepted);
            _toolUpgrade.Draw(width, ref y, _settings.RowHeightPixels, _labelStyle, _buttonStyle, _mousePressAccepted);
        }

        private void DrawListControls(float width, ref float y)
        {
            if (_listView.SortEnabled)
            {
                if (GUI.Button(new Rect(0f, y, width, _settings.RowHeightPixels - 4f),
                    _listView.SortText, _buttonStyle) && _mousePressAccepted) _listView.QueueSort();
                y += _settings.RowHeightPixels;
            }
            if (_listView.FilterEnabled)
            {
                if (GUI.Button(new Rect(0f, y, width, _settings.RowHeightPixels - 4f),
                    _listView.FilterText, _buttonStyle) && _mousePressAccepted) _listView.QueueFilter();
                y += _settings.RowHeightPixels;
            }
            if (_listView.FavoritesFilterEnabled)
            {
                if (GUI.Button(new Rect(0f, y, width, _settings.RowHeightPixels - 4f),
                    _listView.FavoritesFilterText, _buttonStyle) && _mousePressAccepted) _listView.QueueFavoritesFilter();
                y += _settings.RowHeightPixels;
            }
        }

        private void DrawPreferencesReset(float width, ref float y)
        {
            if (!PreferencesResetEnabled) return;
            if (GUI.Button(new Rect(0f, y, width, _settings.RowHeightPixels - 4f),
                _preferencesResetLabel, _buttonStyle) && _mousePressAccepted) _preferencesResetPending = true;
            y += _settings.RowHeightPixels;
        }

        private void Label(float width, ref float y, string text)
        {
            GUI.Label(new Rect(0f, y, width, _settings.RowHeightPixels), text, _labelStyle);
            y += _settings.RowHeightPixels;
        }

        private void ToolLabel(float width, ref float y, string text, Color color)
        {
            var oldColor = GUI.color;
            try { GUI.color = color; Label(width, ref y, text); }
            finally { GUI.color = oldColor; }
        }

        private void DrawRecipe(float width, ref float y, string title, string button, string recipe, string consumptionHint, string missing,
            bool enabled, bool axe)
        {
            Label(width, ref y, title);
            Label(width, ref y, recipe);
            if (consumptionHint.Length != 0) Label(width, ref y, consumptionHint);
            Label(width, ref y, missing);
            var oldEnabled = GUI.enabled;
            try
            {
                GUI.enabled = oldEnabled && enabled;
                if (GUI.Button(new Rect(0f, y, width, _settings.RowHeightPixels - 4f),
                    button, _buttonStyle) && _mousePressAccepted)
                {
                    if (axe) _craftAxe = true;
                    else _craftPickaxe = true;
                }
            }
            finally { GUI.enabled = oldEnabled; }
            y += _settings.RowHeightPixels;
        }

        private void Close()
        {
            _preferences.Flush();
            _open = _craftAxe = _craftPickaxe = _mousePressAccepted = _rowMousePressAccepted = _preferencesResetPending = false;
            _listView.ClearPending();
            _search.Close();
            _details.Close();
            _favorites.ClearPending();
            _drop.ClearPending();
            _repair.ClearPending();
            _upgrade.ClearPending();
            _toolUpgrade.ClearPending();
            _scroll = Vector2.zero;
        }

        public void Clear() { _ready = false; _upgrade.ClearPending(); _toolUpgrade.ClearPending(); }

        public void Reset()
        {
            Close();
            _configured = _ready = _durabilityEnabled = false;
            _axeDurabilityStatus = _pickaxeDurabilityStatus = CombatPrototypeMapGatherToolDurabilityHudClient.ToolStatus.Plain;
            _snapshot.Reset();
            _listView.Reset();
            _search.Reset();
            _preferences.Reset();
            _details.Reset();
            _favorites.Reset();
            _drop.Reset();
            _repair.Reset();
            _upgrade.Reset();
            _toolUpgrade.Reset();
            _labelStyle = _buttonStyle = null;
            _feedback = _preferencesResetLabel = string.Empty;
            _woodQuantity = _stoneQuantity = -1;
            _axeDurability = _pickaxeDurability = int.MinValue;
            _axeConsumptionHint = _pickaxeConsumptionHint = string.Empty;
            _favoritesRevision = 0; _consumptionHintRows = 0;
            _lastInputFrame = -1;
            _lastInventoryValid = false;
            _levelLabel = _recraftLabel = string.Empty; _axeLevel = _pickaxeLevel = -1;
        }
    }
}
