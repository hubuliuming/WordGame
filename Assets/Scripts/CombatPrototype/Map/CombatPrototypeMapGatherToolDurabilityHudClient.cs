using System.Globalization;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    // Read-only projection of the owner tool snapshot already validated by the HUD.
    internal sealed class CombatPrototypeMapGatherToolDurabilityHudClient
    {
        internal readonly struct ToolStatus
        {
            public readonly Color TextColor;
            public readonly string WarningLabel;
            public readonly string Detail;
            public readonly string RepairHint;

            public ToolStatus(Color color, string warningLabel, string detail, string repairHint)
            {
                TextColor = color; WarningLabel = warningLabel; Detail = detail; RepairHint = repairHint;
            }

            public static ToolStatus Plain => new ToolStatus(Color.white, string.Empty, string.Empty, string.Empty);
        }

        private sealed class ToolCache
        {
            private int _durability = int.MinValue, _level = -1;
            public ToolStatus Status = ToolStatus.Plain;

            public void Capture(CombatPrototypeMapGatherToolDurabilityHudClient owner,
                CombatPrototypeMapGatherToolDefinition definition, int durability, int level, string repairKey, string recraftKey)
            {
                if (_durability == durability && _level == level) return;
                _durability = durability; _level = level;
                Status = owner.Project(definition, durability, repairKey, recraftKey);
            }

            public void Reset() { _durability = int.MinValue; _level = -1; Status = ToolStatus.Plain; }
        }

        private readonly ToolCache _axe = new ToolCache(), _pickaxe = new ToolCache();
        private CombatPrototypeMapGatherToolDurabilityHudSettings _settings;
        private bool _repairEnabled;
        private Color _warningColor, _criticalColor, _brokenColor;
        private string _warning, _critical, _broken, _uses, _repair, _notOwned, _recraft;
        public bool Enabled { get; private set; }
        public ToolStatus Axe => _axe.Status;
        public ToolStatus Pickaxe => _pickaxe.Status;

        public void Configure(CombatPrototypeMapGatherToolDurabilityHudSettings settings,
            CombatPrototypeMapGatherToolSettings tools, string notOwned, string recraft)
        {
            Reset();
            _settings = settings;
            Enabled = settings.Enabled != 0 && tools.Enabled != 0;
            _repairEnabled = tools.RepairEnabled != 0;
            _warningColor = new Color(settings.WarningColor.x, settings.WarningColor.y, settings.WarningColor.z, 1f);
            _criticalColor = new Color(settings.CriticalColor.x, settings.CriticalColor.y, settings.CriticalColor.z, 1f);
            _brokenColor = new Color(settings.BrokenColor.x, settings.BrokenColor.y, settings.BrokenColor.z, 1f);
            _warning = settings.WarningLabel.ToString(); _critical = settings.CriticalLabel.ToString();
            _broken = settings.BrokenLabel.ToString(); _uses = settings.RemainingUsesLabel.ToString();
            _repair = settings.RepairHintLabel.ToString(); _notOwned = notOwned; _recraft = recraft;
        }

        public void Capture(CombatPrototypeMapGatherToolDefinition axe, int axeDurability, int axeLevel,
            CombatPrototypeMapGatherToolDefinition pickaxe, int pickaxeDurability, int pickaxeLevel)
        {
            _axe.Capture(this, axe, axeDurability, axeLevel, "3", "1");
            _pickaxe.Capture(this, pickaxe, pickaxeDurability, pickaxeLevel, "4", "2");
        }

        public ToolStatus ForKind(CombatPrototypeMapGatherToolKind kind) =>
            kind == CombatPrototypeMapGatherToolKind.Axe ? Axe : Pickaxe;

        private ToolStatus Project(CombatPrototypeMapGatherToolDefinition definition, int durability, string repairKey, string recraftKey)
        {
            if (!Enabled) return ToolStatus.Plain;
            if (durability < 0) return new ToolStatus(Color.white, string.Empty, _notOwned, string.Empty);
            var ratio = (double)durability / definition.MaxDurability;
            var isBroken = durability < definition.DurabilityCostPerCompletion;
            var label = isBroken ? _broken : ratio <= _settings.CriticalRatio ? _critical :
                ratio <= _settings.WarningRatio ? _warning : string.Empty;
            var color = isBroken ? _brokenColor : ratio <= _settings.CriticalRatio ? _criticalColor :
                ratio <= _settings.WarningRatio ? _warningColor : Color.white;
            var detail = _uses + " " + (durability / definition.DurabilityCostPerCompletion).ToString(CultureInfo.InvariantCulture);
            var hint = label.Length != 0 && _repairEnabled ? repairKey + ": " + _repair : string.Empty;
            if (hint.Length != 0) detail += "  [" + hint + "]";
            else if (isBroken) detail += "  [" + recraftKey + ": " + _recraft + "]";
            return new ToolStatus(color, label, detail, hint);
        }

        public void Reset()
        {
            Enabled = _repairEnabled = false; _settings = default;
            _axe.Reset(); _pickaxe.Reset();
            _warningColor = _criticalColor = _brokenColor = Color.white;
            _warning = _critical = _broken = _uses = _repair = _notOwned = _recraft = string.Empty;
        }
    }
}
