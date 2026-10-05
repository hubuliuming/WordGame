using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;
using Code_01.CombatPrototype.Networking;

namespace Code_01.CombatPrototype.Map
{
    [DisallowMultipleComponent]
    public sealed class CombatPrototypeMapInteractionHud : MonoBehaviour
    {
        private Unity.Entities.World _clientWorld;
        private CombatPrototypeMapInteractionHudBindingSystem _binding;
        private CombatPrototypeMapInteractionHudSettings _settings;
        private CombatPrototypeMapGatherToolSettings _toolSettings;
        private CombatPrototypeMapGatherToolDefinition _axe;
        private CombatPrototypeMapGatherToolDefinition _pickaxe;
        private CombatPrototypeMapGatherToolUpgradeDefinition _axeSecond, _axeThird, _pickaxeSecond, _pickaxeThird;
        private string _levelLabel, _recraftLabel;
        private int _axeLevel = -1, _pickaxeLevel = -1;
        private GUIStyle _labelStyle;
        private string _gatherLabel;
        private string _treeLabel;
        private string _mineLabel;
        private string _noSpaceLabel;
        private string _text;
        private string _toolText;
        private Color _textColor = Color.white;
        private Color _toolTextColor = Color.white;
        private readonly CombatPrototypeMapInteractionFailureHudClient _interactionFailure = new CombatPrototypeMapInteractionFailureHudClient();
        private readonly CombatPrototypeMapGatherOutcomeHudClient _gatherOutcome = new CombatPrototypeMapGatherOutcomeHudClient();
        private readonly CombatPrototypeMapGatherToolDurabilityHudClient _durabilityHud = new CombatPrototypeMapGatherToolDurabilityHudClient();
        private string _feedbackText;
        private string _axeName;
        private string _pickaxeName;
        private string _axeStatus;
        private string _pickaxeStatus;
        private int _axeDurability = int.MinValue;
        private int _pickaxeDurability = int.MinValue;
        private uint _feedbackSequence;
        private bool _observedFeedback;
        private CombatPrototypeMapGatherToolKind _feedbackKind;
        private double _feedbackUntil;
        private CombatPrototypeMapInteractionHudMode _mode;
        private byte _kind;
        private int _percent = -1;
        private float _progress;
        private bool _visible;
        private readonly CombatPrototypeMapInventoryPanel _inventoryPanel = new CombatPrototypeMapInventoryPanel();
        private readonly CombatPrototypeMapPickupHudClient _pickupHud = new CombatPrototypeMapPickupHudClient();
        private readonly CombatPrototypeMapInteractionHighlightClient _highlight = new CombatPrototypeMapInteractionHighlightClient();
        private readonly CombatPrototypeMapResourceStatusHudClient _resourceStatus = new CombatPrototypeMapResourceStatusHudClient();
        private readonly CombatPrototypeMapGatherToolRepairFeedbackClient _repairFeedback = new CombatPrototypeMapGatherToolRepairFeedbackClient();
        private readonly CombatPrototypeMapWorldSaveHudClient _worldSaveHud = new CombatPrototypeMapWorldSaveHudClient();
        private Camera _highlightCamera;

        private void Awake()
        {
            _highlightCamera = GetComponent<Camera>();
            if (_highlightCamera == null)
                throw new InvalidOperationException("[CombatPrototype.Map] The Main Camera HUD requires its existing Camera component.");
        }

        private void OnEnable()
        {
            BindCurrentWorld();
        }

        private void Update()
        {
            // Scene and network-world lifetimes can end or restart independently.
            if (_clientWorld != ClientServerBootstrap.ClientWorld ||
                (_clientWorld != null && !_clientWorld.IsCreated)) BindCurrentWorld();
        }

        private void BindCurrentWorld()
        {
            Unbind();
            var world = ClientServerBootstrap.ClientWorld;
            // Server-only startup and a disposed client world have no local display.
            if (world == null || !world.IsCreated) return;
            var binding = world.GetExistingSystemManaged<CombatPrototypeMapInteractionHudBindingSystem>();
            if (binding == null)
                throw new InvalidOperationException("[CombatPrototype.Map] Client World=" + world.Name +
                    " requires CombatPrototypeMapInteractionHudBindingSystem.");
            binding.RegisterHud(this);
            _clientWorld = world;
            _binding = binding;
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Unbind()
        {
            Reset();
            if (_binding != null && _clientWorld.IsCreated) _binding.UnregisterHud(this);
            _binding = null;
            _clientWorld = null;
        }

        internal void Configure(CombatPrototypeMapInteractionHudSettings settings,
            CombatPrototypeMapInventoryPanelSettings inventorySettings, CombatPrototypeMapGatherToolSettings toolSettings, CombatPrototypeMapGatherToolDefinition axe,
            CombatPrototypeMapGatherToolDefinition pickaxe, CombatPrototypeMapInventoryDropSettings dropSettings,
            DynamicBuffer<CombatPrototypeMapInventoryDropDefinition> dropDefinitions, CombatPrototypeMapPickupHudSettings pickupSettings,
            CombatPrototypeMapInteractionHighlightSettings highlightSettings,
            CombatPrototypeMapResourceStatusHudSettings resourceStatusSettings, CombatPrototypeMapWorldSaveHudSettings worldSaveSettings,
            CombatPrototypeMapResourcePersistenceSettings persistenceSettings,
            CombatPrototypeMapInventoryCapacitySettings capacity, DynamicBuffer<CombatPrototypeMapInventoryCapacityDefinition> capacityDefinitions,
            CombatPrototypeMapInventoryCapacityUpgradeSettings upgradeSettings,
            DynamicBuffer<CombatPrototypeMapInventoryCapacityUpgradeDefinition> upgradeDefinitions,
            CombatPrototypeMapGatherToolUpgradeSettings toolUpgradeSettings,
            DynamicBuffer<CombatPrototypeMapGatherToolUpgradeDefinition> toolUpgradeDefinitions,
            CombatPrototypeMapGatherToolDurabilityHudSettings durabilitySettings,
            CombatPrototypeMapInteractionFailureHudSettings failureSettings, CombatPrototypeMapGatherOutcomeHudSettings outcomeSettings,
            CombatPrototypeMapPickupFeedbackHudSettings pickupFeedbackSettings,
            float treeDuration, float mineDuration, string mapId)
        {
            Reset();
            _settings = settings;
            _toolSettings = toolSettings;
            _axe = axe;
            _pickaxe = pickaxe;
            _axeSecond = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(toolUpgradeDefinitions, axe.ToolId, 2);
            _axeThird = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(toolUpgradeDefinitions, axe.ToolId, 3);
            _pickaxeSecond = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(toolUpgradeDefinitions, pickaxe.ToolId, 2);
            _pickaxeThird = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(toolUpgradeDefinitions, pickaxe.ToolId, 3);
            _levelLabel = toolUpgradeSettings.LevelLabel.ToString(); _recraftLabel = toolUpgradeSettings.RecraftLabel.ToString();
            _axeName = axe.DisplayName.ToString();
            _pickaxeName = pickaxe.DisplayName.ToString();
            _gatherLabel = settings.GatherLabel.ToString();
            _treeLabel = settings.TreeLabel.ToString();
            _mineLabel = settings.MineLabel.ToString();
            _noSpaceLabel = settings.NoSpaceLabel.ToString();
            _interactionFailure.Configure(failureSettings, _noSpaceLabel, mapId);
            _gatherOutcome.Configure(outcomeSettings, _noSpaceLabel, mapId);
            _durabilityHud.Configure(durabilitySettings, toolSettings, inventorySettings.NotOwnedLabel.ToString(), _recraftLabel);
            _inventoryPanel.Configure(inventorySettings, toolSettings, axe, pickaxe, dropSettings, dropDefinitions, capacity, capacityDefinitions, upgradeSettings, upgradeDefinitions, toolUpgradeSettings, toolUpgradeDefinitions, _durabilityHud.Enabled, treeDuration, mineDuration, mapId);
            _pickupHud.Configure(pickupSettings, pickupFeedbackSettings, mapId);
            _highlight.Configure(highlightSettings, mapId, _highlightCamera);
            _resourceStatus.Configure(resourceStatusSettings, settings, mapId);
            _repairFeedback.Configure(toolSettings, inventorySettings, axe, pickaxe, mapId);
            _worldSaveHud.Configure(worldSaveSettings, persistenceSettings, mapId);
        }

        internal void Show(CombatPrototypeMapInteractionHudState state, DynamicBuffer<CombatPrototypeMapGatherTool> tools,
            CombatPrototypeMapToolCraftFeedback feedback, CombatPrototypeMapToolRepairFeedback repairFeedback,
            CombatPrototypeMapInventoryDropFeedback dropFeedback, int capacityLevel,
            CombatPrototypeMapInventoryCapacityUpgradeFeedback upgradeFeedback, CombatPrototypeMapToolUpgradeFeedback toolUpgradeFeedback,
            DynamicBuffer<CombatPrototypeInventoryItem> inventory,
            Entity source, Entity player, CombatPrototypeMapPickupHudState pickupState,
            CombatPrototypeMapInteractionFailureFeedback failureFeedback, CombatPrototypeMapGatherOutcomeFeedback outcomeFeedback,
            CombatPrototypeMapPickupFeedback pickupFeedback)
        {
            _textColor = Color.white;
            _toolTextColor = Color.white;
            _interactionFailure.Observe(failureFeedback);
            _gatherOutcome.Observe(outcomeFeedback);
            _pickupHud.Show(pickupState, pickupFeedback);
            RefreshToolStatus(tools);
            ObserveFeedback(feedback);
            _repairFeedback.Observe(repairFeedback);
            var repairText = _repairFeedback.Feedback;
            var hasRepairFeedback = !string.IsNullOrEmpty(repairText);
            var hasCraftFeedback = Time.unscaledTimeAsDouble < _feedbackUntil;
            var hasFeedback = hasRepairFeedback || hasCraftFeedback;
            var displayFeedback = hasRepairFeedback ? repairText : hasCraftFeedback ? _feedbackText : string.Empty;
            var feedbackKind = hasRepairFeedback ? _repairFeedback.Kind : _feedbackKind;
            _inventoryPanel.Show(inventory, _axeDurability, _pickaxeDurability, _axeLevel, _pickaxeLevel,
                DefinitionForLevel(_axe, _axeLevel), DefinitionForLevel(_pickaxe, _pickaxeLevel),
                _durabilityHud.Axe, _durabilityHud.Pickaxe,
                displayFeedback, dropFeedback, capacityLevel, upgradeFeedback, toolUpgradeFeedback, source, player);
            if (_settings.Enabled == 0) { _visible = false; return; }
            var failureText = _interactionFailure.Feedback;
            var hasFailure = !string.IsNullOrEmpty(failureText);
            var outcomeText = _gatherOutcome.Feedback;
            var hasOutcome = !string.IsNullOrEmpty(outcomeText);
            var showOutcome = hasOutcome && (!hasFailure || _gatherOutcome.Result == CombatPrototypeMapGatherOutcomeResult.NoSpace);
            var showFailure = hasFailure && !showOutcome;
            if (state.Mode == CombatPrototypeMapInteractionHudMode.Hidden)
            {
                _mode = state.Mode;
                _kind = 0;
                _percent = -1;
                _progress = 0f;
                _visible = showFailure || showOutcome || hasFeedback;
                _text = showFailure ? failureText : showOutcome ? outcomeText : displayFeedback;
                _textColor = showFailure ? _interactionFailure.TextColor : showOutcome ? _gatherOutcome.TextColor : Color.white;
                _toolText = showFailure ? string.Empty : showOutcome ? OutcomeToolStatus(_gatherOutcome.Kind) :
                    hasFeedback ? ToolStatus(feedbackKind) : string.Empty;
                _toolTextColor = showFailure ? Color.white : showOutcome ? OutcomeToolColor(_gatherOutcome.Kind) :
                    hasFeedback ? _durabilityHud.ForKind(feedbackKind).TextColor : Color.white;
                return;
            }
            if ((state.Mode != CombatPrototypeMapInteractionHudMode.Ready && state.Mode != CombatPrototypeMapInteractionHudMode.Working &&
                 state.Mode != CombatPrototypeMapInteractionHudMode.NoSpace) ||
                state.Kind < (byte)CombatPrototypeMapInteractionKind.Gather || state.Kind > (byte)CombatPrototypeMapInteractionKind.Mine ||
                (state.Mode == CombatPrototypeMapInteractionHudMode.NoSpace && (state.Kind != (byte)CombatPrototypeMapInteractionKind.Gather || state.ProgressPermille != 0)) ||
                state.PlacementIndex < 0 || state.ProgressPermille > 1000)
                throw new InvalidOperationException("[CombatPrototype.Map] Invalid owner HUD snapshot; mode=" + state.Mode +
                    ", type=" + state.Kind + ", placement=" + state.PlacementIndex + ", progress=" + state.ProgressPermille + ".");
            var percent = state.ProgressPermille / 10;
            if (_mode != state.Mode || _kind != state.Kind || _percent != percent)
            {
                var label = (CombatPrototypeMapInteractionKind)state.Kind == CombatPrototypeMapInteractionKind.Gather ?
                    _gatherLabel : (CombatPrototypeMapInteractionKind)state.Kind == CombatPrototypeMapInteractionKind.Tree ? _treeLabel : _mineLabel;
                _text = state.Mode == CombatPrototypeMapInteractionHudMode.NoSpace ? _noSpaceLabel :
                    state.Mode == CombatPrototypeMapInteractionHudMode.Ready ? "F  " + label : label + "  " + percent + "%";
                _mode = state.Mode;
                _kind = state.Kind;
                _percent = percent;
            }
            _progress = state.ProgressPermille / 1000f;
            _toolText = state.Mode == CombatPrototypeMapInteractionHudMode.NoSpace ? "F  " + _gatherLabel :
                showFailure ? failureText : showOutcome ? outcomeText : hasFeedback ? displayFeedback :
                state.Kind == (byte)CombatPrototypeMapInteractionKind.Gather ? "Hands" :
                ToolStatus(state.Kind == (byte)CombatPrototypeMapInteractionKind.Tree ?
                    CombatPrototypeMapGatherToolKind.Axe : CombatPrototypeMapGatherToolKind.Pickaxe);
            if (state.Mode != CombatPrototypeMapInteractionHudMode.NoSpace && showFailure)
                _toolTextColor = _interactionFailure.TextColor;
            else if (state.Mode != CombatPrototypeMapInteractionHudMode.NoSpace && showOutcome)
                _toolTextColor = _gatherOutcome.TextColor;
            else if (!hasFeedback && state.Kind != (byte)CombatPrototypeMapInteractionKind.Gather)
                _toolTextColor = _durabilityHud.ForKind(state.Kind == (byte)CombatPrototypeMapInteractionKind.Tree ?
                    CombatPrototypeMapGatherToolKind.Axe : CombatPrototypeMapGatherToolKind.Pickaxe).TextColor;
            _visible = true;
        }

        internal void ShowHighlight(CombatPrototypeMapInteractionHighlightFrame f, CombatPrototypeMapInteractionHighlightFrame g)
        {
            _highlight.Show(f, g);
        }

        internal void ShowResourceStatus(CombatPrototypeMapResourceStatusHudState state) => _resourceStatus.Show(state);
        internal void ShowWorldSave(CombatPrototypeMapWorldSaveHudState state) => _worldSaveHud.Show(state);

        private void RefreshToolStatus(DynamicBuffer<CombatPrototypeMapGatherTool> tools)
        {
            if (tools.Length > 2) throw new InvalidOperationException("Invalid owner tool snapshot: too many tools.");
            var axeDurability = -1;
            var pickaxeDurability = -1;
            var axeLevel = 0; var pickaxeLevel = 0;
            foreach (var tool in tools)
            {
                CombatPrototypeMapGatherToolUtility.ValidateLevel(tool.Level);
                if (tool.ToolId.Equals(_axe.ToolId))
                {
                    if (axeDurability >= 0 || tool.Durability < 0 || tool.Durability > DefinitionForLevel(_axe, tool.Level).MaxDurability)
                        throw new InvalidOperationException("Invalid owner axe snapshot.");
                    axeDurability = tool.Durability; axeLevel = tool.Level;
                }
                else if (tool.ToolId.Equals(_pickaxe.ToolId))
                {
                    if (pickaxeDurability >= 0 || tool.Durability < 0 || tool.Durability > DefinitionForLevel(_pickaxe, tool.Level).MaxDurability)
                        throw new InvalidOperationException("Invalid owner pickaxe snapshot.");
                    pickaxeDurability = tool.Durability; pickaxeLevel = tool.Level;
                }
                else throw new InvalidOperationException("Unknown owner tool ID: " + tool.ToolId);
            }
            _durabilityHud.Capture(DefinitionForLevel(_axe, axeLevel), axeDurability, axeLevel,
                DefinitionForLevel(_pickaxe, pickaxeLevel), pickaxeDurability, pickaxeLevel);
            if (_axeDurability != axeDurability || _axeLevel != axeLevel)
            {
                _axeDurability = axeDurability; _axeLevel = axeLevel;
                _axeStatus = FormatTool(DefinitionForLevel(_axe, axeLevel), _axeName, axeDurability, axeLevel, "1");
            }
            if (_pickaxeDurability != pickaxeDurability || _pickaxeLevel != pickaxeLevel)
            {
                _pickaxeDurability = pickaxeDurability; _pickaxeLevel = pickaxeLevel;
                _pickaxeStatus = FormatTool(DefinitionForLevel(_pickaxe, pickaxeLevel), _pickaxeName, pickaxeDurability, pickaxeLevel, "2");
            }
        }

        private CombatPrototypeMapGatherToolDefinition DefinitionForLevel(CombatPrototypeMapGatherToolDefinition definition, int level)
        {
            // Level zero represents an absent tool in this display projection.
            if (level <= 1) return definition;
            var upgrade = definition.Kind == CombatPrototypeMapGatherToolKind.Axe ?
                (level == 2 ? _axeSecond : _axeThird) : (level == 2 ? _pickaxeSecond : _pickaxeThird);
            return CombatPrototypeMapGatherToolUtility.ApplyUpgrade(definition, upgrade);
        }

        private string FormatTool(CombatPrototypeMapGatherToolDefinition definition, string name, int durability, int level, string key)
        {
            if (_toolSettings.Enabled == 0) return "Hands";
            if (durability < 0) return "Hands  [" + key + ": Craft " + name + "]";
            var status = name + " " + _levelLabel + " " + level + "  " + durability + "/" + definition.MaxDurability;
            if (durability < definition.DurabilityCostPerCompletion && _durabilityHud.Enabled && _toolSettings.RepairEnabled != 0)
                return status + "  [" + _durabilityHud.ForKind(definition.Kind).RepairHint + "]";
            return durability < definition.DurabilityCostPerCompletion ? status + "  [" + key + ": " + _recraftLabel + "]" : status;
        }

        private string OutcomeToolStatus(byte kind) => kind == (byte)CombatPrototypeMapInteractionKind.Gather ? "Hands" :
            ToolStatus(kind == (byte)CombatPrototypeMapInteractionKind.Tree ? CombatPrototypeMapGatherToolKind.Axe : CombatPrototypeMapGatherToolKind.Pickaxe);

        private Color OutcomeToolColor(byte kind) => kind == (byte)CombatPrototypeMapInteractionKind.Gather ? Color.white :
            _durabilityHud.ForKind(kind == (byte)CombatPrototypeMapInteractionKind.Tree ? CombatPrototypeMapGatherToolKind.Axe : CombatPrototypeMapGatherToolKind.Pickaxe).TextColor;

        private string ToolStatus(CombatPrototypeMapGatherToolKind kind)
        {
            return kind == CombatPrototypeMapGatherToolKind.Axe ? _axeStatus : _pickaxeStatus;
        }

        private void ObserveFeedback(CombatPrototypeMapToolCraftFeedback feedback)
        {
            if (feedback.Result > CombatPrototypeMapToolCraftResult.Failed ||
                (feedback.Result == CombatPrototypeMapToolCraftResult.None ? feedback.Kind != CombatPrototypeMapGatherToolKind.None :
                    feedback.Kind != CombatPrototypeMapGatherToolKind.Axe && feedback.Kind != CombatPrototypeMapGatherToolKind.Pickaxe))
                throw new InvalidOperationException("Invalid owner craft feedback snapshot.");
            if (!_observedFeedback)
            {
                // Observe the initial sequence without replaying a result from a prior binding or connection.
                _feedbackSequence = feedback.Sequence;
                _observedFeedback = true;
                return;
            }
            if (_feedbackSequence == feedback.Sequence) return;
            _feedbackSequence = feedback.Sequence;
            if (feedback.Result == CombatPrototypeMapToolCraftResult.None) { _feedbackUntil = 0d; return; }
            _feedbackKind = feedback.Kind;
            var name = feedback.Kind == CombatPrototypeMapGatherToolKind.Axe ? _axeName : _pickaxeName;
            switch (feedback.Result)
            {
                case CombatPrototypeMapToolCraftResult.Success: _feedbackText = "Crafted " + name; break;
                case CombatPrototypeMapToolCraftResult.Disabled: _feedbackText = "Tool crafting disabled"; break;
                case CombatPrototypeMapToolCraftResult.AlreadyUsable: _feedbackText = name + " is still usable"; break;
                case CombatPrototypeMapToolCraftResult.InsufficientMaterials: _feedbackText = "Need wood and stone"; break;
                case CombatPrototypeMapToolCraftResult.Busy: _feedbackText = "Finish gathering first"; break;
                case CombatPrototypeMapToolCraftResult.FHasPriority: _feedbackText = "F interaction has priority"; break;
                case CombatPrototypeMapToolCraftResult.PlayerUnavailable: _feedbackText = "Stand still to craft"; break;
                case CombatPrototypeMapToolCraftResult.Failed: _feedbackText = "Craft failed"; break;
                default: throw new InvalidOperationException("Unsupported craft feedback: " + feedback.Result);
            }
            // This timer controls only presentation; authoritative work timing remains in the server HUD state.
            _feedbackUntil = Time.unscaledTimeAsDouble + _toolSettings.CraftFeedbackSeconds;
        }

        internal bool ReadPanelInput(Keyboard keyboard, Mouse mouse, out bool craftAxe, out bool craftPickaxe, out bool repairAxe, out bool repairPickaxe,
            out CombatPrototypeMapInventoryDropRequest dropRequest, out bool upgrade, out bool upgradeAxe, out bool upgradePickaxe) =>
            _inventoryPanel.ReadInput(keyboard, mouse, out craftAxe, out craftPickaxe, out repairAxe, out repairPickaxe, out dropRequest, out upgrade, out upgradeAxe, out upgradePickaxe);

        internal void Clear()
        {
            _visible = false;
            _inventoryPanel.Clear();
            _pickupHud.Clear();
            _highlight.Clear();
            _resourceStatus.Clear();
            _worldSaveHud.Clear();
        }

        internal void Reset()
        {
            Clear();
            _inventoryPanel.Reset();
            _pickupHud.Reset();
            _highlight.Reset();
            _resourceStatus.Reset();
            _repairFeedback.Reset();
            _worldSaveHud.Reset();
            _durabilityHud.Reset();
            _interactionFailure.Reset();
            _gatherOutcome.Reset();
            _textColor = Color.white;
            _toolTextColor = Color.white;
            _labelStyle = null;
            _text = _toolText = _feedbackText = string.Empty;
            _mode = CombatPrototypeMapInteractionHudMode.Hidden;
            _kind = 0;
            _percent = -1;
            _progress = 0f;
            _axeDurability = _pickaxeDurability = int.MinValue;
            _axeStatus = _pickaxeStatus = _levelLabel = _recraftLabel = string.Empty;
            _axeLevel = _pickaxeLevel = -1;
            _axeSecond = _axeThird = _pickaxeSecond = _pickaxeThird = default;
            _observedFeedback = false;
            _feedbackSequence = 0;
            _feedbackKind = CombatPrototypeMapGatherToolKind.None;
            _feedbackUntil = 0d;
        }

        private void OnGUI()
        {
            _highlight.Draw();
            _inventoryPanel.Draw();
            _pickupHud.Draw();
            _resourceStatus.Draw();
            _worldSaveHud.Draw();
            if (!_visible || Event.current.type != EventType.Repaint || Screen.width <= 0 || Screen.height <= 0) return;
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter, fontSize = _settings.FontSize, richText = false
                };
                _labelStyle.normal.textColor = Color.white;
            }
            var scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            var panel = new Rect(Screen.width / scale * 0.5f - _settings.PanelWidthPixels * 0.5f,
                Screen.height / scale - _settings.BottomMarginPixels - _settings.PanelHeightPixels,
                _settings.PanelWidthPixels, _settings.PanelHeightPixels);
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = _textColor;
                GUI.Label(new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, _settings.FontSize + 8f), _text, _labelStyle);
                GUI.color = _toolTextColor;
                GUI.Label(new Rect(panel.x + 12f, panel.y + 20f + _settings.FontSize, panel.width - 24f, _settings.FontSize + 8f),
                    _toolText, _labelStyle);
                if (_mode != CombatPrototypeMapInteractionHudMode.Working) return;
                var bar = new Rect(panel.x + 16f, panel.yMax - 16f - _settings.ProgressBarHeightPixels,
                    panel.width - 32f, _settings.ProgressBarHeightPixels);
                GUI.color = new Color(1f, 1f, 1f, 0.2f);
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
                GUI.color = new Color(0.85f, 0.65f, 0.3f, 1f);
                bar.width *= _progress;
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
        }
    }
}
