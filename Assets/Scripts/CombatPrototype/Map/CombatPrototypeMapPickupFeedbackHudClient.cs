using System;
using System.Globalization;
using Unity.Collections;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapPickupFeedbackHudClient
    {
        private readonly FixedString64Bytes _appleId = new FixedString64Bytes(CombatPrototypeMapYieldItemResolver.VitalityAppleId);
        private readonly FixedString64Bytes _woodId = new FixedString64Bytes(CombatPrototypeMapYieldItemResolver.WoodId);
        private readonly FixedString64Bytes _stoneId = new FixedString64Bytes(CombatPrototypeMapYieldItemResolver.StoneId);
        private CombatPrototypeMapPickupFeedbackHudSettings _settings;
        private string _mapId, _appleLabel, _woodLabel, _stoneLabel, _noSpaceLabel, _text = string.Empty;
        private uint _sequence;
        private bool _observed;
        private double _until;
        public CombatPrototypeMapPickupResult Result { get; private set; }
        public Color TextColor { get; private set; } = Color.white;
        public string Feedback => _settings.Enabled != 0 && Time.unscaledTimeAsDouble < _until ? _text : string.Empty;

        public void Configure(CombatPrototypeMapPickupFeedbackHudSettings settings, CombatPrototypeMapPickupHudSettings pickup, string mapId)
        {
            Reset();
            _settings = settings;
            _mapId = mapId;
            _appleLabel = pickup.AppleLabel.ToString();
            _woodLabel = pickup.WoodLabel.ToString();
            _stoneLabel = pickup.StoneLabel.ToString();
            _noSpaceLabel = pickup.NoSpaceLabel.ToString();
        }

        public void Observe(CombatPrototypeMapPickupFeedback feedback)
        {
            try
            {
                var pickedUp = feedback.Result == CombatPrototypeMapPickupResult.PickedUp;
                if (feedback.Result > CombatPrototypeMapPickupResult.Failed ||
                    (pickedUp ? feedback.Quantity <= 0 ||
                        (!feedback.ItemId.Equals(_appleId) && !feedback.ItemId.Equals(_woodId) && !feedback.ItemId.Equals(_stoneId)) :
                        feedback.Quantity != 0 || feedback.ItemId.Length != 0))
                    throw new InvalidOperationException("Invalid owner pickup feedback snapshot.");
                if (!_observed) { _observed = true; _sequence = feedback.Sequence; return; }
                if (_sequence == feedback.Sequence) return;
                _sequence = feedback.Sequence;
                if (feedback.Result == CombatPrototypeMapPickupResult.None) { ClearMessage(); return; }
                Result = feedback.Result;
                var color = _settings.FailureColor;
                switch (feedback.Result)
                {
                    case CombatPrototypeMapPickupResult.PickedUp:
                        var label = feedback.ItemId.Equals(_appleId) ? _appleLabel : feedback.ItemId.Equals(_woodId) ? _woodLabel : _stoneLabel;
                        _text = _settings.SuccessLabel.ToString() + "  " + label + " ×" + feedback.Quantity.ToString(CultureInfo.InvariantCulture);
                        color = _settings.SuccessColor;
                        break;
                    case CombatPrototypeMapPickupResult.PlayerMoving: _text = _settings.MovingLabel.ToString(); break;
                    case CombatPrototypeMapPickupResult.AttackInProgress: _text = _settings.AttackingLabel.ToString(); break;
                    case CombatPrototypeMapPickupResult.NoLandedTarget: _text = _settings.NoTargetLabel.ToString(); break;
                    case CombatPrototypeMapPickupResult.NoSpace: _text = _noSpaceLabel; break;
                    case CombatPrototypeMapPickupResult.Failed: _text = _settings.FailedLabel.ToString(); break;
                }
                TextColor = new Color(color.x, color.y, color.z, 1f);
                // Client time limits presentation only; the server supplies the actual pickup result.
                _until = Time.unscaledTimeAsDouble + _settings.FeedbackSeconds;
            }
            catch (Exception exception)
            {
                ClearMessage();
                Debug.LogError("[CombatPrototype.Map] Pickup feedback failed; stage=ReadSnapshot, map=" + _mapId +
                    ", sequence=" + feedback.Sequence + ", result=" + feedback.Result + ", itemId=" + feedback.ItemId +
                    ", quantity=" + feedback.Quantity + ". " + exception);
            }
        }

        private void ClearMessage()
        {
            _until = 0d;
            _text = string.Empty;
            Result = CombatPrototypeMapPickupResult.None;
            TextColor = Color.white;
        }

        public void Reset()
        {
            _settings = default;
            _mapId = _appleLabel = _woodLabel = _stoneLabel = _noSpaceLabel = string.Empty;
            _sequence = 0;
            _observed = false;
            ClearMessage();
        }
    }
}
