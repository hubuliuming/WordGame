using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal readonly struct CombatPrototypeMapInventoryDropRequest
    {
        public readonly CombatPrototypeMapInventoryDropKind Kind;
        public readonly CombatPrototypeMapInventoryDropMode Mode;
        public CombatPrototypeMapInventoryDropRequest(CombatPrototypeMapInventoryDropKind kind, CombatPrototypeMapInventoryDropMode mode)
        { Kind = kind; Mode = mode; }
    }

    // Local button/request/feedback state only; gameplay and persistence remain on the server.
    internal sealed class CombatPrototypeMapInventoryDropClient
    {
        private readonly List<CombatPrototypeMapInventoryDropDefinition> _definitions = new List<CombatPrototypeMapInventoryDropDefinition>(3);
        private CombatPrototypeMapInventoryDropSettings _settings;
        private CombatPrototypeMapInventoryDropRequest _pending;
        private string _single, _all, _unavailable, _success, _rejected, _failure, _feedback;
        private string _apple, _wood, _stone;
        private uint _sequence;
        private bool _observed;
        private double _feedbackUntil;

        public string Feedback => Time.unscaledTimeAsDouble < _feedbackUntil ? _feedback : string.Empty;

        public void Configure(CombatPrototypeMapInventoryDropSettings settings,
            DynamicBuffer<CombatPrototypeMapInventoryDropDefinition> definitions, CombatPrototypeMapInventoryPanelSettings panel)
        {
            Reset();
            _settings = settings;
            foreach (var definition in definitions) _definitions.Add(definition);
            _single = settings.DropLabel + " x" + settings.SingleDropQuantity;
            _all = settings.DropAllLabel.ToString();
            _unavailable = settings.UnavailableLabel.ToString();
            _success = settings.SuccessLabel.ToString();
            _rejected = settings.RejectedLabel.ToString();
            _failure = settings.FailureLabel.ToString();
            _apple = panel.AppleLabel.ToString();
            _wood = panel.WoodLabel.ToString();
            _stone = panel.StoneLabel.ToString();
        }

        public void Observe(CombatPrototypeMapInventoryDropFeedback feedback)
        {
            if ((byte)feedback.Result > (byte)CombatPrototypeMapInventoryDropResult.Failed ||
                (byte)feedback.Kind > (byte)CombatPrototypeMapInventoryDropKind.Stone || feedback.Quantity < 0 ||
                (feedback.Result == CombatPrototypeMapInventoryDropResult.Success &&
                 (feedback.Kind == CombatPrototypeMapInventoryDropKind.None || feedback.Quantity <= 0)))
                throw new InvalidOperationException("Invalid owner inventory drop feedback: sequence=" + feedback.Sequence + ", result=" + feedback.Result);
            if (!_observed) { _observed = true; _sequence = feedback.Sequence; return; }
            if (feedback.Sequence == _sequence) return;
            _sequence = feedback.Sequence;
            if (feedback.Result == CombatPrototypeMapInventoryDropResult.None) return;
            var label = feedback.Result == CombatPrototypeMapInventoryDropResult.Success ? _success :
                feedback.Result == CombatPrototypeMapInventoryDropResult.Rejected ? _rejected : _failure;
            _feedback = feedback.Result == CombatPrototypeMapInventoryDropResult.Success ?
                label + ": " + Name(feedback.Kind) + " x" + feedback.Quantity : label;
            _feedbackUntil = Time.unscaledTimeAsDouble + _settings.FeedbackSeconds;
        }

        private string Name(CombatPrototypeMapInventoryDropKind kind) => kind == CombatPrototypeMapInventoryDropKind.Apple ? _apple :
            kind == CombatPrototypeMapInventoryDropKind.Wood ? _wood : _stone;

        public void DrawRow(float width, float y, float height, GUIStyle labelStyle, GUIStyle buttonStyle,
            CombatPrototypeMapInventoryPanelSnapshot.Row row, bool inventoryValid, bool mousePressAccepted)
        {
            var kind = CombatPrototypeMapInventoryDropKind.None;
            foreach (var definition in _definitions)
                if (definition.ItemName.Equals(row.Name)) { kind = definition.Kind; break; }
            if (_settings.Enabled == 0 || kind == CombatPrototypeMapInventoryDropKind.None)
            {
                GUI.Label(new Rect(0f, y, width, height), _unavailable, labelStyle);
                return;
            }
            var oldEnabled = GUI.enabled;
            try
            {
                GUI.enabled = oldEnabled && inventoryValid && row.Quantity >= _settings.SingleDropQuantity;
                if (GUI.Button(new Rect(0f, y, width * 0.5f - 4f, height - 4f), _single, buttonStyle) && mousePressAccepted)
                    Queue(kind, CombatPrototypeMapInventoryDropMode.Single);
                GUI.enabled = oldEnabled && inventoryValid && row.Quantity > 0 && _settings.AllowDropAll != 0;
                if (GUI.Button(new Rect(width * 0.5f + 4f, y, width * 0.5f - 4f, height - 4f), _all, buttonStyle) && mousePressAccepted)
                    Queue(kind, CombatPrototypeMapInventoryDropMode.All);
            }
            finally { GUI.enabled = oldEnabled; }
        }

        private void Queue(CombatPrototypeMapInventoryDropKind kind, CombatPrototypeMapInventoryDropMode mode)
        {
            if (_pending.Mode == CombatPrototypeMapInventoryDropMode.None)
                _pending = new CombatPrototypeMapInventoryDropRequest(kind, mode);
        }

        public CombatPrototypeMapInventoryDropRequest ReadRequest()
        {
            var request = _pending;
            ClearPending();
            return request;
        }

        public void ClearPending() { _pending = default; }

        public void Reset()
        {
            ClearPending();
            _definitions.Clear();
            _observed = false;
            _sequence = 0;
            _feedbackUntil = 0d;
            _feedback = string.Empty;
        }
    }
}
