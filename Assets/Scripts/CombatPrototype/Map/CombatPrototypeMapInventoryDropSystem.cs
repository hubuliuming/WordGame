using System;
using System.Collections.Generic;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMapDropSpawnSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapGatherToolCraftSystem))]
    [UpdateBefore(typeof(CombatPrototypeMapInteractionSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    [CreateAfter(typeof(CombatPrototypeMapDropSpawnSystem))]
    [CreateAfter(typeof(CombatPrototypeMapInteractionSystem))]
    public partial class CombatPrototypeMapInventoryDropSystem : SystemBase
    {
        private struct Request
        {
            public Entity Player;
            public int NetworkId;
            public CombatPrototypePlayerInput Input;
        }

        private readonly HashSet<Entity> _busy = new HashSet<Entity>();
        private CombatPrototypeMapInteractionSystem _interaction;
        private CombatPrototypeMapDropSpawnSystem _dropOwner;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _interaction = World.GetExistingSystemManaged<CombatPrototypeMapInteractionSystem>();
            _dropOwner = World.GetExistingSystemManaged<CombatPrototypeMapDropSpawnSystem>();
            if (_interaction == null || _dropOwner == null)
                throw new InvalidOperationException("Inventory dropping requires the F interaction and map drop ownership services.");
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapInventoryDropSettings>(source);
            using var requests = new NativeList<Request>(Allocator.Temp);
            foreach (var (stream, command, id) in SystemAPI.Query<RefRO<NetworkStreamConnection>, RefRO<CommandTarget>, RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>())
            {
                var player = command.ValueRO.targetEntity;
                if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !EntityManager.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                    !EntityManager.HasComponent<Simulate>(player) || !EntityManager.IsComponentEnabled<Simulate>(player)) continue;
                try
                {
                    var input = EntityManager.GetComponentData<CombatPrototypePlayerInput>(player);
                    if (input.DropInventory.IsSet)
                        requests.Add(new Request { Player = player, NetworkId = id.ValueRO.Value, Input = input });
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.InventoryDrop] Request failed; stage=ReadInput, map=" + map.MapDefinitionId +
                        ", NetworkId=" + id.ValueRO.Value + ", player=" + player + ". " + exception);
                }
            }
            if (requests.Length == 0) return;
            _busy.Clear();
            foreach (var (state, progress) in SystemAPI.Query<RefRO<CombatPrototypeMapGatherState>, RefRO<CombatPrototypeMapGatherProgress>>())
                if (state.ValueRO.Phase == CombatPrototypeMapGatherPhase.Collecting) _busy.Add(progress.ValueRO.Collector);
            foreach (var (state, progress) in SystemAPI.Query<RefRO<CombatPrototypeMapTreeState>, RefRO<CombatPrototypeMapTreeProgress>>())
                if (state.ValueRO.Phase == CombatPrototypeMapTreePhase.Chopping) _busy.Add(progress.ValueRO.Collector);
            foreach (var (state, progress) in SystemAPI.Query<RefRO<CombatPrototypeMapMineState>, RefRO<CombatPrototypeMapMineProgress>>())
                if (state.ValueRO.Phase == CombatPrototypeMapMinePhase.Mining) _busy.Add(progress.ValueRO.Collector);

            // Instantiate invalidates DynamicBuffers; keep an independent definition snapshot for this batch.
            using var definitions = EntityManager.GetBuffer<CombatPrototypeMapInventoryDropDefinition>(source, true).ToNativeArray(Allocator.Temp);
            foreach (var request in requests) Execute(source, map.MapDefinitionId, settings, definitions, request);
        }

        private void Execute(Entity source, FixedString64Bytes mapId, CombatPrototypeMapInventoryDropSettings settings,
            NativeArray<CombatPrototypeMapInventoryDropDefinition> definitions, Request request)
        {
            var stage = "ValidatePlayer";
            var drop = Entity.Null;
            var dropId = 0;
            var saved = false;
            var committed = false;
            var activated = false;
            var kind = (CombatPrototypeMapInventoryDropKind)request.Input.InventoryDropItem;
            try
            {
                var reason = _interaction.GetInteractionHintRejection(request.Player, request.NetworkId, request.Input);
                if (reason == "CommandTargetOwnerMismatch") return;
                if (reason == null && HasPriorOperation(request.Input)) reason = "ExistingOperationHasPriority";
                if (reason == null && _busy.Contains(request.Player)) reason = "ResourceInteractionBusy";
                if (reason == null && settings.Enabled == 0) reason = "Disabled";
                var mode = (CombatPrototypeMapInventoryDropMode)request.Input.InventoryDropMode;
                if (reason == null && (mode != CombatPrototypeMapInventoryDropMode.Single && mode != CombatPrototypeMapInventoryDropMode.All))
                    reason = "InvalidMode";
                if (reason == null && mode == CombatPrototypeMapInventoryDropMode.All && settings.AllowDropAll == 0) reason = "AllDisabled";
                var definition = default(CombatPrototypeMapInventoryDropDefinition);
                var found = false;
                for (var index = 0; index < definitions.Length; index++)
                    if (definitions[index].Kind == kind) { definition = definitions[index]; found = true; break; }
                if (reason == null && !found) reason = "UnsupportedItem";
                if (reason != null) { Reject(mapId, request, kind, reason); return; }

                stage = "PrepareConsumption";
                var inventory = EntityManager.GetBuffer<CombatPrototypeInventoryItem>(request.Player);
                var itemIndex = FindItem(inventory, definition.ItemName);
                var current = itemIndex >= 0 ? inventory[itemIndex] : default;
                var quantity = mode == CombatPrototypeMapInventoryDropMode.All ? current.Quantity : settings.SingleDropQuantity;
                if (current.Quantity <= 0 || quantity <= 0 || current.Quantity < quantity)
                {
                    Reject(mapId, request, kind, "InsufficientQuantity");
                    return;
                }
                var next = current;
                next.Quantity -= quantity;
                var identity = EntityManager.GetComponentData<CombatPrototypePlayerIdentity>(request.Player).PlayerId;
                var reward = EntityManager.GetComponentData<CombatPrototypePlayerReward>(request.Player);
                var candidate = CombatPrototypePlayerSaveStore.PrepareItemConsumption(identity, reward, inventory,
                    EntityManager.GetBuffer<CombatPrototypeMapGatherTool>(request.Player), itemIndex, next);
                stage = "ValidatePrefab";
                ValidatePrefab(definition.Prefab);
                var start = EntityManager.GetComponentData<LocalTransform>(request.Player).Position;
                stage = "CreatePreparedDrop";
                drop = _dropOwner.SpawnOwnedDrop(source, definition.Prefab, definition.ResourceKey, definition.ItemId,
                    quantity, start, out dropId, CombatPrototypeMapDropPhase.Prepared);

                stage = "ReacquireCommitReferences";
                inventory = EntityManager.GetBuffer<CombatPrototypeInventoryItem>(request.Player);
                var dropState = SystemAPI.GetComponentLookup<CombatPrototypeMapDropState>().GetRefRW(drop);
                var feedback = SystemAPI.GetComponentLookup<CombatPrototypeMapInventoryDropFeedback>().GetRefRW(request.Player);
                stage = "SavePrepared";
                CombatPrototypePlayerSaveStore.SavePrepared(candidate);
                saved = true;
                stage = "CommitInventory";
                if (next.Quantity == 0) inventory.RemoveAt(itemIndex);
                else inventory[itemIndex] = next;
                committed = true;
                stage = "ActivateDrop";
                dropState.ValueRW.Phase = CombatPrototypeMapDropPhase.Airborne;
                activated = true;
                Report(feedback, kind, quantity, CombatPrototypeMapInventoryDropResult.Success);
                Debug.Log("[CombatPrototype.InventoryDrop] Dropped and saved; map=" + mapId + ", NetworkId=" + request.NetworkId +
                    ", PlayerId=" + identity + ", DropId=" + dropId + ", itemId=" + definition.ItemId + ", quantity=" + quantity + ".");
            }
            catch (Exception exception)
            {
                // Save failure releases only this prepared instance. A successful save cannot be claimed rolled back.
                if (!saved && drop != Entity.Null) _dropOwner.ReleaseDrop(drop, "InventoryDropRollback");
                try
                {
                    var feedback = SystemAPI.GetComponentLookup<CombatPrototypeMapInventoryDropFeedback>().GetRefRW(request.Player);
                    Report(feedback, kind, 0, CombatPrototypeMapInventoryDropResult.Failed);
                }
                catch (Exception feedbackException)
                {
                    Debug.LogError("[CombatPrototype.InventoryDrop] Feedback failed; stage=ReportFailure, map=" + mapId +
                        ", NetworkId=" + request.NetworkId + ", player=" + request.Player + ". " + feedbackException);
                }
                Debug.LogError("[CombatPrototype.InventoryDrop] Drop failed; stage=" + stage + ", map=" + mapId +
                    ", NetworkId=" + request.NetworkId + ", player=" + request.Player + ", item=" + kind + ", DropId=" + dropId +
                    ", saved=" + saved + ", committed=" + committed + ", activated=" + activated + ". " + exception);
            }
        }

        private void ValidatePrefab(Entity prefab)
        {
            if (!EntityManager.HasComponent<Prefab>(prefab) || !EntityManager.HasComponent<GhostType>(prefab) ||
                !EntityManager.HasComponent<LocalTransform>(prefab) || !EntityManager.HasComponent<CombatPrototypeMapDropState>(prefab) ||
                !EntityManager.HasComponent<CombatPrototypeMapDropProgress>(prefab) ||
                EntityManager.GetComponentData<CombatPrototypeMapDropState>(prefab).Phase != CombatPrototypeMapDropPhase.Prepared)
                throw new InvalidOperationException("Inventory drop prefab requires the existing Ghost/transform/drop components and Prepared initial phase: " + prefab);
        }

        private static bool HasPriorOperation(CombatPrototypePlayerInput input) => input.Gather.IsSet || input.Pickup.IsSet ||
            input.UseItem.IsSet || input.CraftAxe.IsSet || input.CraftPickaxe.IsSet || input.Respawn.IsSet ||
            input.RepairAxe.IsSet || input.RepairPickaxe.IsSet;

        private static int FindItem(DynamicBuffer<CombatPrototypeInventoryItem> inventory, FixedString64Bytes name)
        {
            var found = -1;
            for (var index = 0; index < inventory.Length; index++)
            {
                if (!inventory[index].ItemName.Equals(name)) continue;
                if (found >= 0) throw new InvalidOperationException("Inventory contains duplicate drop item: " + name);
                found = index;
            }
            return found;
        }

        private void Reject(FixedString64Bytes mapId, Request request, CombatPrototypeMapInventoryDropKind kind, string reason)
        {
            var feedback = SystemAPI.GetComponentLookup<CombatPrototypeMapInventoryDropFeedback>().GetRefRW(request.Player);
            Report(feedback, kind, 0, CombatPrototypeMapInventoryDropResult.Rejected);
            Debug.Log("[CombatPrototype.InventoryDrop] Drop rejected; map=" + mapId + ", NetworkId=" + request.NetworkId +
                ", player=" + request.Player + ", item=" + kind + ", reason=" + reason + ".");
        }

        private static void Report(RefRW<CombatPrototypeMapInventoryDropFeedback> feedback, CombatPrototypeMapInventoryDropKind kind,
            int quantity, CombatPrototypeMapInventoryDropResult result)
        {
            feedback.ValueRW = new CombatPrototypeMapInventoryDropFeedback { Sequence = unchecked(feedback.ValueRO.Sequence + 1),
                Kind = (byte)kind <= (byte)CombatPrototypeMapInventoryDropKind.Stone ? kind : CombatPrototypeMapInventoryDropKind.None,
                Quantity = quantity, Result = result };
        }

        protected override void OnStopRunning() { _busy.Clear(); }
    }
}
