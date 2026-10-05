using System;
using System.Globalization;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapPickupLifetimeHudClient
    {
        private CombatPrototypeMapPickupHudSettings _settings;
        private CombatPrototypeMapPickupLifetimeHudMode _mode;
        private float _remaining;
        private GUIStyle _labelStyle;
        private Color _warningColor;
        private string _expiresInLabel;
        private string _permanentLabel;
        private string _expiringSoonLabel;
        private string _secondsLabel;
        private string _text;
        private bool _visible;

        internal bool Visible => _visible;

        internal void Configure(CombatPrototypeMapPickupHudSettings settings)
        {
            Reset();
            _settings = settings;
            _expiresInLabel = settings.ExpiresInLabel.ToString();
            _permanentLabel = settings.PermanentLabel.ToString();
            _expiringSoonLabel = settings.ExpiringSoonLabel.ToString();
            _secondsLabel = settings.SecondsLabel.ToString();
            _warningColor = new Color(settings.ExpiryWarningColor.x, settings.ExpiryWarningColor.y,
                settings.ExpiryWarningColor.z, 1f);
        }

        internal void Show(CombatPrototypeMapPickupHudState state)
        {
            Clear();
            if (state.Mode == CombatPrototypeMapPickupHudMode.Hidden || _settings.LifetimeEnabled == 0)
            {
                if (state.LifetimeMode != CombatPrototypeMapPickupLifetimeHudMode.None || state.RemainingSeconds != 0f)
                    InvalidSnapshot(state);
                return;
            }
            switch (state.LifetimeMode)
            {
                case CombatPrototypeMapPickupLifetimeHudMode.Timed:
                case CombatPrototypeMapPickupLifetimeHudMode.ExpiringSoon:
                    if (float.IsNaN(state.RemainingSeconds) || float.IsInfinity(state.RemainingSeconds) ||
                        state.RemainingSeconds <= 0f || state.RemainingSeconds != Mathf.Ceil(state.RemainingSeconds) ||
                        (state.LifetimeMode == CombatPrototypeMapPickupLifetimeHudMode.ExpiringSoon && _settings.ExpiryWarningEnabled == 0))
                        InvalidSnapshot(state);
                    break;
                case CombatPrototypeMapPickupLifetimeHudMode.Permanent:
                    if (state.RemainingSeconds != 0f) InvalidSnapshot(state);
                    break;
                default:
                    InvalidSnapshot(state);
                    return;
            }
            if (_mode != state.LifetimeMode || _remaining != state.RemainingSeconds)
            {
                _text = state.LifetimeMode == CombatPrototypeMapPickupLifetimeHudMode.Permanent ? _permanentLabel :
                    (state.LifetimeMode == CombatPrototypeMapPickupLifetimeHudMode.ExpiringSoon ? _expiringSoonLabel : _expiresInLabel) +
                    " " + state.RemainingSeconds.ToString("0", CultureInfo.InvariantCulture) + _secondsLabel;
            }
            _mode = state.LifetimeMode;
            _remaining = state.RemainingSeconds;
            _visible = true;
        }

        private static void InvalidSnapshot(CombatPrototypeMapPickupHudState state) =>
            throw new InvalidOperationException("Invalid owner pickup lifetime snapshot; DropId=" + state.DropId +
                ", itemId=" + state.ItemId + ", lifetimeMode=" + state.LifetimeMode + ", remaining=" + state.RemainingSeconds + ".");

        internal void Clear() => _visible = false;

        internal void Reset()
        {
            Clear();
            _settings = default;
            _mode = CombatPrototypeMapPickupLifetimeHudMode.None;
            _remaining = 0f;
            _labelStyle = null;
            _warningColor = default;
            _expiresInLabel = _permanentLabel = _expiringSoonLabel = _secondsLabel = _text = string.Empty;
        }

        internal void Draw(Rect row)
        {
            if (!_visible) return;
            if (_labelStyle == null)
                _labelStyle = new GUIStyle(GUI.skin.label)
                    { alignment = TextAnchor.MiddleCenter, fontSize = _settings.FontSize, richText = false };
            _labelStyle.normal.textColor = _mode == CombatPrototypeMapPickupLifetimeHudMode.ExpiringSoon ? _warningColor : Color.white;
            GUI.Label(row, _text, _labelStyle);
        }
    }
}
