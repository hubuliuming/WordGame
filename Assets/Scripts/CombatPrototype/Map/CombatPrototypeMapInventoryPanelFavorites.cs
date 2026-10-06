using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    // Local favorite names and display projection; inventory and business requests remain in the original chain.
    internal sealed class CombatPrototypeMapInventoryPanelFavorites
    {
        public const int MaximumStoredCount = 256;
        private readonly HashSet<FixedString64Bytes> _names = new HashSet<FixedString64Bytes>();
        private readonly List<CombatPrototypeMapInventoryPanelSnapshot.Row> _ordinary =
            new List<CombatPrototypeMapInventoryPanelSnapshot.Row>();
        private readonly Dictionary<FixedString64Bytes, string> _markedText = new Dictionary<FixedString64Bytes, string>();
        private FixedString64Bytes _pendingName;
        private string _favorite, _unfavorite, _tag, _full;
        private int _maximum;
        private bool _pending;

        public bool Enabled { get; private set; }
        public bool HasPending => _pending;
        public uint Revision { get; private set; }

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings)
        {
            Reset();
            Enabled = settings.FavoritesEnabled != 0;
            _maximum = settings.FavoritesMaxCount;
            _favorite = settings.FavoriteButtonLabel.ToString();
            _unfavorite = settings.UnfavoriteButtonLabel.ToString();
            _tag = settings.FavoriteTagLabel.ToString();
            _full = settings.FavoritesFullLabel.ToString();
        }

        // Store has already validated the complete external record, including absent inventory names.
        public void Restore(IReadOnlyList<string> names)
        {
            if (!Enabled) return;
            foreach (var name in names) _names.Add(new FixedString64Bytes(name));
            if (_names.Count != 0) Revision++;
        }

        // Called after the latest filter/search has produced valid, positive visible rows.
        public bool ApplyPending(IReadOnlyList<CombatPrototypeMapInventoryPanelSnapshot.Row> visible)
        {
            if (!_pending) return false;
            var name = _pendingName;
            ClearPending();
            if (!Enabled) return false;
            var found = false;
            foreach (var row in visible)
                if (row.Name.Equals(name)) { found = true; break; }
            if (!found) return false;
            if (!_names.Remove(name))
            {
                if (_names.Count >= _maximum) return false;
                _names.Add(name);
            }
            Revision++;
            return true;
        }

        // Stable partition preserves the existing original/type/quantity order within both groups.
        public void Pin(List<CombatPrototypeMapInventoryPanelSnapshot.Row> rows)
        {
            if (!Enabled || _names.Count == 0) return;
            _ordinary.Clear();
            var write = 0;
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (_names.Contains(row.Name)) rows[write++] = row;
                else _ordinary.Add(row);
            }
            foreach (var row in _ordinary) rows[write++] = row;
            _ordinary.Clear();
        }

        public void CaptureLabels(IReadOnlyList<CombatPrototypeMapInventoryPanelSnapshot.Row> rows)
        {
            _markedText.Clear();
            if (!Enabled) return;
            foreach (var row in rows)
                if (_names.Contains(row.Name)) _markedText.Add(row.Name, row.Text + " [" + _tag + "]");
        }

        public string ItemText(CombatPrototypeMapInventoryPanelSnapshot.Row row) =>
            Enabled && _names.Contains(row.Name) ? _markedText[row.Name] : row.Text;

        public void CopyNames(List<string> destination)
        {
            destination.Clear();
            foreach (var name in _names) destination.Add(name.ToString());
            destination.Sort(StringComparer.Ordinal);
        }

        public void DrawRow(float width, ref float y, float rowHeight, GUIStyle buttonStyle,
            CombatPrototypeMapInventoryPanelSnapshot.Row row, bool mousePressAccepted)
        {
            if (!Enabled) return;
            var favorite = _names.Contains(row.Name);
            var allowed = favorite || _names.Count < _maximum;
            var oldEnabled = GUI.enabled;
            try
            {
                GUI.enabled = oldEnabled && allowed;
                if (GUI.Button(new Rect(0f, y, width, rowHeight - 4f),
                    favorite ? _unfavorite : allowed ? _favorite : _full, buttonStyle) && mousePressAccepted)
                {
                    _pendingName = row.Name;
                    _pending = true;
                }
            }
            finally { GUI.enabled = oldEnabled; }
            y += rowHeight;
        }

        public void ClearPending() { _pending = false; _pendingName = default; }

        public void ResetDisplay()
        {
            ClearPending();
            if (!Enabled || _names.Count == 0) return;
            _names.Clear();
            _markedText.Clear();
            Revision++;
        }

        public void Reset()
        {
            ClearPending();
            _names.Clear(); _ordinary.Clear(); _markedText.Clear();
            Enabled = false;
            Revision = 0;
            _maximum = 0;
            _favorite = _unfavorite = _tag = _full = string.Empty;
        }
    }
}
