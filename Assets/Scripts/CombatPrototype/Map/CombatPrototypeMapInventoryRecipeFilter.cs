using System;

namespace Code_01.CombatPrototype.Map
{
    public enum CombatPrototypeMapInventoryRecipeFilterMode : byte
    {
        All = 0,
        Craft = 1,
        Repair = 2,
        Upgrade = 3
    }

    // Local recipe visibility only; inventory, eligibility and gameplay requests retain their original owners.
    internal sealed class CombatPrototypeMapInventoryRecipeFilter
    {
        private CombatPrototypeMapInventoryRecipeFilterMode _defaultMode;
        private bool _togglePending;
        private string _label, _allLabel, _craftLabel, _repairLabel, _upgradeLabel;

        public bool Enabled { get; private set; }
        public CombatPrototypeMapInventoryRecipeFilterMode Mode { get; private set; }
        public string Text { get; private set; }
        public int RowCount => Enabled ? 1 : 0;
        public bool ShowCraft => Mode == CombatPrototypeMapInventoryRecipeFilterMode.All || Mode == CombatPrototypeMapInventoryRecipeFilterMode.Craft;
        public bool ShowRepair => Mode == CombatPrototypeMapInventoryRecipeFilterMode.All || Mode == CombatPrototypeMapInventoryRecipeFilterMode.Repair;
        public bool ShowUpgrade => Mode == CombatPrototypeMapInventoryRecipeFilterMode.All || Mode == CombatPrototypeMapInventoryRecipeFilterMode.Upgrade;

        internal static CombatPrototypeMapInventoryRecipeFilterMode ResolveMode(string value, string source = "inventoryPanel.defaultRecipeFilterMode")
        {
            switch (value)
            {
                case "all": return CombatPrototypeMapInventoryRecipeFilterMode.All;
                case "craft": return CombatPrototypeMapInventoryRecipeFilterMode.Craft;
                case "repair": return CombatPrototypeMapInventoryRecipeFilterMode.Repair;
                case "upgrade": return CombatPrototypeMapInventoryRecipeFilterMode.Upgrade;
                default: throw new InvalidOperationException(source + " requires all, craft, repair or upgrade; value=" + value + ".");
            }
        }

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings)
        {
            Reset();
            Enabled = settings.RecipeFilterEnabled != 0;
            _defaultMode = settings.DefaultRecipeFilterMode;
            Mode = Enabled ? _defaultMode : CombatPrototypeMapInventoryRecipeFilterMode.All;
            _label = settings.RecipeFilterLabel.ToString();
            _allLabel = settings.AllRecipesLabel.ToString();
            _craftLabel = settings.CraftRecipesLabel.ToString();
            _repairLabel = settings.RepairRecipesLabel.ToString();
            _upgradeLabel = settings.UpgradeRecipesLabel.ToString();
            UpdateLabel();
        }

        // Preferences restore an already validated selection before the first snapshot; no GUI request is queued.
        public void Restore(CombatPrototypeMapInventoryRecipeFilterMode mode)
        {
            if (!Enabled) return;
            ClearPending();
            Mode = mode;
            UpdateLabel();
        }

        public void QueueToggle() { if (Enabled) _togglePending = true; }
        public void ClearPending() { _togglePending = false; }

        // Panel.Show applies a GUI selection once, before reading the original recipe requests.
        public bool ApplyPending()
        {
            if (!_togglePending) return false;
            ClearPending();
            Mode = Mode == CombatPrototypeMapInventoryRecipeFilterMode.All ? CombatPrototypeMapInventoryRecipeFilterMode.Craft :
                Mode == CombatPrototypeMapInventoryRecipeFilterMode.Craft ? CombatPrototypeMapInventoryRecipeFilterMode.Repair :
                Mode == CombatPrototypeMapInventoryRecipeFilterMode.Repair ? CombatPrototypeMapInventoryRecipeFilterMode.Upgrade :
                CombatPrototypeMapInventoryRecipeFilterMode.All;
            UpdateLabel();
            return true;
        }

        public bool ResetDisplay()
        {
            ClearPending();
            var mode = Enabled ? _defaultMode : CombatPrototypeMapInventoryRecipeFilterMode.All;
            if (Mode == mode) return false;
            Mode = mode;
            UpdateLabel();
            return true;
        }

        private void UpdateLabel()
        {
            var value = Mode == CombatPrototypeMapInventoryRecipeFilterMode.Craft ? _craftLabel :
                Mode == CombatPrototypeMapInventoryRecipeFilterMode.Repair ? _repairLabel :
                Mode == CombatPrototypeMapInventoryRecipeFilterMode.Upgrade ? _upgradeLabel : _allLabel;
            Text = _label + ": " + value;
        }

        public void Reset()
        {
            ClearPending();
            Enabled = false;
            Mode = _defaultMode = CombatPrototypeMapInventoryRecipeFilterMode.All;
            _label = _allLabel = _craftLabel = _repairLabel = _upgradeLabel = Text = string.Empty;
        }
    }
}
