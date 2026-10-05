using System;
using System.Collections.Generic;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMapInventoryDropSystem))]
    [UpdateBefore(typeof(CombatPrototypeMapInteractionSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    [CreateAfter(typeof(CombatPrototypeMapInteractionSystem))]
    public partial class CombatPrototypeMapInventoryCapacityUpgradeSystem : SystemBase
    {
        private struct Request
        {
            public Entity Player;
            public int NetworkId;
            public CombatPrototypePlayerInput Input;
        }

        private readonly HashSet<Entity> _busy = new HashSet<Entity>();
        private CombatPrototypeMapInteractionSystem _interaction;
        private static readonly FixedString64Bytes WoodName = new FixedString64Bytes(Msg.ItemName.木材);
        private static readonly FixedString64Bytes StoneName = new FixedString64Bytes(Msg.ItemName.石材);

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _interaction = World.GetExistingSystemManaged<CombatPrototypeMapInteractionSystem>();
            if (_interaction == null) throw new InvalidOperationException("Capacity upgrades require the F interaction service.");
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var capacity = EntityManager.GetComponentData<CombatPrototypeMapInventoryCapacitySettings>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapInventoryCapacityUpgradeSettings>(source);
            var definitions = EntityManager.GetBuffer<CombatPrototypeMapInventoryCapacityUpgradeDefinition>(source, true);
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
                    if (input.UpgradeInventoryCapacity.IsSet)
                        requests.Add(new Request { Player = player, NetworkId = id.ValueRO.Value, Input = input });
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.CapacityUpgrade] Request failed; stage=ReadInput, map=" + map.MapDefinitionId +
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

            foreach (var request in requests)
            {
                var stage = "ValidatePlayer";
                var feedback = default(RefRW<CombatPrototypeMapInventoryCapacityUpgradeFeedback>);
                var hasFeedback = false;
                var saved = false;
                try
                {
                    var reason = _interaction.GetInteractionHintRejection(request.Player, request.NetworkId, request.Input);
                    if (reason == "CommandTargetOwnerMismatch") continue;
                    feedback = SystemAPI.GetComponentLookup<CombatPrototypeMapInventoryCapacityUpgradeFeedback>().GetRefRW(request.Player);
                    hasFeedback = true;
                    var result = reason != null ? CombatPrototypeMapInventoryCapacityUpgradeResult.PlayerUnavailable :
                        HasPriorOperation(request.Input) ? CombatPrototypeMapInventoryCapacityUpgradeResult.ExistingOperationHasPriority :
                        _busy.Contains(request.Player) ? CombatPrototypeMapInventoryCapacityUpgradeResult.Busy :
                        capacity.Enabled == 0 || settings.Enabled == 0 ? CombatPrototypeMapInventoryCapacityUpgradeResult.Disabled :
                        CombatPrototypeMapInventoryCapacityUpgradeResult.None;
                    var level = SystemAPI.GetComponentLookup<CombatPrototypeMapInventoryCapacityLevel>().GetRefRW(request.Player);
                    CombatPrototypeMapInventoryCapacityUtility.ValidateLevel(level.ValueRO.Level);
                    if (result == CombatPrototypeMapInventoryCapacityUpgradeResult.None &&
                        level.ValueRO.Level == CombatPrototypeMapInventoryCapacityUtility.MaximumLevel)
                        result = CombatPrototypeMapInventoryCapacityUpgradeResult.MaxLevel;
                    if (result != CombatPrototypeMapInventoryCapacityUpgradeResult.None)
                    {
                        Report(feedback, result);
                        Reject(map.MapDefinitionId, request, reason ?? result.ToString());
                        continue;
                    }
                    stage = "PrepareUpgrade";
                    var definition = CombatPrototypeMapInventoryCapacityUtility.RequireUpgradeDefinition(definitions, level.ValueRO.Level + 1);
                    var inventory = EntityManager.GetBuffer<CombatPrototypeInventoryItem>(request.Player);
                    var woodIndex = FindMaterial(inventory, WoodName);
                    var stoneIndex = FindMaterial(inventory, StoneName);
                    var woodQuantity = woodIndex >= 0 ? inventory[woodIndex].Quantity : 0;
                    var stoneQuantity = stoneIndex >= 0 ? inventory[stoneIndex].Quantity : 0;
                    if (woodQuantity < definition.WoodQuantity || stoneQuantity < definition.StoneQuantity)
                    {
                        Report(feedback, CombatPrototypeMapInventoryCapacityUpgradeResult.InsufficientMaterials);
                        Reject(map.MapDefinitionId, request, "InsufficientMaterials");
                        continue;
                    }
                    var nextWood = woodQuantity - definition.WoodQuantity;
                    var nextStone = stoneQuantity - definition.StoneQuantity;
                    var identity = EntityManager.GetComponentData<CombatPrototypePlayerIdentity>(request.Player).PlayerId;
                    var reward = EntityManager.GetComponentData<CombatPrototypePlayerReward>(request.Player);
                    var tools = EntityManager.GetBuffer<CombatPrototypeMapGatherTool>(request.Player, true);
                    var candidate = CombatPrototypePlayerSaveStore.PrepareCapacityUpgrade(identity, reward, inventory, tools,
                        woodIndex, nextWood, stoneIndex, nextStone, definition.Level);
                    stage = "SavePrepared";
                    CombatPrototypePlayerSaveStore.SavePrepared(candidate);
                    saved = true;
                    stage = "CommitUpgrade";
                    // All component refs and candidate allocations were prepared before the file replacement.
                    CommitMaterials(inventory, woodIndex, nextWood, stoneIndex, nextStone);
                    level.ValueRW = new CombatPrototypeMapInventoryCapacityLevel { Level = definition.Level };
                    Report(feedback, CombatPrototypeMapInventoryCapacityUpgradeResult.Success);
                    Debug.Log("[CombatPrototype.CapacityUpgrade] Upgraded and saved; map=" + map.MapDefinitionId +
                        ", NetworkId=" + request.NetworkId + ", PlayerId=" + identity + ", level=" + definition.Level + ".");
                }
                catch (Exception exception)
                {
                    if (hasFeedback) Report(feedback, CombatPrototypeMapInventoryCapacityUpgradeResult.Failed);
                    Debug.LogError("[CombatPrototype.CapacityUpgrade] Upgrade failed; stage=" + stage + ", map=" + map.MapDefinitionId +
                        ", NetworkId=" + request.NetworkId + ", player=" + request.Player + ", saved=" + saved + ". " + exception);
                }
            }
        }

        private static bool HasPriorOperation(CombatPrototypePlayerInput input) => input.Gather.IsSet || input.Pickup.IsSet ||
            input.UseItem.IsSet || input.Respawn.IsSet || input.CraftAxe.IsSet || input.CraftPickaxe.IsSet ||
            input.RepairAxe.IsSet || input.RepairPickaxe.IsSet || input.DropInventory.IsSet;

        private static int FindMaterial(DynamicBuffer<CombatPrototypeInventoryItem> inventory, FixedString64Bytes name)
        {
            for (var index = 0; index < inventory.Length; index++)
                if (inventory[index].ItemName.Equals(name)) return index;
            return -1;
        }

        private static void CommitMaterials(DynamicBuffer<CombatPrototypeInventoryItem> inventory,
            int woodIndex, int nextWood, int stoneIndex, int nextStone)
        {
            if (woodIndex >= 0) { var item = inventory[woodIndex]; item.Quantity = nextWood; inventory[woodIndex] = item; }
            if (stoneIndex >= 0) { var item = inventory[stoneIndex]; item.Quantity = nextStone; inventory[stoneIndex] = item; }
            var high = Math.Max(woodIndex, stoneIndex);
            var low = Math.Min(woodIndex, stoneIndex);
            if (high >= 0 && inventory[high].Quantity == 0) inventory.RemoveAt(high);
            if (low >= 0 && inventory[low].Quantity == 0) inventory.RemoveAt(low);
        }

        private static void Report(RefRW<CombatPrototypeMapInventoryCapacityUpgradeFeedback> feedback,
            CombatPrototypeMapInventoryCapacityUpgradeResult result)
        {
            var next = feedback.ValueRO;
            next.Sequence = unchecked(next.Sequence + 1);
            next.Result = result;
            feedback.ValueRW = next;
        }

        private static void Reject(FixedString64Bytes mapId, Request request, string reason) =>
            Debug.Log("[CombatPrototype.CapacityUpgrade] Upgrade rejected; map=" + mapId + ", NetworkId=" + request.NetworkId +
                ", player=" + request.Player + ", reason=" + reason + ".");

        protected override void OnStopRunning() { _busy.Clear(); }
    }
}
