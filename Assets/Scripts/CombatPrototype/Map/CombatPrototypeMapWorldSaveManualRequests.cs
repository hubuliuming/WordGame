using System;
using System.Collections.Generic;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapWorldSaveManualRequests
    {
        private struct Feedback
        {
            internal uint Sequence;
            internal CombatPrototypeMapWorldSaveManualResult Result;
        }

        private readonly List<Entity> _accepted = new List<Entity>();
        private readonly HashSet<Entity> _present = new HashSet<Entity>();
        private readonly List<Entity> _removed = new List<Entity>();
        private readonly Dictionary<Entity, Feedback> _feedback = new Dictionary<Entity, Feedback>();
        private double _nextManualAt;

        internal bool Collect(EntityManager manager, EntityQuery connections, FixedString64Bytes mapId,
            CombatPrototypeMapResourcePersistenceSettings settings, CombatPrototypeMapResourceRestorePhase phase, double time)
        {
            _accepted.Clear();
            _present.Clear();
            using var clients = connections.ToEntityArray(Allocator.Temp);
            foreach (var connection in clients)
            {
                var player = Entity.Null;
                var networkId = 0;
                var owned = false;
                var stage = "ReadConnection";
                try
                {
                    networkId = manager.GetComponentData<NetworkId>(connection).Value;
                    if (manager.GetComponentData<NetworkStreamConnection>(connection).CurrentState != ConnectionState.State.Connected)
                        continue;
                    player = manager.GetComponentData<CommandTarget>(connection).targetEntity;
                    if (!manager.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                        !manager.HasComponent<Simulate>(player) || !manager.IsComponentEnabled<Simulate>(player)) continue;
                    stage = "ReadPlayer";
                    if (manager.GetComponentData<GhostOwner>(player).NetworkId != networkId) continue;
                    owned = true;
                    _present.Add(player);
                    if (!manager.GetComponentData<CombatPrototypePlayerInput>(player).SaveWorld.IsSet) continue;
                    stage = "ValidateRequest";
                    var result = manager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0 ?
                        CombatPrototypeMapWorldSaveManualResult.Unavailable :
                        settings.Enabled == 0 || settings.ManualSaveEnabled == 0 ? CombatPrototypeMapWorldSaveManualResult.Disabled :
                        phase != CombatPrototypeMapResourceRestorePhase.Ready ? CombatPrototypeMapWorldSaveManualResult.Unavailable :
                        time < _nextManualAt ? CombatPrototypeMapWorldSaveManualResult.Cooldown : CombatPrototypeMapWorldSaveManualResult.None;
                    if (result != CombatPrototypeMapWorldSaveManualResult.None) SetResult(player, result);
                    else _accepted.Add(player);
                }
                catch (Exception exception)
                {
                    if (owned) SetResult(player, CombatPrototypeMapWorldSaveManualResult.Failed);
                    Debug.LogError("[CombatPrototype.Map] World save request failed; stage=" + stage + ", map=" +
                        mapId + ", NetworkId=" + networkId + ", player=" + player + ". " + exception);
                }
            }
            _removed.Clear();
            foreach (var entry in _feedback) if (!_present.Contains(entry.Key)) _removed.Add(entry.Key);
            foreach (var player in _removed) _feedback.Remove(player);
            if (_accepted.Count == 0) return false;
            // Reserve one global attempt after collecting the whole tick, including attempts that later fail.
            _nextManualAt = time + settings.ManualSaveCooldownSeconds;
            return true;
        }

        internal void Complete(bool saved)
        {
            foreach (var player in _accepted)
                SetResult(player, saved ? CombatPrototypeMapWorldSaveManualResult.Success : CombatPrototypeMapWorldSaveManualResult.Failed);
            _accepted.Clear();
        }

        private void SetResult(Entity player, CombatPrototypeMapWorldSaveManualResult result)
        {
            _feedback.TryGetValue(player, out var previous);
            _feedback[player] = new Feedback { Sequence = unchecked(previous.Sequence + 1), Result = result };
        }

        internal void ApplyFeedback(Entity player, ref CombatPrototypeMapWorldSaveHudState frame)
        {
            if (!_feedback.TryGetValue(player, out var feedback)) return;
            frame.ManualSequence = feedback.Sequence;
            frame.ManualResult = feedback.Result;
        }

        internal void Reset()
        {
            _accepted.Clear();
            _present.Clear();
            _removed.Clear();
            _feedback.Clear();
            _nextManualAt = 0d;
        }
    }
}
