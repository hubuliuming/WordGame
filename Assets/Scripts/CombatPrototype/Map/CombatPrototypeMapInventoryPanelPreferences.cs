using System;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    // 管理本次客户端绑定的读取、已应用状态观察、延迟写入和失败隔离。
    internal sealed class CombatPrototypeMapInventoryPanelPreferences
    {
        private CombatPrototypeMapInventoryPanelPreferencesData _data;
        private CombatPrototypeMapInventorySortMode _observedSort, _writtenSort;
        private CombatPrototypeMapInventoryFilterMode _observedFilter, _writtenFilter;
        private string _observedSearch, _writtenSearch, _mapId, _path;
        private float _delay, _changedAt;
        private bool _active, _dirty, _sortEnabled, _filterEnabled, _saveSearch;

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings, string mapId,
            CombatPrototypeMapInventoryPanelListView view, CombatPrototypeMapInventoryPanelSearch search)
        {
            // Panel先Reset并提交旧绑定，再配置本绑定；关闭功能时不访问文件。
            if (settings.Enabled == 0 || settings.PreferencesEnabled == 0) return;
            _mapId = mapId;
            _sortEnabled = view.SortEnabled;
            _filterEnabled = view.FilterEnabled;
            _saveSearch = settings.PreferencesSaveSearch != 0 && search.Enabled;
            _delay = settings.PreferencesSaveDelaySeconds;
            try
            {
                _path = CombatPrototypeMapInventoryPanelPreferencesStore.PathFor(settings.PreferencesFileId.ToString(), mapId);
                _data = CombatPrototypeMapInventoryPanelPreferencesStore.Load(_path, mapId);
                if (_data == null)
                    _data = new CombatPrototypeMapInventoryPanelPreferencesData
                    {
                        Version = CombatPrototypeMapInventoryPanelPreferencesStore.CurrentVersion,
                        MapDefinitionId = mapId,
                        SortMode = settings.DefaultSortMode,
                        FilterMode = settings.DefaultFilterMode,
                        SearchText = string.Empty
                    };
                else
                {
                    view.Restore(_data.SortMode, _data.FilterMode);
                    if (_saveSearch) search.Restore(_data.SearchText);
                }
                _writtenSort = _data.SortMode;
                _writtenFilter = _data.FilterMode;
                _writtenSearch = _data.SearchText;
                _observedSort = view.SortMode;
                _observedFilter = view.FilterMode;
                _observedSearch = search.AppliedText;
                _active = true;
            }
            catch (Exception exception) { Disable("Load", exception); }
        }

        public void Capture(CombatPrototypeMapInventoryPanelListView view, CombatPrototypeMapInventoryPanelSearch search)
        {
            if (!_active) return;
            var changed = false;
            if (_sortEnabled && _observedSort != view.SortMode)
            {
                _data.SortMode = _observedSort = view.SortMode;
                changed = true;
            }
            if (_filterEnabled && _observedFilter != view.FilterMode)
            {
                _data.FilterMode = _observedFilter = view.FilterMode;
                changed = true;
            }
            if (_saveSearch && _observedSearch != search.AppliedText)
            {
                _data.SearchText = _observedSearch = search.AppliedText;
                changed = true;
            }
            if (changed)
            {
                _changedAt = Time.unscaledTime;
                _dirty = _data.SortMode != _writtenSort || _data.FilterMode != _writtenFilter || _data.SearchText != _writtenSearch;
            }
            if (_dirty && Time.unscaledTime - _changedAt >= _delay) Flush();
        }

        public void Flush()
        {
            if (!_active || !_dirty) return;
            try
            {
                CombatPrototypeMapInventoryPanelPreferencesStore.Save(_path, _data);
                _writtenSort = _data.SortMode;
                _writtenFilter = _data.FilterMode;
                _writtenSearch = _data.SearchText;
                _dirty = false;
            }
            catch (Exception exception) { Disable("Save", exception); }
        }

        private void Disable(string stage, Exception exception)
        {
            _active = false;
            Debug.LogError("[CombatPrototype.Map.InventoryPreferences] stage=" + stage + "; map=" + _mapId +
                "; path=" + _path + "; preferences I/O disabled for this binding.\n" + exception);
        }

        public void Reset()
        {
            _active = _dirty = _sortEnabled = _filterEnabled = _saveSearch = false;
            _data = null;
            _observedSort = _writtenSort = CombatPrototypeMapInventorySortMode.Original;
            _observedFilter = _writtenFilter = CombatPrototypeMapInventoryFilterMode.All;
            _observedSearch = _writtenSearch = _mapId = _path = string.Empty;
            _delay = _changedAt = 0f;
        }
    }
}
