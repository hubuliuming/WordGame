using System;
using System.Globalization;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapResourceStatusHudClient
    {
        private CombatPrototypeMapResourceStatusHudSettings _settings;
        private CombatPrototypeMapResourceStatusHudState _state;
        private readonly string[] _names = new string[4];
        private readonly string[] _labels = new string[7];
        private GUIStyle _labelStyle;
        private string _mapId;
        private string _text;
        private bool _visible;

        internal void Configure(CombatPrototypeMapResourceStatusHudSettings settings,
            CombatPrototypeMapInteractionHudSettings interaction, string mapId)
        {
            Reset();
            _settings = settings;
            _mapId = mapId;
            _names[1] = interaction.GatherLabel.ToString();
            _names[2] = interaction.TreeLabel.ToString();
            _names[3] = interaction.MineLabel.ToString();
            _labels[1] = settings.AvailableLabel.ToString();
            _labels[2] = settings.WorkingLabel.ToString();
            _labels[3] = settings.OccupiedLabel.ToString();
            _labels[4] = settings.RegrowingLabel.ToString();
            _labels[5] = settings.WaitingLabel.ToString();
            _labels[6] = settings.DepletedLabel.ToString();
        }

        internal void Show(CombatPrototypeMapResourceStatusHudState state)
        {
            _visible = false;
            if (_settings.Enabled == 0) return;
            try
            {
                if (state.Mode == CombatPrototypeMapResourceStatusHudMode.Hidden)
                {
                    if (state.Kind != 0 || state.PlacementIndex != -1 || state.RemainingSeconds != 0f) InvalidSnapshot(state);
                    return;
                }
                if (state.Mode < CombatPrototypeMapResourceStatusHudMode.Available ||
                    state.Mode > CombatPrototypeMapResourceStatusHudMode.Depleted || state.Kind < 1 || state.Kind > 3 ||
                    state.PlacementIndex < 0 || float.IsNaN(state.RemainingSeconds) || float.IsInfinity(state.RemainingSeconds) ||
                    (state.Mode == CombatPrototypeMapResourceStatusHudMode.Regrowing ?
                        state.RemainingSeconds <= 0f || state.RemainingSeconds != Mathf.Ceil(state.RemainingSeconds) :
                        state.RemainingSeconds != 0f)) InvalidSnapshot(state);
                if (_state.Mode != state.Mode || _state.Kind != state.Kind || _state.RemainingSeconds != state.RemainingSeconds)
                {
                    _text = _names[state.Kind] + " | " + _labels[(byte)state.Mode];
                    if (state.Mode == CombatPrototypeMapResourceStatusHudMode.Regrowing)
                        _text += " " + state.RemainingSeconds.ToString("0", CultureInfo.InvariantCulture) + "s";
                }
                _state = state;
                _visible = true;
            }
            catch (Exception exception) { Report("ReadSnapshot", state, exception); }
        }

        private static void InvalidSnapshot(CombatPrototypeMapResourceStatusHudState state)
        {
            throw new InvalidOperationException("Invalid owner resource status snapshot; mode=" + state.Mode +
                ", type=" + state.Kind + ", placement=" + state.PlacementIndex + ", remaining=" + state.RemainingSeconds + ".");
        }

        private void Report(string stage, CombatPrototypeMapResourceStatusHudState state, Exception exception)
        {
            _visible = false;
            Debug.LogError("[CombatPrototype.Map] Resource status display failed; stage=" + stage + ", map=" + _mapId +
                ", type=" + state.Kind + ", placement=" + state.PlacementIndex + ". " + exception);
        }

        internal void Clear() => _visible = false;

        internal void Reset()
        {
            Clear();
            _settings = default;
            _state = CombatPrototypeMapResourceStatusHudState.Hidden;
            Array.Clear(_names, 0, _names.Length);
            Array.Clear(_labels, 0, _labels.Length);
            _labelStyle = null;
            _mapId = _text = string.Empty;
        }

        internal void Draw()
        {
            if (!_visible || Event.current.type != EventType.Repaint || Screen.width <= 0 || Screen.height <= 0) return;
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            try
            {
                if (_labelStyle == null)
                {
                    _labelStyle = new GUIStyle(GUI.skin.label)
                        { alignment = TextAnchor.MiddleCenter, fontSize = _settings.FontSize, richText = false };
                    _labelStyle.normal.textColor = Color.white;
                }
                var scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
                var panel = new Rect(Screen.width / scale * 0.5f - _settings.PanelWidthPixels * 0.5f,
                    Screen.height / scale - _settings.BottomMarginPixels - _settings.PanelHeightPixels,
                    _settings.PanelWidthPixels, _settings.PanelHeightPixels);
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, panel.height - 24f), _text, _labelStyle);
            }
            catch (Exception exception) { Report("Draw", _state, exception); }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
        }
    }
}
