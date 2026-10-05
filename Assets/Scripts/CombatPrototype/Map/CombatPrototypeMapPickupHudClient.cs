using System;
using System.Globalization;
using Unity.Collections;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapPickupHudClient
    {
        private readonly FixedString64Bytes _appleId = new FixedString64Bytes(CombatPrototypeMapYieldItemResolver.VitalityAppleId);
        private readonly FixedString64Bytes _woodId = new FixedString64Bytes(CombatPrototypeMapYieldItemResolver.WoodId);
        private readonly FixedString64Bytes _stoneId = new FixedString64Bytes(CombatPrototypeMapYieldItemResolver.StoneId);
        private readonly CombatPrototypeMapPickupLifetimeHudClient _lifetimeHud = new CombatPrototypeMapPickupLifetimeHudClient();
        private CombatPrototypeMapPickupHudSettings _settings;
        private CombatPrototypeMapPickupHudState _state;
        private GUIStyle _labelStyle;
        private string _pickupLabel;
        private string _appleLabel;
        private string _woodLabel;
        private string _stoneLabel;
        private string _noSpaceLabel;
        private string _text;
        private bool _visible;

        internal void Configure(CombatPrototypeMapPickupHudSettings settings)
        {
            Reset();
            _settings = settings;
            _pickupLabel = settings.PickupLabel.ToString();
            _appleLabel = settings.AppleLabel.ToString();
            _woodLabel = settings.WoodLabel.ToString();
            _stoneLabel = settings.StoneLabel.ToString();
            _noSpaceLabel = settings.NoSpaceLabel.ToString();
            _lifetimeHud.Configure(settings);
        }

        internal void Show(CombatPrototypeMapPickupHudState state)
        {
            Clear();
            if (_settings.Enabled == 0) return;
            try
            {
                if (state.Mode == CombatPrototypeMapPickupHudMode.Hidden)
                {
                    if (state.DropId != 0 || state.Quantity != 0 || state.ItemId.Length != 0) InvalidSnapshot(state);
                    _lifetimeHud.Show(state);
                    return;
                }
                if ((state.Mode != CombatPrototypeMapPickupHudMode.Ready && state.Mode != CombatPrototypeMapPickupHudMode.NoSpace) ||
                    state.DropId <= 0 || state.Quantity <= 0)
                    InvalidSnapshot(state);
                string label;
                if (state.ItemId.Equals(_appleId)) label = _appleLabel;
                else if (state.ItemId.Equals(_woodId)) label = _woodLabel;
                else if (state.ItemId.Equals(_stoneId)) label = _stoneLabel;
                else { InvalidSnapshot(state); return; }
                _lifetimeHud.Show(state);
                if (_state.Mode != state.Mode || !_state.ItemId.Equals(state.ItemId) || _state.Quantity != state.Quantity)
                    _text = "G  " + (state.Mode == CombatPrototypeMapPickupHudMode.NoSpace ? _noSpaceLabel : _pickupLabel) +
                        "  " + label + " ×" + state.Quantity.ToString(CultureInfo.InvariantCulture);
                _state = state;
                _visible = true;
            }
            catch (Exception exception)
            {
                Clear();
                Debug.LogError("[CombatPrototype.Map] Pickup HUD display failed; stage=ReadSnapshot, DropId=" +
                    state.DropId + ", itemId=" + state.ItemId + ". " + exception);
            }
        }

        private static void InvalidSnapshot(CombatPrototypeMapPickupHudState state)
        {
            throw new InvalidOperationException("[CombatPrototype.Map] Invalid owner pickup HUD snapshot; mode=" +
                state.Mode + ", DropId=" + state.DropId + ", itemId=" + state.ItemId + ", quantity=" + state.Quantity + ".");
        }

        internal void Clear()
        {
            _visible = false;
            _lifetimeHud.Clear();
        }

        internal void Reset()
        {
            Clear();
            _lifetimeHud.Reset();
            _settings = default;
            _state = CombatPrototypeMapPickupHudState.Hidden;
            _labelStyle = null;
            _pickupLabel = _appleLabel = _woodLabel = _stoneLabel = _noSpaceLabel = _text = string.Empty;
        }

        internal void Draw()
        {
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
                if (_lifetimeHud.Visible)
                {
                    var rowHeight = _settings.FontSize + 4f;
                    var top = panel.y + (panel.height - 2f * rowHeight - 8f) * 0.5f;
                    GUI.Label(new Rect(panel.x + 12f, top, panel.width - 24f, rowHeight), _text, _labelStyle);
                    _lifetimeHud.Draw(new Rect(panel.x + 12f, top + rowHeight + 8f, panel.width - 24f, rowHeight));
                }
                else
                    GUI.Label(new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, panel.height - 24f), _text, _labelStyle);
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
        }
    }
}
