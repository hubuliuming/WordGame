using UnityEngine;
using ConsumptionOperation = Code_01.CombatPrototype.Map.CombatPrototypeMapInventoryConsumptionConfirmation.Operation;

namespace Code_01.CombatPrototype.Map
{
    // Cached local recipe text and visibility. The shared editor owns focus and draft/applied input.
    internal sealed class CombatPrototypeMapInventoryRecipeSearch
    {
        public CombatPrototypeMapInventoryPanelSearch Editor { get; } = new CombatPrototypeMapInventoryPanelSearch();
        private readonly string[] _texts = new string[8];
        private readonly bool[] _visible = new bool[8];
        private CombatPrototypeMapInventoryRecipeFilterMode _mode;
        private uint _revision;
        private bool _captured;

        public bool CraftAxe => _visible[(int)ConsumptionOperation.CraftAxe];
        public bool CraftPickaxe => _visible[(int)ConsumptionOperation.CraftPickaxe];
        public bool RepairAxe => _visible[(int)ConsumptionOperation.RepairAxe];
        public bool RepairPickaxe => _visible[(int)ConsumptionOperation.RepairPickaxe];
        public bool CapacityUpgrade => _visible[(int)ConsumptionOperation.CapacityUpgrade];
        public bool UpgradeAxe => _visible[(int)ConsumptionOperation.UpgradeAxe];
        public bool UpgradePickaxe => _visible[(int)ConsumptionOperation.UpgradePickaxe];
        public bool ShowCraft => CraftAxe || CraftPickaxe;
        public bool ShowRepair => RepairAxe || RepairPickaxe;
        public bool ShowToolUpgrade => UpgradeAxe || UpgradePickaxe;
        public bool HasResults => ShowCraft || ShowRepair || CapacityUpgrade || ShowToolUpgrade;
        public int CraftRowCount => ShowCraft ? 1 + (CraftAxe ? 4 : 0) + (CraftPickaxe ? 4 : 0) : 0;

        public void Configure(CombatPrototypeMapInventoryPanelSettings panel, CombatPrototypeMapGatherToolDefinition axe,
            CombatPrototypeMapGatherToolDefinition pickaxe, CombatPrototypeMapInventoryCapacityUpgradeSettings capacity,
            CombatPrototypeMapGatherToolUpgradeSettings tools)
        {
            Reset();
            Editor.ConfigureRecipeSearch(panel);
            var craft = panel.CraftLabel + "\n" + panel.CraftButtonLabel + "\n" + tools.RecraftLabel;
            var repair = panel.RepairLabel + "\n" + panel.RepairButtonLabel;
            var upgrade = tools.UpgradeLabel + "\n" + tools.UpgradeButtonLabel;
            _texts[(int)ConsumptionOperation.CraftAxe] = axe.DisplayName + "\n" + craft;
            _texts[(int)ConsumptionOperation.CraftPickaxe] = pickaxe.DisplayName + "\n" + craft;
            _texts[(int)ConsumptionOperation.RepairAxe] = axe.DisplayName + "\n" + repair;
            _texts[(int)ConsumptionOperation.RepairPickaxe] = pickaxe.DisplayName + "\n" + repair;
            _texts[(int)ConsumptionOperation.CapacityUpgrade] = capacity.UpgradeLabel + "\n" + capacity.UpgradeButtonLabel + "\n" + panel.CapacityLabel;
            _texts[(int)ConsumptionOperation.UpgradeAxe] = axe.DisplayName + "\n" + upgrade;
            _texts[(int)ConsumptionOperation.UpgradePickaxe] = pickaxe.DisplayName + "\n" + upgrade;
        }

        public void Capture(CombatPrototypeMapInventoryRecipeFilter filter)
        {
            if (_captured && _mode == filter.Mode && _revision == Editor.Revision) return;
            _captured = true;
            _mode = filter.Mode;
            _revision = Editor.Revision;
            for (var index = 1; index < _visible.Length; index++)
            {
                var category = index <= (int)ConsumptionOperation.CraftPickaxe ? filter.ShowCraft :
                    index <= (int)ConsumptionOperation.RepairPickaxe ? filter.ShowRepair : filter.ShowUpgrade;
                _visible[index] = category && Editor.Matches(_texts[index]);
            }
        }

        public void Draw(float width, ref float y, float rowHeight, GUIStyle labelStyle, GUIStyle buttonStyle, bool mousePressAccepted)
        {
            if (!Editor.Enabled) return;
            GUI.Label(new Rect(0f, y, width, rowHeight), Editor.Label, labelStyle);
            y += rowHeight;
            Editor.Draw(width, y, rowHeight, labelStyle, buttonStyle, mousePressAccepted);
            y += rowHeight;
        }

        public void Reset()
        {
            Editor.Reset();
            _captured = false;
            _revision = 0;
            _mode = CombatPrototypeMapInventoryRecipeFilterMode.All;
            for (var index = 0; index < _texts.Length; index++) { _texts[index] = string.Empty; _visible[index] = false; }
        }
    }
}
