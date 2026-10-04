using System;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapGatherToolRepairFeedbackClient
    {
        private string _axeName, _pickaxeName, _full, _mapId, _text = string.Empty;
        private float _seconds;
        private uint _sequence;
        private bool _observed;
        private double _until;
        public CombatPrototypeMapGatherToolKind Kind { get; private set; }
        public string Feedback => Time.unscaledTimeAsDouble < _until ? _text : string.Empty;

        public void Configure(CombatPrototypeMapGatherToolSettings settings, CombatPrototypeMapInventoryPanelSettings panel,
            CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe, string mapId)
        {
            Reset();
            _seconds = settings.RepairFeedbackSeconds;
            _axeName = axe.DisplayName.ToString();
            _pickaxeName = pickaxe.DisplayName.ToString();
            _full = panel.FullDurabilityLabel.ToString();
            _mapId = mapId;
        }

        public void Observe(CombatPrototypeMapToolRepairFeedback feedback)
        {
            try
            {
                if (feedback.Result > CombatPrototypeMapToolRepairResult.Failed ||
                    (feedback.Result == CombatPrototypeMapToolRepairResult.None ? feedback.Kind != CombatPrototypeMapGatherToolKind.None :
                        feedback.Kind != CombatPrototypeMapGatherToolKind.Axe && feedback.Kind != CombatPrototypeMapGatherToolKind.Pickaxe))
                    throw new InvalidOperationException("Invalid owner tool repair feedback.");
                if (!_observed) { _observed = true; _sequence = feedback.Sequence; return; }
                if (_sequence == feedback.Sequence) return;
                _sequence = feedback.Sequence;
                Kind = feedback.Kind;
                if (feedback.Result == CombatPrototypeMapToolRepairResult.None) { _until = 0d; return; }
                var name = feedback.Kind == CombatPrototypeMapGatherToolKind.Axe ? _axeName : _pickaxeName;
                switch (feedback.Result)
                {
                    case CombatPrototypeMapToolRepairResult.Success: _text = "Repaired " + name; break;
                    case CombatPrototypeMapToolRepairResult.Disabled: _text = "Tool repair disabled"; break;
                    case CombatPrototypeMapToolRepairResult.NotOwned: _text = name + " not owned"; break;
                    case CombatPrototypeMapToolRepairResult.AlreadyFull: _text = name + "  " + _full; break;
                    case CombatPrototypeMapToolRepairResult.InsufficientMaterials: _text = "Need wood and stone"; break;
                    case CombatPrototypeMapToolRepairResult.Busy: _text = "Finish gathering first"; break;
                    case CombatPrototypeMapToolRepairResult.ExistingOperationHasPriority: _text = "Another action has priority"; break;
                    case CombatPrototypeMapToolRepairResult.PlayerUnavailable: _text = "Stand still to repair"; break;
                    case CombatPrototypeMapToolRepairResult.Failed: _text = "Repair failed"; break;
                }
                _until = Time.unscaledTimeAsDouble + _seconds;
            }
            catch (Exception exception)
            {
                _until = 0d;
                _text = string.Empty;
                Debug.LogError("[CombatPrototype.ToolRepair] Feedback failed; stage=ReadSnapshot, map=" + _mapId +
                    ", sequence=" + feedback.Sequence + ", tool=" + feedback.Kind + ", result=" + feedback.Result + ". " + exception);
            }
        }

        public void Reset()
        {
            _axeName = _pickaxeName = _full = _mapId = _text = string.Empty;
            _seconds = 0f;
            _sequence = 0;
            _observed = false;
            _until = 0d;
            Kind = CombatPrototypeMapGatherToolKind.None;
        }
    }
}
