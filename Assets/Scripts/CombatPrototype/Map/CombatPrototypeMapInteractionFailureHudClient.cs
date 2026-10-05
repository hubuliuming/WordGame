using System;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapInteractionFailureHudClient
    {
        private CombatPrototypeMapInteractionFailureHudSettings _settings;
        private string _mapId, _noSpaceLabel, _text = string.Empty;
        private uint _sequence;
        private bool _observed;
        private double _until;
        public Color TextColor { get; private set; } = Color.white;
        public string Feedback => _settings.Enabled != 0 && Time.unscaledTimeAsDouble < _until ? _text : string.Empty;

        public void Configure(CombatPrototypeMapInteractionFailureHudSettings settings, string noSpaceLabel, string mapId)
        {
            Reset();
            _settings = settings;
            _noSpaceLabel = noSpaceLabel;
            _mapId = mapId;
            TextColor = new Color(settings.ErrorColor.x, settings.ErrorColor.y, settings.ErrorColor.z, 1f);
        }

        public void Observe(CombatPrototypeMapInteractionFailureFeedback feedback)
        {
            try
            {
                if (feedback.Result > CombatPrototypeMapInteractionFailureResult.Failed)
                    throw new InvalidOperationException("Invalid owner interaction failure feedback.");
                if (!_observed) { _observed = true; _sequence = feedback.Sequence; return; }
                if (_sequence == feedback.Sequence) return;
                _sequence = feedback.Sequence;
                if (feedback.Result == CombatPrototypeMapInteractionFailureResult.None)
                {
                    _until = 0d;
                    _text = string.Empty;
                    return;
                }
                switch (feedback.Result)
                {
                    case CombatPrototypeMapInteractionFailureResult.AlreadyInteracting: _text = _settings.AlreadyInteractingLabel.ToString(); break;
                    case CombatPrototypeMapInteractionFailureResult.AttackInProgress: _text = _settings.AttackingLabel.ToString(); break;
                    case CombatPrototypeMapInteractionFailureResult.PlayerMoving: _text = _settings.MovingLabel.ToString(); break;
                    case CombatPrototypeMapInteractionFailureResult.NoAvailableTarget: _text = _settings.NoTargetLabel.ToString(); break;
                    case CombatPrototypeMapInteractionFailureResult.NoSpace: _text = _noSpaceLabel; break;
                    case CombatPrototypeMapInteractionFailureResult.TargetUnavailable: _text = _settings.TargetUnavailableLabel.ToString(); break;
                    case CombatPrototypeMapInteractionFailureResult.Failed: _text = _settings.FailedLabel.ToString(); break;
                }
                // Presentation timeout only; resource work timing remains authoritative.
                _until = Time.unscaledTimeAsDouble + _settings.FeedbackSeconds;
            }
            catch (Exception exception)
            {
                _until = 0d;
                _text = string.Empty;
                Debug.LogError("[CombatPrototype.Map] Interaction feedback failed; stage=ReadSnapshot, map=" + _mapId +
                    ", sequence=" + feedback.Sequence + ", result=" + feedback.Result + ". " + exception);
            }
        }

        public void Reset()
        {
            _settings = default;
            _mapId = _noSpaceLabel = _text = string.Empty;
            _sequence = 0;
            _observed = false;
            _until = 0d;
            TextColor = Color.white;
        }
    }
}
