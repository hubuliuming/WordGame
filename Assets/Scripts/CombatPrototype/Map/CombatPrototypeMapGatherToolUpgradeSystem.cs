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
    [UpdateAfter(typeof(CombatPrototypeMapInventoryCapacityUpgradeSystem))]
    [UpdateBefore(typeof(CombatPrototypeMapInteractionSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    [CreateAfter(typeof(CombatPrototypeMapInteractionSystem))]
    public partial class CombatPrototypeMapGatherToolUpgradeSystem : SystemBase
    {
        private struct Request
        {
            public Entity Player;
            public int NetworkId;
            public CombatPrototypePlayerInput Input;
            public CombatPrototypeMapGatherToolKind Kind;
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
            if (_interaction == null) throw new InvalidOperationException("Tool upgrades require the F interaction service.");
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var toolSettings = EntityManager.GetComponentData<CombatPrototypeMapGatherToolSettings>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapGatherToolUpgradeSettings>(source);
            var definitions = EntityManager.GetBuffer<CombatPrototypeMapGatherToolDefinition>(source, true);
            var upgrades = EntityManager.GetBuffer<CombatPrototypeMapGatherToolUpgradeDefinition>(source, true);
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
                    if (!input.UpgradeAxe.IsSet && !input.UpgradePickaxe.IsSet) continue;
                    requests.Add(new Request { Player = player, NetworkId = id.ValueRO.Value, Input = input,
                        Kind = input.UpgradeAxe.IsSet ? CombatPrototypeMapGatherToolKind.Axe : CombatPrototypeMapGatherToolKind.Pickaxe });
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.ToolUpgrade] Request failed; stage=ReadInput, map=" + map.MapDefinitionId +
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
                var feedback = default(RefRW<CombatPrototypeMapToolUpgradeFeedback>);
                var hasFeedback = false;
                var saved = false;
                try
                {
                    var reason = _interaction.GetInteractionHintRejection(request.Player, request.NetworkId, request.Input);
                    if (reason == "CommandTargetOwnerMismatch") continue;
                    feedback = SystemAPI.GetComponentLookup<CombatPrototypeMapToolUpgradeFeedback>().GetRefRW(request.Player);
                    hasFeedback = true;
                    var result = reason != null ? CombatPrototypeMapToolUpgradeResult.PlayerUnavailable :
                        HasPriorOperation(request.Input) ? CombatPrototypeMapToolUpgradeResult.ExistingOperationHasPriority :
                        _busy.Contains(request.Player) ? CombatPrototypeMapToolUpgradeResult.Busy :
                        toolSettings.Enabled == 0 || settings.Enabled == 0 ? CombatPrototypeMapToolUpgradeResult.Disabled :
                        CombatPrototypeMapToolUpgradeResult.None;
                    if (result != CombatPrototypeMapToolUpgradeResult.None)
                    {
                        Report(feedback, request.Kind, result);
                        Reject(map.MapDefinitionId, request, reason ?? result.ToString());
                        continue;
                    }
                    stage = "PrepareUpgrade";
                    var definition = CombatPrototypeMapGatherToolUtility.RequireDefinition(definitions, request.Kind);
                    var tools = EntityManager.GetBuffer<CombatPrototypeMapGatherTool>(request.Player);
                    var toolIndex = CombatPrototypeMapGatherToolUtility.FindOwned(tools, definition.ToolId);
                    if (toolIndex < 0)
                    {
                        Report(feedback, request.Kind, CombatPrototypeMapToolUpgradeResult.NotOwned);
                        Reject(map.MapDefinitionId, request, "NotOwned");
                        continue;
                    }
                    var nextTool = tools[toolIndex];
                    CombatPrototypeMapGatherToolUtility.ValidateLevel(nextTool.Level);
                    if (nextTool.Level == CombatPrototypeMapGatherToolUtility.MaximumLevel)
                    {
                        Report(feedback, request.Kind, CombatPrototypeMapToolUpgradeResult.MaxLevel);
                        Reject(map.MapDefinitionId, request, "MaxLevel");
                        continue;
                    }
                    var upgrade = CombatPrototypeMapGatherToolUtility.RequireUpgradeDefinition(upgrades, definition.ToolId, nextTool.Level + 1);
                    var inventory = EntityManager.GetBuffer<CombatPrototypeInventoryItem>(request.Player);
                    var woodIndex = FindMaterial(inventory, WoodName);
                    var stoneIndex = FindMaterial(inventory, StoneName);
                    var woodQuantity = woodIndex >= 0 ? inventory[woodIndex].Quantity : 0;
                    var stoneQuantity = stoneIndex >= 0 ? inventory[stoneIndex].Quantity : 0;
                    if (woodQuantity < upgrade.WoodQuantity || stoneQuantity < upgrade.StoneQuantity)
                    {
                        Report(feedback, request.Kind, CombatPrototypeMapToolUpgradeResult.InsufficientMaterials);
                        Reject(map.MapDefinitionId, request, "InsufficientMaterials");
                        continue;
                    }
                    var nextWood = woodQuantity - upgrade.WoodQuantity;
                    var nextStone = stoneQuantity - upgrade.StoneQuantity;
                    // Upgrading raises the maximum and efficiency without granting durability.
                    nextTool.Level = upgrade.Level;
                    var identity = EntityManager.GetComponentData<CombatPrototypePlayerIdentity>(request.Player).PlayerId;
                    var reward = EntityManager.GetComponentData<CombatPrototypePlayerReward>(request.Player);
                    // The existing projection replaces one owned tool and the two material quantities.
                    var candidate = CombatPrototypePlayerSaveStore.PrepareToolCraft(identity, reward, inventory, tools,
                        EntityManager.GetComponentData<CombatPrototypeMapInventoryCapacityLevel>(request.Player).Level,
                        woodIndex, nextWood, stoneIndex, nextStone, toolIndex, nextTool);
                    stage = "SavePrepared";
                    CombatPrototypePlayerSaveStore.SavePrepared(candidate);
                    saved = true;
                    stage = "CommitUpgrade";
                    // References are acquired before saving; commit needs no structural change or allocation.
                    CommitMaterials(inventory, woodIndex, nextWood, stoneIndex, nextStone);
                    tools[toolIndex] = nextTool;
                    Report(feedback, request.Kind, CombatPrototypeMapToolUpgradeResult.Success);
                    Debug.Log("[CombatPrototype.ToolUpgrade] Upgraded and saved; map=" + map.MapDefinitionId +
                        ", NetworkId=" + request.NetworkId + ", PlayerId=" + identity + ", tool=" + definition.ToolId +
                        ", level=" + nextTool.Level + ", durability=" + nextTool.Durability + ".");
                }
                catch (Exception exception)
                {
                    if (hasFeedback) Report(feedback, request.Kind, CombatPrototypeMapToolUpgradeResult.Failed);
                    Debug.LogError("[CombatPrototype.ToolUpgrade] Upgrade failed; stage=" + stage + ", map=" + map.MapDefinitionId +
                        ", NetworkId=" + request.NetworkId + ", player=" + request.Player + ", tool=" + request.Kind +
                        ", saved=" + saved + ". " + exception);
                }
            }
        }

        private static bool HasPriorOperation(CombatPrototypePlayerInput input) => input.Gather.IsSet || input.Pickup.IsSet ||
            input.UseItem.IsSet || input.Respawn.IsSet || input.CraftAxe.IsSet || input.CraftPickaxe.IsSet ||
            input.RepairAxe.IsSet || input.RepairPickaxe.IsSet || input.DropInventory.IsSet || input.UpgradeInventoryCapacity.IsSet;

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

        private static void Report(RefRW<CombatPrototypeMapToolUpgradeFeedback> feedback,
            CombatPrototypeMapGatherToolKind kind, CombatPrototypeMapToolUpgradeResult result)
        {
            var next = feedback.ValueRO;
            next.Sequence = unchecked(next.Sequence + 1);
            next.Kind = kind; next.Result = result;
            feedback.ValueRW = next;
        }

        private static void Reject(FixedString64Bytes mapId, Request request, string reason)
        {
            Debug.Log("[CombatPrototype.ToolUpgrade] Rejected; map=" + mapId + ", NetworkId=" + request.NetworkId +
                ", player=" + request.Player + ", tool=" + request.Kind + ", reason=" + reason + ".");
        }

        protected override void OnStopRunning() { _busy.Clear(); }
    }
}
