using System;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapInventoryCapacityUpgradeFeedbackClient
    {
        private string _success, _rejected, _failed, _maxLevel, _mapId, _text = string.Empty;
        private float _seconds;
        private uint _sequence;
        private bool _observed;
        private double _until;
        public string Feedback => Time.unscaledTimeAsDouble < _until ? _text : string.Empty;

        public void Configure(CombatPrototypeMapInventoryCapacityUpgradeSettings settings, string mapId)
        {
            Reset();
            _seconds = settings.FeedbackSeconds;
            _success = settings.SuccessLabel.ToString();
            _rejected = settings.RejectedLabel.ToString();
            _failed = settings.FailureLabel.ToString();
            _maxLevel = settings.MaxLevelLabel.ToString();
            _mapId = mapId;
        }

        public void Observe(CombatPrototypeMapInventoryCapacityUpgradeFeedback feedback)
        {
            try
            {
                if (feedback.Result > CombatPrototypeMapInventoryCapacityUpgradeResult.Failed ||
                    (feedback.Result == CombatPrototypeMapInventoryCapacityUpgradeResult.None && feedback.Sequence != 0))
                    throw new InvalidOperationException("Invalid owner capacity upgrade feedback.");
                if (!_observed) { _observed = true; _sequence = feedback.Sequence; return; }
                if (_sequence == feedback.Sequence) return;
                _sequence = feedback.Sequence;
                if (feedback.Result == CombatPrototypeMapInventoryCapacityUpgradeResult.None) { _until = 0d; return; }
                _text = feedback.Result == CombatPrototypeMapInventoryCapacityUpgradeResult.Success ? _success :
                    feedback.Result == CombatPrototypeMapInventoryCapacityUpgradeResult.Failed ? _failed :
                    feedback.Result == CombatPrototypeMapInventoryCapacityUpgradeResult.MaxLevel ? _maxLevel : _rejected;
                _until = Time.unscaledTimeAsDouble + _seconds;
            }
            catch (Exception exception)
            {
                _until = 0d;
                _text = string.Empty;
                Debug.LogError("[CombatPrototype.CapacityUpgrade] Feedback failed; stage=ReadSnapshot, map=" + _mapId +
                    ", sequence=" + feedback.Sequence + ", result=" + feedback.Result + ". " + exception);
            }
        }

        public void Reset()
        {
            _success = _rejected = _failed = _maxLevel = _mapId = _text = string.Empty;
            _seconds = 0f;
            _sequence = 0;
            _observed = false;
            _until = 0d;
        }
    }
}
