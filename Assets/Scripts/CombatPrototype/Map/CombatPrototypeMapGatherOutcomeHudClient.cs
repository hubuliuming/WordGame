using System;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapGatherOutcomeHudClient
    {
        private CombatPrototypeMapGatherOutcomeHudSettings _settings;
        private string _mapId, _noSpaceLabel, _text = string.Empty;
        private uint _sequence;
        private bool _observed;
        private double _until;
        public byte Kind { get; private set; }
        public CombatPrototypeMapGatherOutcomeResult Result { get; private set; }
        public Color TextColor { get; private set; } = Color.white;
        public string Feedback => _settings.Enabled != 0 && Time.unscaledTimeAsDouble < _until ? _text : string.Empty;

        public void Configure(CombatPrototypeMapGatherOutcomeHudSettings settings, string noSpaceLabel, string mapId)
        {
            Reset();
            _settings = settings;
            _noSpaceLabel = noSpaceLabel;
            _mapId = mapId;
        }

        public void Observe(CombatPrototypeMapGatherOutcomeFeedback feedback)
        {
            try
            {
                if (feedback.Result > CombatPrototypeMapGatherOutcomeResult.Failed ||
                    (feedback.Result == CombatPrototypeMapGatherOutcomeResult.None ? feedback.Kind != 0 :
                        feedback.Kind < (byte)CombatPrototypeMapInteractionKind.Gather || feedback.Kind > (byte)CombatPrototypeMapInteractionKind.Mine))
                    throw new InvalidOperationException("Invalid owner gather outcome snapshot.");
                if (!_observed) { _observed = true; _sequence = feedback.Sequence; return; }
                if (_sequence == feedback.Sequence) return;
                _sequence = feedback.Sequence;
                if (feedback.Result == CombatPrototypeMapGatherOutcomeResult.None)
                {
                    ClearMessage();
                    return;
                }
                Kind = feedback.Kind;
                Result = feedback.Result;
                var color = _settings.InterruptedColor;
                switch (feedback.Result)
                {
                    case CombatPrototypeMapGatherOutcomeResult.Completed:
                        _text = Kind == (byte)CombatPrototypeMapInteractionKind.Gather ? _settings.GatherCompletedLabel.ToString() :
                            Kind == (byte)CombatPrototypeMapInteractionKind.Tree ? _settings.TreeCompletedLabel.ToString() : _settings.MineCompletedLabel.ToString();
                        color = _settings.CompletedColor;
                        break;
                    case CombatPrototypeMapGatherOutcomeResult.PlayerMoving: _text = _settings.MovingLabel.ToString(); break;
                    case CombatPrototypeMapGatherOutcomeResult.AttackInProgress: _text = _settings.AttackingLabel.ToString(); break;
                    case CombatPrototypeMapGatherOutcomeResult.PlayerHit: _text = _settings.HitLabel.ToString(); break;
                    case CombatPrototypeMapGatherOutcomeResult.OutOfRange: _text = _settings.OutOfRangeLabel.ToString(); break;
                    case CombatPrototypeMapGatherOutcomeResult.NoSpace: _text = _noSpaceLabel; color = _settings.FailedColor; break;
                    case CombatPrototypeMapGatherOutcomeResult.Failed: _text = _settings.FailedLabel.ToString(); color = _settings.FailedColor; break;
                }
                TextColor = new Color(color.x, color.y, color.z, 1f);
                // Presentation timeout only; completion and cancellation come from the server.
                _until = Time.unscaledTimeAsDouble + _settings.FeedbackSeconds;
            }
            catch (Exception exception)
            {
                ClearMessage();
                Debug.LogError("[CombatPrototype.Map] Gather outcome feedback failed; stage=ReadSnapshot, map=" + _mapId +
                    ", sequence=" + feedback.Sequence + ", kind=" + feedback.Kind + ", result=" + feedback.Result + ". " + exception);
            }
        }

        private void ClearMessage()
        {
            _until = 0d;
            _text = string.Empty;
            Kind = 0;
            Result = CombatPrototypeMapGatherOutcomeResult.None;
            TextColor = Color.white;
        }

        public void Reset()
        {
            _settings = default;
            _mapId = _noSpaceLabel = string.Empty;
            _sequence = 0;
            _observed = false;
            ClearMessage();
        }
    }
}
