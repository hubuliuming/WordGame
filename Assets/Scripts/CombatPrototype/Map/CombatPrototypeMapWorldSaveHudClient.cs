using System;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapWorldSaveHudClient
    {
        private CombatPrototypeMapWorldSaveHudSettings _settings;
        private string _mapId;
        private string _disabled, _notSaved, _saved, _captureFailed, _saveFailed;
        private string _manualSave, _manualDisabled, _cooldown, _unavailable;
        private string _text, _feedback;
        private GUIStyle _labelStyle;
        private Color _errorColor;
        private CombatPrototypeMapWorldSaveHudMode _mode;
        private bool _manualEnabled, _visible, _observedFeedback, _feedbackError;
        private uint _sequence;
        private double _feedbackUntil;

        internal void Configure(CombatPrototypeMapWorldSaveHudSettings settings,
            CombatPrototypeMapResourcePersistenceSettings persistence, string mapId)
        {
            Reset();
            _settings = settings;
            _mapId = mapId;
            _manualEnabled = persistence.Enabled != 0 && persistence.ManualSaveEnabled != 0;
            _disabled = settings.DisabledLabel.ToString();
            _notSaved = settings.NotSavedLabel.ToString();
            _saved = settings.SavedLabel.ToString();
            _captureFailed = settings.CaptureFailedLabel.ToString();
            _saveFailed = settings.SaveFailedLabel.ToString();
            _manualSave = settings.ManualSaveLabel.ToString();
            _manualDisabled = settings.ManualDisabledLabel.ToString();
            _cooldown = settings.CooldownLabel.ToString();
            _unavailable = settings.UnavailableLabel.ToString();
            _errorColor = new Color(settings.ErrorColor.x, settings.ErrorColor.y, settings.ErrorColor.z, 1f);
        }

        internal void Show(CombatPrototypeMapWorldSaveHudState state)
        {
            Clear();
            if (_settings.Enabled == 0) return;
            try
            {
                if (state.Mode == CombatPrototypeMapWorldSaveHudMode.Hidden)
                {
                    if (state.ManualSequence != 0 || state.ManualResult != CombatPrototypeMapWorldSaveManualResult.None)
                        InvalidSnapshot(state);
                    return;
                }
                if (state.Mode > CombatPrototypeMapWorldSaveHudMode.SaveFailed ||
                    state.ManualResult > CombatPrototypeMapWorldSaveManualResult.Failed ||
                    (state.ManualResult == CombatPrototypeMapWorldSaveManualResult.None && state.ManualSequence != 0))
                    InvalidSnapshot(state);
                if (_mode != state.Mode)
                {
                    switch (state.Mode)
                    {
                        case CombatPrototypeMapWorldSaveHudMode.Disabled: _text = _disabled; break;
                        case CombatPrototypeMapWorldSaveHudMode.NotSaved: _text = _notSaved; break;
                        case CombatPrototypeMapWorldSaveHudMode.Saved: _text = _saved; break;
                        case CombatPrototypeMapWorldSaveHudMode.CaptureFailed: _text = _captureFailed; break;
                        case CombatPrototypeMapWorldSaveHudMode.SaveFailed: _text = _saveFailed; break;
                        default: InvalidSnapshot(state); return;
                    }
                    _mode = state.Mode;
                }
                if (!_observedFeedback)
                {
                    _observedFeedback = true;
                    _sequence = state.ManualSequence;
                }
                else if (_sequence != state.ManualSequence)
                {
                    _sequence = state.ManualSequence;
                    _feedbackError = state.ManualResult == CombatPrototypeMapWorldSaveManualResult.Failed;
                    switch (state.ManualResult)
                    {
                        case CombatPrototypeMapWorldSaveManualResult.None: _feedbackUntil = 0d; break;
                        case CombatPrototypeMapWorldSaveManualResult.Success: _feedback = _saved; break;
                        case CombatPrototypeMapWorldSaveManualResult.Disabled: _feedback = _manualDisabled; break;
                        case CombatPrototypeMapWorldSaveManualResult.Cooldown: _feedback = _cooldown; break;
                        case CombatPrototypeMapWorldSaveManualResult.Unavailable: _feedback = _unavailable; break;
                        case CombatPrototypeMapWorldSaveManualResult.Failed: _feedback = _saveFailed; break;
                        default: InvalidSnapshot(state); return;
                    }
                    if (state.ManualResult != CombatPrototypeMapWorldSaveManualResult.None)
                        _feedbackUntil = Time.unscaledTimeAsDouble + _settings.FeedbackSeconds;
                }
                _visible = true;
            }
            catch (Exception exception)
            {
                Clear();
                Debug.LogError("[CombatPrototype.Map] World save HUD display failed; stage=ReadSnapshot, map=" + _mapId +
                    ", mode=" + state.Mode + ", sequence=" + state.ManualSequence + ", result=" + state.ManualResult + ". " + exception);
            }
        }

        private static void InvalidSnapshot(CombatPrototypeMapWorldSaveHudState state) =>
            throw new InvalidOperationException("Invalid owner world save snapshot; mode=" + state.Mode +
                ", sequence=" + state.ManualSequence + ", result=" + state.ManualResult + ".");

        internal void Clear() => _visible = false;

        internal void Reset()
        {
            Clear();
            _settings = default;
            _mapId = _disabled = _notSaved = _saved = _captureFailed = _saveFailed = string.Empty;
            _manualSave = _manualDisabled = _cooldown = _unavailable = _text = _feedback = string.Empty;
            _labelStyle = null;
            _errorColor = default;
            _mode = CombatPrototypeMapWorldSaveHudMode.Hidden;
            _manualEnabled = _observedFeedback = _feedbackError = false;
            _sequence = 0;
            _feedbackUntil = 0d;
        }

        internal void Draw()
        {
            if (!_visible || Event.current.type != EventType.Repaint || Screen.width <= 0 || Screen.height <= 0) return;
            if (_labelStyle == null)
                _labelStyle = new GUIStyle(GUI.skin.label)
                    { alignment = TextAnchor.MiddleCenter, fontSize = _settings.FontSize, richText = false };
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
                var rowHeight = _settings.FontSize + 4f;
                var top = panel.y + (panel.height - 2f * rowHeight - 8f) * 0.5f;
                _labelStyle.normal.textColor = _mode == CombatPrototypeMapWorldSaveHudMode.CaptureFailed ||
                    _mode == CombatPrototypeMapWorldSaveHudMode.SaveFailed ? _errorColor : Color.white;
                GUI.Label(new Rect(panel.x + 12f, top, panel.width - 24f, rowHeight), _text, _labelStyle);
                var hasFeedback = Time.unscaledTimeAsDouble < _feedbackUntil;
                _labelStyle.normal.textColor = hasFeedback && _feedbackError ? _errorColor : Color.white;
                GUI.Label(new Rect(panel.x + 12f, top + rowHeight + 8f, panel.width - 24f, rowHeight),
                    hasFeedback ? _feedback : _manualEnabled ? _manualSave : _manualDisabled, _labelStyle);
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }
    }
}
