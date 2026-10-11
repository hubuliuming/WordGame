using System;
using System.Collections.Generic;
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
        private CombatPrototypeMapInventoryRecipeFilterMode _observedRecipeFilter, _writtenRecipeFilter;
        private string _observedRecipeSearch, _writtenRecipeSearch;
        private bool _saveRecipeFilter, _saveRecipeSearch;
        private readonly HashSet<string> _writtenFavorites = new HashSet<string>(StringComparer.Ordinal);
        private uint _observedFavoritesRevision;
        private float _delay, _changedAt;
        private bool _active, _dirty, _sortEnabled, _filterEnabled, _saveSearch, _saveFavorites, _saveFavoritesFilter;
        private bool _observedFavoritesOnly, _writtenFavoritesOnly;

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings, string mapId,
            CombatPrototypeMapInventoryPanelListView view, CombatPrototypeMapInventoryPanelSearch search, CombatPrototypeMapInventoryPanelFavorites favorites,
            CombatPrototypeMapInventoryRecipeFilter recipeFilter, CombatPrototypeMapInventoryPanelSearch recipeSearch)
        {
            // Panel先Reset并提交旧绑定，再配置本绑定；关闭功能时不访问文件。
            if (settings.Enabled == 0 || settings.PreferencesEnabled == 0) return;
            _mapId = mapId;
            _sortEnabled = view.SortEnabled;
            _filterEnabled = view.FilterEnabled;
            _saveSearch = settings.PreferencesSaveSearch != 0 && search.Enabled;
            _saveRecipeFilter = settings.PreferencesSaveRecipeFilter != 0 && recipeFilter.Enabled;
            _saveRecipeSearch = settings.PreferencesSaveRecipeSearch != 0 && recipeSearch.Enabled;
            _saveFavorites = favorites.Enabled;
            _saveFavoritesFilter = view.FavoritesFilterEnabled;
            _delay = settings.PreferencesSaveDelaySeconds;
            try
            {
                _path = CombatPrototypeMapInventoryPanelPreferencesStore.PathFor(settings.PreferencesFileId.ToString(), mapId);
                _data = CombatPrototypeMapInventoryPanelPreferencesStore.Load(_path, mapId, settings.DefaultRecipeFilterMode);
                if (_data == null)
                    _data = new CombatPrototypeMapInventoryPanelPreferencesData
                    {
                        Version = CombatPrototypeMapInventoryPanelPreferencesStore.CurrentVersion,
                        MapDefinitionId = mapId,
                        SortMode = settings.DefaultSortMode,
                        FilterMode = settings.DefaultFilterMode,
                        SearchText = string.Empty,
                        FavoritesOnly = settings.DefaultFavoritesOnly != 0,
                        RecipeFilterMode = settings.DefaultRecipeFilterMode,
                        RecipeSearchText = string.Empty
                    };
                else
                {
                    view.Restore(_data.SortMode, _data.FilterMode);
                    if (_saveSearch) search.Restore(_data.SearchText);
                    if (_saveFavorites) favorites.Restore(_data.FavoriteItemNames);
                    if (_saveFavoritesFilter) view.RestoreFavoritesOnly(_data.FavoritesOnly);
                    if (_saveRecipeFilter) recipeFilter.Restore(_data.RecipeFilterMode);
                    if (_saveRecipeSearch) recipeSearch.Restore(_data.RecipeSearchText);
                }
                _writtenSort = _data.SortMode;
                _writtenFilter = _data.FilterMode;
                _writtenSearch = _data.SearchText;
                _writtenRecipeFilter = _data.RecipeFilterMode;
                _writtenRecipeSearch = _data.RecipeSearchText;
                _writtenFavoritesOnly = _data.FavoritesOnly;
                _writtenFavorites.Clear();
                _writtenFavorites.UnionWith(_data.FavoriteItemNames);
                _observedSort = view.SortMode;
                _observedFilter = view.FilterMode;
                _observedSearch = search.AppliedText;
                _observedRecipeFilter = recipeFilter.Mode;
                _observedRecipeSearch = recipeSearch.AppliedText;
                _observedFavoritesRevision = favorites.Revision;
                _observedFavoritesOnly = view.FavoritesOnly;
                _active = true;
            }
            catch (Exception exception) { Disable("Load", exception); }
        }

        public void Capture(CombatPrototypeMapInventoryPanelListView view, CombatPrototypeMapInventoryPanelSearch search, CombatPrototypeMapInventoryPanelFavorites favorites,
            CombatPrototypeMapInventoryRecipeFilter recipeFilter, CombatPrototypeMapInventoryPanelSearch recipeSearch)
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
            if (_saveFavorites && _observedFavoritesRevision != favorites.Revision)
            {
                favorites.CopyNames(_data.FavoriteItemNames);
                _observedFavoritesRevision = favorites.Revision;
                changed = true;
            }
            if (_saveFavoritesFilter && _observedFavoritesOnly != view.FavoritesOnly)
            {
                _data.FavoritesOnly = _observedFavoritesOnly = view.FavoritesOnly;
                changed = true;
            }
            if (_saveRecipeFilter && _observedRecipeFilter != recipeFilter.Mode)
            {
                _data.RecipeFilterMode = _observedRecipeFilter = recipeFilter.Mode;
                changed = true;
            }
            if (_saveRecipeSearch && _observedRecipeSearch != recipeSearch.AppliedText)
            {
                _data.RecipeSearchText = _observedRecipeSearch = recipeSearch.AppliedText;
                changed = true;
            }
            if (changed)
            {
                _changedAt = Time.unscaledTime;
                _dirty = _data.SortMode != _writtenSort || _data.FilterMode != _writtenFilter || _data.SearchText != _writtenSearch || _data.FavoritesOnly != _writtenFavoritesOnly ||
                    _data.RecipeFilterMode != _writtenRecipeFilter || _data.RecipeSearchText != _writtenRecipeSearch || FavoritesDiffer();
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
                _writtenRecipeFilter = _data.RecipeFilterMode;
                _writtenRecipeSearch = _data.RecipeSearchText;
                _writtenFavoritesOnly = _data.FavoritesOnly;
                _writtenFavorites.Clear();
                _writtenFavorites.UnionWith(_data.FavoriteItemNames);
                _dirty = false;
            }
            catch (Exception exception) { Disable("Save", exception); }
        }

        private bool FavoritesDiffer()
        {
            if (_data.FavoriteItemNames.Count != _writtenFavorites.Count) return true;
            foreach (var name in _data.FavoriteItemNames)
                if (!_writtenFavorites.Contains(name)) return true;
            return false;
        }

        private void Disable(string stage, Exception exception)
        {
            _active = false;
            Debug.LogError("[CombatPrototype.Map.InventoryPreferences] stage=" + stage + "; map=" + _mapId +
                "; path=" + _path + "; preferences I/O disabled for this binding.\n" + exception);
        }

        public void Reset()
        {
            _active = _dirty = _sortEnabled = _filterEnabled = _saveSearch = _saveFavorites = _saveFavoritesFilter = false;
            _observedFavoritesOnly = _writtenFavoritesOnly = _saveRecipeFilter = _saveRecipeSearch = false;
            _data = null;
            _writtenFavorites.Clear();
            _observedFavoritesRevision = 0;
            _observedSort = _writtenSort = CombatPrototypeMapInventorySortMode.Original;
            _observedFilter = _writtenFilter = CombatPrototypeMapInventoryFilterMode.All;
            _observedRecipeFilter = _writtenRecipeFilter = CombatPrototypeMapInventoryRecipeFilterMode.All;
            _observedRecipeSearch = _writtenRecipeSearch = string.Empty;
            _observedSearch = _writtenSearch = _mapId = _path = string.Empty;
            _delay = _changedAt = 0f;
        }
    }
}
