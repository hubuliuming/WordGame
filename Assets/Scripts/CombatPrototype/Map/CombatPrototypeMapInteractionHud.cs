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
        private GUIStyle _labelStyle;
        private string _gatherLabel;
        private string _treeLabel;
        private string _mineLabel;
        private string _text;
        private string _toolText;
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
            CombatPrototypeMapInteractionHighlightSettings highlightSettings, string mapId)
        {
            Reset();
            _settings = settings;
            _toolSettings = toolSettings;
            _axe = axe;
            _pickaxe = pickaxe;
            _axeName = axe.DisplayName.ToString();
            _pickaxeName = pickaxe.DisplayName.ToString();
            _gatherLabel = settings.GatherLabel.ToString();
            _treeLabel = settings.TreeLabel.ToString();
            _mineLabel = settings.MineLabel.ToString();
            _inventoryPanel.Configure(inventorySettings, toolSettings, axe, pickaxe, dropSettings, dropDefinitions);
            _pickupHud.Configure(pickupSettings);
            _highlight.Configure(highlightSettings, mapId, _highlightCamera);
        }

        internal void Show(CombatPrototypeMapInteractionHudState state, DynamicBuffer<CombatPrototypeMapGatherTool> tools,
            CombatPrototypeMapToolCraftFeedback feedback, CombatPrototypeMapInventoryDropFeedback dropFeedback,
            DynamicBuffer<CombatPrototypeInventoryItem> inventory,
            Entity source, Entity player, CombatPrototypeMapPickupHudState pickupState)
        {
            _pickupHud.Show(pickupState);
            RefreshToolStatus(tools);
            ObserveFeedback(feedback);
            var hasFeedback = Time.unscaledTimeAsDouble < _feedbackUntil;
            _inventoryPanel.Show(inventory, _axeDurability, _pickaxeDurability,
                hasFeedback ? _feedbackText : string.Empty, dropFeedback, source, player);
            if (_settings.Enabled == 0) { _visible = false; return; }
            if (state.Mode == CombatPrototypeMapInteractionHudMode.Hidden)
            {
                _mode = state.Mode;
                _kind = 0;
                _percent = -1;
                _progress = 0f;
                _visible = hasFeedback;
                _text = hasFeedback ? _feedbackText : string.Empty;
                _toolText = hasFeedback ? ToolStatus(_feedbackKind) : string.Empty;
                return;
            }
            if ((state.Mode != CombatPrototypeMapInteractionHudMode.Ready && state.Mode != CombatPrototypeMapInteractionHudMode.Working) ||
                state.Kind < (byte)CombatPrototypeMapInteractionKind.Gather || state.Kind > (byte)CombatPrototypeMapInteractionKind.Mine ||
                state.PlacementIndex < 0 || state.ProgressPermille > 1000)
                throw new InvalidOperationException("[CombatPrototype.Map] Invalid owner HUD snapshot; mode=" + state.Mode +
                    ", type=" + state.Kind + ", placement=" + state.PlacementIndex + ", progress=" + state.ProgressPermille + ".");
            var percent = state.ProgressPermille / 10;
            if (_mode != state.Mode || _kind != state.Kind || _percent != percent)
            {
                var label = (CombatPrototypeMapInteractionKind)state.Kind == CombatPrototypeMapInteractionKind.Gather ?
                    _gatherLabel : (CombatPrototypeMapInteractionKind)state.Kind == CombatPrototypeMapInteractionKind.Tree ? _treeLabel : _mineLabel;
                _text = state.Mode == CombatPrototypeMapInteractionHudMode.Ready ? "F  " + label : label + "  " + percent + "%";
                _mode = state.Mode;
                _kind = state.Kind;
                _percent = percent;
            }
            _progress = state.ProgressPermille / 1000f;
            _toolText = hasFeedback ? _feedbackText :
                state.Kind == (byte)CombatPrototypeMapInteractionKind.Gather ? "Hands" :
                ToolStatus(state.Kind == (byte)CombatPrototypeMapInteractionKind.Tree ?
                    CombatPrototypeMapGatherToolKind.Axe : CombatPrototypeMapGatherToolKind.Pickaxe);
            _visible = true;
        }

        internal void ShowHighlight(CombatPrototypeMapInteractionHighlightFrame f, CombatPrototypeMapInteractionHighlightFrame g)
        {
            _highlight.Show(f, g);
        }

        private void RefreshToolStatus(DynamicBuffer<CombatPrototypeMapGatherTool> tools)
        {
            if (tools.Length > 2) throw new InvalidOperationException("Invalid owner tool snapshot: too many tools.");
            var axeDurability = -1;
            var pickaxeDurability = -1;
            foreach (var tool in tools)
            {
                if (tool.ToolId.Equals(_axe.ToolId))
                {
                    if (axeDurability >= 0 || tool.Durability < 0 || tool.Durability > _axe.MaxDurability)
                        throw new InvalidOperationException("Invalid owner axe snapshot.");
                    axeDurability = tool.Durability;
                }
                else if (tool.ToolId.Equals(_pickaxe.ToolId))
                {
                    if (pickaxeDurability >= 0 || tool.Durability < 0 || tool.Durability > _pickaxe.MaxDurability)
                        throw new InvalidOperationException("Invalid owner pickaxe snapshot.");
                    pickaxeDurability = tool.Durability;
                }
                else throw new InvalidOperationException("Unknown owner tool ID: " + tool.ToolId);
            }
            if (_axeDurability != axeDurability)
            {
                _axeDurability = axeDurability;
                _axeStatus = FormatTool(_axe, _axeName, axeDurability, "1");
            }
            if (_pickaxeDurability != pickaxeDurability)
            {
                _pickaxeDurability = pickaxeDurability;
                _pickaxeStatus = FormatTool(_pickaxe, _pickaxeName, pickaxeDurability, "2");
            }
        }

        private string FormatTool(CombatPrototypeMapGatherToolDefinition definition, string name, int durability, string key)
        {
            if (_toolSettings.Enabled == 0) return "Hands";
            if (durability < 0) return "Hands  [" + key + ": Craft " + name + "]";
            var status = name + "  " + durability + "/" + definition.MaxDurability;
            return durability < definition.DurabilityCostPerCompletion ? status + "  [" + key + ": Craft]" : status;
        }

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

        internal bool ReadPanelInput(Keyboard keyboard, Mouse mouse, out bool craftAxe, out bool craftPickaxe,
            out CombatPrototypeMapInventoryDropRequest dropRequest) =>
            _inventoryPanel.ReadInput(keyboard, mouse, out craftAxe, out craftPickaxe, out dropRequest);

        internal void Clear()
        {
            _visible = false;
            _inventoryPanel.Clear();
            _pickupHud.Clear();
            _highlight.Clear();
        }

        internal void Reset()
        {
            Clear();
            _inventoryPanel.Reset();
            _pickupHud.Reset();
            _highlight.Reset();
            _labelStyle = null;
            _text = _toolText = _feedbackText = string.Empty;
            _mode = CombatPrototypeMapInteractionHudMode.Hidden;
            _kind = 0;
            _percent = -1;
            _progress = 0f;
            _axeDurability = _pickaxeDurability = int.MinValue;
            _axeStatus = _pickaxeStatus = string.Empty;
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
                GUI.color = Color.white;
                GUI.Label(new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, _settings.FontSize + 8f), _text, _labelStyle);
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
