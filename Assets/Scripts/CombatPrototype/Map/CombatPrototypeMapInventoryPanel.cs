using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code_01.CombatPrototype.Map
{
    // Hosted by the existing camera HUD. No scene component, inventory mutation or save call.
    internal sealed class CombatPrototypeMapInventoryPanel
    {
        private readonly CombatPrototypeMapInventoryPanelSnapshot _snapshot = new CombatPrototypeMapInventoryPanelSnapshot();
        private readonly CombatPrototypeMapInventoryDropClient _drop = new CombatPrototypeMapInventoryDropClient();
        private readonly CombatPrototypeMapGatherToolRepairPanel _repair = new CombatPrototypeMapGatherToolRepairPanel();
        private CombatPrototypeMapInventoryPanelSettings _settings;
        private CombatPrototypeMapGatherToolSettings _toolSettings;
        private CombatPrototypeMapGatherToolDefinition _axe;
        private CombatPrototypeMapGatherToolDefinition _pickaxe;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private Vector2 _scroll;
        private string _title, _materials, _tools, _craft, _craftButton, _empty, _close;
        private string _wood, _stone, _missing, _usable, _broken, _notOwned, _disabled, _readyLabel;
        private string _axeName, _pickaxeName, _axeStatus, _pickaxeStatus, _axeRecipe, _pickaxeRecipe;
        private string _axeMissing, _pickaxeMissing, _axeCraftTitle, _pickaxeCraftTitle, _axeButton, _pickaxeButton, _feedback;
        private int _woodQuantity = -1, _stoneQuantity = -1, _axeDurability = int.MinValue, _pickaxeDurability = int.MinValue;
        private int _lastInputFrame = -1;
        private bool _configured, _ready, _open, _craftAxe, _craftPickaxe, _mousePressAccepted;
        private bool _canCraftAxe, _canCraftPickaxe, _lastInventoryValid;

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings, CombatPrototypeMapGatherToolSettings toolSettings,
            CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe,
            CombatPrototypeMapInventoryDropSettings dropSettings, DynamicBuffer<CombatPrototypeMapInventoryDropDefinition> dropDefinitions)
        {
            Reset();
            _settings = settings;
            _toolSettings = toolSettings;
            _axe = axe;
            _pickaxe = pickaxe;
            _title = settings.PanelTitle.ToString();
            _materials = settings.MaterialsLabel.ToString();
            _tools = settings.ToolsLabel.ToString();
            _craft = settings.CraftLabel.ToString();
            _craftButton = settings.CraftButtonLabel.ToString();
            _empty = settings.EmptyInventoryLabel.ToString();
            _close = settings.CloseLabel.ToString();
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
            _snapshot.Configure(settings);
            _drop.Configure(dropSettings, dropDefinitions, settings);
            _repair.Configure(settings, toolSettings, axe, pickaxe);
            _configured = true;
            _open = settings.Enabled != 0 && settings.InitiallyOpen != 0;
        }

        public void Show(DynamicBuffer<CombatPrototypeInventoryItem> inventory, int axeDurability, int pickaxeDurability,
            string feedback, CombatPrototypeMapInventoryDropFeedback dropFeedback, Entity source, Entity player)
        {
            if (_settings.Enabled == 0) return;
            _snapshot.Capture(inventory, axeDurability, pickaxeDurability, source, player);
            _canCraftAxe = CanCraft(_axe, axeDurability);
            _canCraftPickaxe = CanCraft(_pickaxe, pickaxeDurability);
            _repair.Capture(_snapshot.WoodQuantity, _snapshot.StoneQuantity, _snapshot.InventoryValid, axeDurability, pickaxeDurability);
            if (_woodQuantity != _snapshot.WoodQuantity || _stoneQuantity != _snapshot.StoneQuantity ||
                _axeDurability != axeDurability || _pickaxeDurability != pickaxeDurability ||
                _lastInventoryValid != _snapshot.InventoryValid)
            {
                _woodQuantity = _snapshot.WoodQuantity;
                _stoneQuantity = _snapshot.StoneQuantity;
                _axeDurability = axeDurability;
                _pickaxeDurability = pickaxeDurability;
                _lastInventoryValid = _snapshot.InventoryValid;
                _axeStatus = ToolStatus(_axe, _axeName, axeDurability);
                _pickaxeStatus = ToolStatus(_pickaxe, _pickaxeName, pickaxeDurability);
                _axeRecipe = Recipe(_axe);
                _pickaxeRecipe = Recipe(_pickaxe);
                _axeMissing = Missing(_axe);
                _pickaxeMissing = Missing(_pickaxe);
                _axeCraftTitle = _axeName + "  " + Availability(_axe, axeDurability);
                _pickaxeCraftTitle = _pickaxeName + "  " + Availability(_pickaxe, pickaxeDurability);
            }
            _drop.Observe(dropFeedback);
            _feedback = string.IsNullOrEmpty(_drop.Feedback) ? feedback : _drop.Feedback;
            _ready = true;
        }

        private bool CanCraft(CombatPrototypeMapGatherToolDefinition definition, int durability) =>
            _toolSettings.Enabled != 0 && _snapshot.InventoryValid && durability < definition.DurabilityCostPerCompletion &&
            _snapshot.WoodQuantity >= definition.CraftWoodQuantity && _snapshot.StoneQuantity >= definition.CraftStoneQuantity;

        private string ToolStatus(CombatPrototypeMapGatherToolDefinition definition, string name, int durability)
        {
            var text = name + "  " + (durability < 0 ? _notOwned : durability + "/" + definition.MaxDurability);
            if (_toolSettings.Enabled == 0) return text + "  " + _disabled;
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
            out CombatPrototypeMapInventoryDropRequest dropRequest)
        {
            craftAxe = craftPickaxe = repairAxe = repairPickaxe = false;
            dropRequest = default;
            if (!_configured || !_ready || _settings.Enabled == 0)
            {
                _craftAxe = _craftPickaxe = _mousePressAccepted = false;
                _drop.ClearPending();
                _repair.ClearPending();
                return false;
            }
            var wasInside = ContainsMouse(mouse);
            if (_lastInputFrame != Time.frameCount)
            {
                _lastInputFrame = Time.frameCount;
                if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
                {
                    if (_open) Close();
                    else _open = true;
                }
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    _mousePressAccepted = ContainsMouse(mouse);
            }
            var inside = ContainsMouse(mouse);
            if (_open)
            {
                craftAxe = _craftAxe;
                craftPickaxe = _craftPickaxe;
                dropRequest = _drop.ReadRequest();
                _repair.ReadRequest(out repairAxe, out repairPickaxe);
            }
            _craftAxe = _craftPickaxe = false;
            // Closing by B must also consume the mouse press that began over this panel.
            return wasInside || inside;
        }

        private bool ContainsMouse(Mouse mouse)
        {
            if (!_open || mouse == null || Screen.width <= 0 || Screen.height <= 0) return false;
            var scale = Scale();
            var point = mouse.position.ReadValue();
            return Rect(scale).Contains(new Vector2(point.x / scale, (Screen.height - point.y) / scale));
        }

        private static float Scale() => Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        private Rect Rect(float scale) => new Rect(Screen.width / scale - _settings.RightMarginPixels - _settings.PanelWidthPixels,
            _settings.TopMarginPixels, _settings.PanelWidthPixels, _settings.PanelHeightPixels);

        public void Draw()
        {
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
                var viewport = new Rect(panel.x + 12f, panel.y + 12f + _settings.RowHeightPixels,
                    panel.width - 24f, panel.height - 3f * _settings.RowHeightPixels - 24f);
                var rows = Mathf.Max(_snapshot.Items.Count * 2, 1) + 13 + CombatPrototypeMapGatherToolRepairPanel.RowCount;
                var content = new Rect(0f, 0f, viewport.width - 18f, rows * _settings.RowHeightPixels);
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
                if (mouseUp) _mousePressAccepted = false;
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
                GUI.enabled = oldEnabled;
            }
        }

        private void DrawBody(float width)
        {
            var y = 0f;
            Label(width, ref y, _materials);
            if (_snapshot.Items.Count == 0) Label(width, ref y, _empty);
            foreach (var item in _snapshot.Items)
            {
                Label(width, ref y, item.Text);
                _drop.DrawRow(width, y, _settings.RowHeightPixels, _labelStyle, _buttonStyle, item,
                    _snapshot.InventoryValid, _mousePressAccepted);
                y += _settings.RowHeightPixels;
            }
            Label(width, ref y, _tools);
            Label(width, ref y, _axeStatus);
            Label(width, ref y, _pickaxeStatus);
            Label(width, ref y, _craft);
            DrawRecipe(width, ref y, _axeCraftTitle, _axeButton, _axeRecipe, _axeMissing, _canCraftAxe, true);
            DrawRecipe(width, ref y, _pickaxeCraftTitle, _pickaxeButton, _pickaxeRecipe, _pickaxeMissing, _canCraftPickaxe, false);
            _repair.Draw(width, ref y, _settings.RowHeightPixels, _labelStyle, _buttonStyle, _mousePressAccepted);
        }

        private void Label(float width, ref float y, string text)
        {
            GUI.Label(new Rect(0f, y, width, _settings.RowHeightPixels), text, _labelStyle);
            y += _settings.RowHeightPixels;
        }

        private void DrawRecipe(float width, ref float y, string title, string button, string recipe, string missing,
            bool enabled, bool axe)
        {
            Label(width, ref y, title);
            Label(width, ref y, recipe);
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
            _open = _craftAxe = _craftPickaxe = _mousePressAccepted = false;
            _drop.ClearPending();
            _repair.ClearPending();
            _scroll = Vector2.zero;
        }

        public void Clear() { _ready = false; }

        public void Reset()
        {
            Close();
            _configured = _ready = false;
            _snapshot.Reset();
            _drop.Reset();
            _repair.Reset();
            _labelStyle = _buttonStyle = null;
            _feedback = string.Empty;
            _woodQuantity = _stoneQuantity = -1;
            _axeDurability = _pickaxeDurability = int.MinValue;
            _lastInputFrame = -1;
            _lastInventoryValid = false;
        }
    }
}
