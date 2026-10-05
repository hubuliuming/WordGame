using System;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapGatherOutcomeFeedbackUtility
    {
        internal static void Write(EntityManager manager, FixedString64Bytes mapId, Entity player, int networkId,
            CombatPrototypeMapInteractionKind kind, CombatPrototypeMapGatherOutcomeResult result) =>
            WriteInternal(manager, mapId, player, networkId, kind, result, null);

        internal static void WriteCancellation(EntityManager manager, FixedString64Bytes mapId, Entity player, int networkId,
            CombatPrototypeMapInteractionKind kind, string reason) =>
            WriteInternal(manager, mapId, player, networkId, kind, CombatPrototypeMapGatherOutcomeResult.None, reason);

        private static void WriteInternal(EntityManager manager, FixedString64Bytes mapId, Entity player, int networkId,
            CombatPrototypeMapInteractionKind kind, CombatPrototypeMapGatherOutcomeResult result, string reason)
        {
            // Feedback is written only after the caller's commit/cancel; errors never enter gameplay rollback.
            try
            {
                if (reason != null)
                {
                    switch (reason)
                    {
                        case "ReservationFailed":
                        case "CollectorOffline":
                        case "CommandTargetOwnerMismatch":
                        case "PlayerDead": return;
                        case "PlayerMoving": result = CombatPrototypeMapGatherOutcomeResult.PlayerMoving; break;
                        case "AttackInProgress": result = CombatPrototypeMapGatherOutcomeResult.AttackInProgress; break;
                        case "PlayerHit": result = CombatPrototypeMapGatherOutcomeResult.PlayerHit; break;
                        case "OutOfRange": result = CombatPrototypeMapGatherOutcomeResult.OutOfRange; break;
                        case "InventoryAlreadyOverCapacity":
                        case "MaterialTotalCapacityExceeded":
                        case "MaterialItemCapacityExceeded": result = CombatPrototypeMapGatherOutcomeResult.NoSpace; break;
                        case "InvalidMoveInput":
                        case "ProcessingFailed":
                        case "SettlementFailed":
                        case "DropOrSaveOrCommitFailed": result = CombatPrototypeMapGatherOutcomeResult.Failed; break;
                        default: throw new InvalidOperationException("Unsupported resource cancellation reason: " + reason);
                    }
                }
                if (manager.GetComponentData<GhostOwner>(player).NetworkId != networkId ||
                    manager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0) return;
                var feedback = manager.GetComponentData<CombatPrototypeMapGatherOutcomeFeedback>(player);
                if (result == CombatPrototypeMapGatherOutcomeResult.None && feedback.Result == result) return;
                unchecked { feedback.Sequence++; }
                feedback.Kind = result == CombatPrototypeMapGatherOutcomeResult.None ? (byte)0 : (byte)kind;
                feedback.Result = result;
                manager.SetComponentData(player, feedback);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Gather outcome feedback failed; stage=WriteFeedback, map=" + mapId +
                    ", NetworkId=" + networkId + ", player=" + player + ", kind=" + kind + ", result=" + result +
                    ", reason=" + reason + ". " + exception);
            }
        }
    }
}
