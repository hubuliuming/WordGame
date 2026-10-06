using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code_01.CombatPrototype.Map
{
    // One local search editor. GUI focus is only read/released during OnGUI.
    internal sealed class CombatPrototypeMapInventoryPanelSearch
    {
        private readonly string _controlName = "CombatPrototype.InventorySearch." + Guid.NewGuid().ToString("N");
        private string _draft = string.Empty, _applied = string.Empty, _query = string.Empty;
        private string _placeholder, _clearLabel;
        private StringComparison _comparison;
        private GUIStyle _textStyle;
        private int _maximum, _fontSize, _keyboardFrame = -1, _mouseFrame = -1;
        private bool _pending, _focused, _focusRequested, _releaseGUIFocus;

        public bool Enabled { get; private set; }
        public string Label { get; private set; }
        public string NoResultsText { get; private set; }
        public uint Revision { get; private set; }
        public string AppliedText => _applied;
        public bool HasQuery => Enabled && _query.Length != 0;
        public int RowCount => Enabled ? 2 : 0;
        public bool BlocksKeyboard => Enabled && (_focused || _focusRequested || _keyboardFrame == Time.frameCount);
        public bool BlocksMouse => _mouseFrame == Time.frameCount;

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings)
        {
            Reset();
            Enabled = settings.SearchEnabled != 0;
            _comparison = settings.SearchIgnoreCase != 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            _maximum = settings.SearchMaxLength;
            _fontSize = settings.FontSize;
            Label = settings.SearchLabel.ToString();
            _placeholder = settings.SearchPlaceholderLabel.ToString();
            _clearLabel = settings.ClearSearchLabel.ToString();
            NoResultsText = settings.NoSearchResultsLabel.ToString();
        }

        // Sample geometry before GUI callbacks so a focus-changing click blocks this frame's keys.
        public void ReadInput(Keyboard keyboard, Mouse mouse, bool insideField, bool insidePanel)
        {
            if (!Enabled) return;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (insideField)
                {
                    _focused = _focusRequested = true;
                    _keyboardFrame = Time.frameCount;
                }
                else if (_focused || _focusRequested)
                {
                    ReleaseFocus();
                    if (!insidePanel) _mouseFrame = Time.frameCount;
                }
            }
            // Enter/Escape belong to an active IME candidate until composition ends.
            if ((_focused || _focusRequested) && keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame) &&
                UnityEngine.Input.compositionString.Length == 0) ReleaseFocus();
        }

        public void ReleaseFocus()
        {
            if (_focused || _focusRequested) _keyboardFrame = Time.frameCount;
            _focused = _focusRequested = false;
            _releaseGUIFocus = true;
        }

        public void FlushGUIFocus()
        {
            if (!_releaseGUIFocus) return;
            if (GUI.GetNameOfFocusedControl() == _controlName) GUI.FocusControl(null);
            _releaseGUIFocus = false;
        }

        public static Rect FieldRect(float width, float y, float height) => new Rect(0f, y, width * 0.75f - 4f, height - 4f);

        public void Draw(float width, float y, float height, GUIStyle labelStyle, GUIStyle buttonStyle, bool mousePressAccepted)
        {
            if (_textStyle == null)
                _textStyle = new GUIStyle(GUI.skin.textField) { fontSize = _fontSize, richText = false, wordWrap = false };
            var field = FieldRect(width, y, height);
            GUI.SetNextControlName(_controlName);
            var text = GUI.TextField(field, _draft, _maximum, _textStyle);
            if (text != _draft) { _draft = CleanInput(text); _pending = true; }
            if (_focusRequested) { GUI.FocusControl(_controlName); _focusRequested = false; }
            var focused = GUI.GetNameOfFocusedControl() == _controlName;
            if (_focused && !focused) _keyboardFrame = Time.frameCount;
            _focused = focused;
            if (_draft.Length == 0 && !focused && UnityEngine.Input.compositionString.Length == 0)
                GUI.Label(new Rect(field.x + 4f, field.y, field.width - 8f, field.height), _placeholder, labelStyle);
            var oldEnabled = GUI.enabled;
            try
            {
                GUI.enabled = oldEnabled && _draft.Length != 0;
                if (GUI.Button(new Rect(width * 0.75f, y, width * 0.25f, height - 4f), _clearLabel, buttonStyle) && mousePressAccepted)
                {
                    _draft = string.Empty;
                    _pending = true;
                    ReleaseFocus();
                }
            }
            finally { GUI.enabled = oldEnabled; }
        }

        // 绑定时使用当前长度限制恢复文本，不聚焦、不排队编辑或直接写文件。
        public void Restore(string text)
        {
            _draft = _applied = CleanInput(text);
            _query = _applied.Trim();
            _pending = false;
            Revision = unchecked(Revision + 1);
        }

        // 清已应用查询和草稿；焦点释放仍走原GUI边界。
        public void ResetDisplay()
        {
            if (_applied.Length != 0) Revision = unchecked(Revision + 1);
            _draft = _applied = _query = string.Empty;
            _pending = false;
            ReleaseFocus();
        }

        public bool ApplyPending()
        {
            if (!_pending) return false;
            _pending = false;
            if (_applied == _draft) return false;
            _applied = _draft;
            _query = _applied.Trim();
            Revision = unchecked(Revision + 1);
            return true;
        }

        public bool Matches(CombatPrototypeMapInventoryPanelSnapshot.Row row) => !HasQuery ||
            row.OriginalName.IndexOf(_query, _comparison) >= 0 || row.DisplayName.IndexOf(_query, _comparison) >= 0;

        // External text can include pasted controls or a surrogate cut by the native length limit.
        private string CleanInput(string text)
        {
            var length = Math.Min(text.Length, _maximum);
            var dirty = false;
            for (var index = 0; index < length; index++)
            {
                var character = text[index];
                if (char.IsControl(character) || char.IsLowSurrogate(character)) { dirty = true; break; }
                if (!char.IsHighSurrogate(character)) continue;
                if (index + 1 >= length || !char.IsLowSurrogate(text[index + 1])) { dirty = true; break; }
                index++;
            }
            if (!dirty) return length == text.Length ? text : text.Substring(0, length);
            var builder = new StringBuilder(length);
            for (var index = 0; index < length; index++)
            {
                var character = text[index];
                if (char.IsControl(character) || char.IsLowSurrogate(character)) continue;
                if (char.IsHighSurrogate(character))
                {
                    if (index + 1 < length && char.IsLowSurrogate(text[index + 1]))
                    { builder.Append(character); builder.Append(text[++index]); }
                }
                else builder.Append(character);
            }
            return builder.ToString();
        }

        public void Close()
        {
            _draft = _applied;
            _pending = false;
            ReleaseFocus();
        }

        public void Reset()
        {
            ReleaseFocus();
            Enabled = _pending = false;
            _draft = _applied = _query = string.Empty;
            Label = NoResultsText = _placeholder = _clearLabel = string.Empty;
            _keyboardFrame = _mouseFrame = -1;
            _maximum = _fontSize = 0;
            Revision = 0;
            _textStyle = null;
        }
    }
}
