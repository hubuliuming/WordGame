using System;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapGatherToolUpgradeFeedbackClient
    {
        private string _axeName, _pickaxeName, _success, _rejected, _failed, _maxLevel, _mapId, _text = string.Empty;
        private float _seconds;
        private uint _sequence;
        private bool _observed;
        private double _until;
        public string Feedback => Time.unscaledTimeAsDouble < _until ? _text : string.Empty;

        public void Configure(CombatPrototypeMapGatherToolUpgradeSettings settings,
            CombatPrototypeMapGatherToolDefinition axe, CombatPrototypeMapGatherToolDefinition pickaxe, string mapId)
        {
            Reset();
            _seconds = settings.FeedbackSeconds;
            _axeName = axe.DisplayName.ToString(); _pickaxeName = pickaxe.DisplayName.ToString();
            _success = settings.SuccessLabel.ToString(); _rejected = settings.RejectedLabel.ToString();
            _failed = settings.FailureLabel.ToString(); _maxLevel = settings.MaxLevelLabel.ToString();
            _mapId = mapId;
        }

        public void Observe(CombatPrototypeMapToolUpgradeFeedback feedback)
        {
            try
            {
                if (feedback.Result > CombatPrototypeMapToolUpgradeResult.Failed ||
                    (feedback.Result == CombatPrototypeMapToolUpgradeResult.None ?
                        feedback.Kind != CombatPrototypeMapGatherToolKind.None || feedback.Sequence != 0 :
                        feedback.Kind != CombatPrototypeMapGatherToolKind.Axe && feedback.Kind != CombatPrototypeMapGatherToolKind.Pickaxe))
                    throw new InvalidOperationException("Invalid owner tool upgrade feedback.");
                if (!_observed) { _observed = true; _sequence = feedback.Sequence; return; }
                if (_sequence == feedback.Sequence) return;
                _sequence = feedback.Sequence;
                if (feedback.Result == CombatPrototypeMapToolUpgradeResult.None) { _until = 0d; return; }
                var name = feedback.Kind == CombatPrototypeMapGatherToolKind.Axe ? _axeName : _pickaxeName;
                var label = feedback.Result == CombatPrototypeMapToolUpgradeResult.Success ? _success :
                    feedback.Result == CombatPrototypeMapToolUpgradeResult.Failed ? _failed :
                    feedback.Result == CombatPrototypeMapToolUpgradeResult.MaxLevel ? _maxLevel : _rejected;
                _text = label + "  " + name;
                _until = Time.unscaledTimeAsDouble + _seconds;
            }
            catch (Exception exception)
            {
                _until = 0d; _text = string.Empty;
                Debug.LogError("[CombatPrototype.ToolUpgrade] Feedback failed; stage=ReadSnapshot, map=" + _mapId +
                    ", sequence=" + feedback.Sequence + ", tool=" + feedback.Kind + ", result=" + feedback.Result + ". " + exception);
            }
        }

        public void Reset()
        {
            _axeName = _pickaxeName = _success = _rejected = _failed = _maxLevel = _mapId = _text = string.Empty;
            _seconds = 0f; _sequence = 0; _observed = false; _until = 0d;
        }
    }
}
