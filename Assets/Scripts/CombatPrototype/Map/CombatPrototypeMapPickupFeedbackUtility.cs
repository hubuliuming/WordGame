using System;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapPickupFeedbackUtility
    {
        internal static void WriteSuccess(EntityManager manager, FixedString64Bytes mapId, Entity player, int networkId,
            int dropId, FixedString64Bytes itemId, int quantity) =>
            WriteInternal(manager, mapId, player, networkId, dropId, itemId, quantity, CombatPrototypeMapPickupResult.PickedUp, null);

        internal static void WriteRejection(EntityManager manager, FixedString64Bytes mapId, Entity player, int networkId,
            int dropId, FixedString64Bytes itemId, string reason) =>
            WriteInternal(manager, mapId, player, networkId, dropId, itemId, 0, CombatPrototypeMapPickupResult.None, reason);

        internal static void WriteFailure(EntityManager manager, FixedString64Bytes mapId, Entity player, int networkId,
            int dropId, FixedString64Bytes itemId) =>
            WriteInternal(manager, mapId, player, networkId, dropId, itemId, 0, CombatPrototypeMapPickupResult.Failed, null);

        private static void WriteInternal(EntityManager manager, FixedString64Bytes mapId, Entity player, int networkId,
            int dropId, FixedString64Bytes itemId, int quantity, CombatPrototypeMapPickupResult result, string reason)
        {
            // Result publication is independent of the original pickup settlement and its error boundary.
            try
            {
                if (reason != null)
                {
                    switch (reason)
                    {
                        case "CommandTargetOwnerMismatch":
                        case "PlayerDead": return;
                        case "PlayerMoving": result = CombatPrototypeMapPickupResult.PlayerMoving; break;
                        case "AttackInProgress": result = CombatPrototypeMapPickupResult.AttackInProgress; break;
                        case "NoLandedTarget": result = CombatPrototypeMapPickupResult.NoLandedTarget; break;
                        case "InventoryAlreadyOverCapacity":
                        case "MaterialTotalCapacityExceeded":
                        case "MaterialItemCapacityExceeded": result = CombatPrototypeMapPickupResult.NoSpace; break;
                        case "InvalidMoveInput": result = CombatPrototypeMapPickupResult.Failed; break;
                        default: throw new InvalidOperationException("Unsupported pickup rejection reason: " + reason);
                    }
                }
                if (manager.GetComponentData<GhostOwner>(player).NetworkId != networkId ||
                    manager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0) return;
                var feedback = manager.GetComponentData<CombatPrototypeMapPickupFeedback>(player);
                unchecked { feedback.Sequence++; }
                feedback.Result = result;
                feedback.ItemId = result == CombatPrototypeMapPickupResult.PickedUp ? itemId : default;
                feedback.Quantity = result == CombatPrototypeMapPickupResult.PickedUp ? quantity : 0;
                manager.SetComponentData(player, feedback);
            }
            catch (Exception exception)
            {
                Debug.LogError("[CombatPrototype.Map] Pickup feedback failed; stage=WriteFeedback, map=" + mapId +
                    ", NetworkId=" + networkId + ", player=" + player + ", DropId=" + dropId + ", itemId=" + itemId +
                    ", quantity=" + quantity + ", result=" + result + ", reason=" + reason + ". " + exception);
            }
        }
    }
}
