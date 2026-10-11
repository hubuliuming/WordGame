using System.Collections.Generic;

namespace Code_01.CombatPrototype.Map
{
    // 仅表示本机显示偏好；不承载库存、输入、Ghost或玩家/世界档案。
    internal sealed class CombatPrototypeMapInventoryPanelPreferencesData
    {
        public int Version;
        public string MapDefinitionId;
        public CombatPrototypeMapInventorySortMode SortMode;
        public CombatPrototypeMapInventoryFilterMode FilterMode;
        public string SearchText;
        public List<string> FavoriteItemNames = new List<string>();
        public bool FavoritesOnly;
        public CombatPrototypeMapInventoryRecipeFilterMode RecipeFilterMode;
        public string RecipeSearchText;
        public List<string> FavoriteRecipeIds = new List<string>();
    }
}
