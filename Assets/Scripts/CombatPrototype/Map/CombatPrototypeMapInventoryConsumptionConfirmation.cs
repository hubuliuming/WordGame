using System;
using Unity.Collections;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    // One local B-button confirmation. Captures and decisions never mutate gameplay or saved preferences.
    internal sealed class CombatPrototypeMapInventoryConsumptionConfirmation
    {
        internal enum Operation : byte { None, CraftAxe, CraftPickaxe, RepairAxe, RepairPickaxe, CapacityUpgrade, UpgradeAxe, UpgradePickaxe }

        private readonly struct Candidate
        {
            internal readonly bool Eligible, FavoriteWood, FavoriteStone;
            internal readonly int WoodCost, StoneCost, Wood, Stone, Level, Durability, Maximum;
            internal readonly uint FavoritesRevision;

            internal Candidate(bool eligible, int woodCost, int stoneCost, int wood, int stone, int level, int durability,
                int maximum, CombatPrototypeMapInventoryPanelFavorites favorites)
            {
                Eligible = eligible; WoodCost = woodCost; StoneCost = stoneCost; Wood = wood; Stone = stone;
                Level = level; Durability = durability; Maximum = maximum; FavoritesRevision = favorites.Revision;
                FavoriteWood = woodCost > 0 && favorites.IsFavorite(WoodName);
                FavoriteStone = stoneCost > 0 && favorites.IsFavorite(StoneName);
            }

            internal bool RequiresConfirmation => FavoriteWood || FavoriteStone;
            internal bool Same(in Candidate other) => Eligible == other.Eligible && WoodCost == other.WoodCost &&
                StoneCost == other.StoneCost && Wood == other.Wood && Stone == other.Stone && Level == other.Level &&
                Durability == other.Durability && Maximum == other.Maximum && FavoritesRevision == other.FavoritesRevision &&
                FavoriteWood == other.FavoriteWood && FavoriteStone == other.FavoriteStone;
        }

        private static readonly FixedString64Bytes WoodName = new FixedString64Bytes(Msg.ItemName.木材);
        private static readonly FixedString64Bytes StoneName = new FixedString64Bytes(Msg.ItemName.石材);
        private readonly Candidate[] _candidates = new Candidate[8];
        private Candidate _pendingCandidate;
        private Operation _pendingOperation;
        private string _label, _confirmLabel, _cancelLabel, _woodLabel, _stoneLabel, _prompt = string.Empty;
        private bool _enabled, _decisionPending, _confirmed;
        private uint _decisionRevision;

        public uint Revision { get; private set; }
        public int RowCount => _pendingOperation == Operation.None ? 0 : 1;

        public void Configure(CombatPrototypeMapInventoryPanelSettings settings)
        {
            Reset();
            _enabled = settings.FavoritesConsumptionConfirmationEnabled != 0;
            _label = settings.FavoritesConsumptionConfirmationLabel.ToString();
            _confirmLabel = settings.FavoritesConsumptionConfirmLabel.ToString();
            _cancelLabel = settings.FavoritesConsumptionCancelLabel.ToString();
            _woodLabel = settings.WoodLabel.ToString(); _stoneLabel = settings.StoneLabel.ToString();
        }

        public void Capture(Operation operation, bool eligible, int woodCost, int stoneCost, int wood, int stone, int level,
            int durability, int maximum, CombatPrototypeMapInventoryPanelFavorites favorites)
        {
            var candidate = new Candidate(eligible, woodCost, stoneCost, wood, stone, level, durability, maximum, favorites);
            _candidates[(int)operation] = candidate;
            if (_pendingOperation == operation && !_pendingCandidate.Same(candidate)) ClearPending();
        }

        // Called only from the original validated ReadInput; ordinary button requests keep their original path.
        public bool ReadRequest(Operation operation, bool requested)
        {
            if (!requested) return false;
            var candidate = _candidates[(int)operation];
            ClearPending();
            if (!_enabled || !candidate.RequiresConfirmation) return true;
            if (!candidate.Eligible) return false;
            _pendingOperation = operation; _pendingCandidate = candidate;
            var materials = candidate.FavoriteWood ? _woodLabel + " " + candidate.WoodCost : string.Empty;
            if (candidate.FavoriteStone) materials += (materials.Length != 0 ? ", " : string.Empty) + _stoneLabel + " " + candidate.StoneCost;
            _prompt = _label + " " + materials;
            Revision++;
            return false;
        }

        public Operation ReadDecision()
        {
            if (!_decisionPending) return Operation.None;
            var operation = _pendingOperation;
            var candidate = _candidates[(int)operation];
            var accepted = _confirmed && _decisionRevision == Revision && operation != Operation.None &&
                _enabled && candidate.Eligible && candidate.RequiresConfirmation && _pendingCandidate.Same(candidate);
            ClearPending();
            return accepted ? operation : Operation.None;
        }

        public void DrawPrompt(Operation operation, float width, ref float y, float rowHeight, GUIStyle labelStyle)
        {
            if (_pendingOperation != operation) return;
            GUI.Label(new Rect(0f, y, width, rowHeight), _prompt, labelStyle);
            y += rowHeight;
        }

        public bool DrawButtons(Operation operation, float width, float y, float rowHeight, GUIStyle buttonStyle, bool mousePressAccepted)
        {
            if (_pendingOperation != operation) return false;
            var half = (width - 4f) * 0.5f;
            if (GUI.Button(new Rect(0f, y, half, rowHeight - 4f), _confirmLabel, buttonStyle) && mousePressAccepted) QueueDecision(true);
            if (GUI.Button(new Rect(half + 4f, y, half, rowHeight - 4f), _cancelLabel, buttonStyle) && mousePressAccepted) QueueDecision(false);
            return true;
        }

        private void QueueDecision(bool confirmed)
        {
            if (_decisionPending) return;
            _decisionPending = true; _confirmed = confirmed; _decisionRevision = Revision;
        }

        public void ClearPending()
        {
            if (_pendingOperation != Operation.None) Revision++;
            _pendingOperation = Operation.None; _pendingCandidate = default; _prompt = string.Empty;
            _decisionPending = _confirmed = false; _decisionRevision = 0;
        }

        public void Reset()
        {
            ClearPending(); Array.Clear(_candidates, 0, _candidates.Length);
            _enabled = false; Revision = 0;
            _label = _confirmLabel = _cancelLabel = _woodLabel = _stoneLabel = string.Empty;
        }
    }
}
