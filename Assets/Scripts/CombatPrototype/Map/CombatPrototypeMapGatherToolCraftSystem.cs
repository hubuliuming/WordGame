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
    [UpdateAfter(typeof(CombatPrototypePlayerDamageSystem))]
    [UpdateAfter(typeof(CombatPrototypeMapDropCleanupSystem))]
    [UpdateBefore(typeof(CombatPrototypeMapInteractionSystem))]
    [UpdateBefore(typeof(CombatPrototypePlayerRespawnSystem))]
    [CreateAfter(typeof(CombatPrototypeMapInteractionSystem))]
    public partial class CombatPrototypeMapGatherToolCraftSystem : SystemBase
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
            if (_interaction == null) throw new InvalidOperationException("Gathering tool crafting requires the F interaction service.");
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapGatherToolSettings>(source);
            var definitions = EntityManager.GetBuffer<CombatPrototypeMapGatherToolDefinition>(source, true);
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
                    if (!input.CraftAxe.IsSet && !input.CraftPickaxe.IsSet) continue;
                    requests.Add(new Request { Player = player, NetworkId = id.ValueRO.Value, Input = input,
                        Kind = input.CraftAxe.IsSet ? CombatPrototypeMapGatherToolKind.Axe : CombatPrototypeMapGatherToolKind.Pickaxe });
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Tools] Craft request failed; stage=ReadInput, map=" + map.MapDefinitionId +
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
                var feedback = default(RefRW<CombatPrototypeMapToolCraftFeedback>);
                var hasFeedback = false;
                var saved = false;
                try
                {
                    var reason = _interaction.GetInteractionHintRejection(request.Player, request.NetworkId, request.Input);
                    if (reason == "CommandTargetOwnerMismatch") continue;
                    feedback = SystemAPI.GetComponentLookup<CombatPrototypeMapToolCraftFeedback>().GetRefRW(request.Player);
                    hasFeedback = true;
                    var result = reason != null ? CombatPrototypeMapToolCraftResult.PlayerUnavailable :
                        request.Input.Gather.IsSet ? CombatPrototypeMapToolCraftResult.FHasPriority :
                        _busy.Contains(request.Player) ? CombatPrototypeMapToolCraftResult.Busy :
                        settings.Enabled == 0 ? CombatPrototypeMapToolCraftResult.Disabled : CombatPrototypeMapToolCraftResult.None;
                    if (result != CombatPrototypeMapToolCraftResult.None)
                    {
                        Report(feedback, request.Kind, result);
                        Reject(map.MapDefinitionId, request, reason ?? result.ToString());
                        continue;
                    }
                    stage = "PrepareCraft";
                    var definition = CombatPrototypeMapGatherToolUtility.RequireDefinition(definitions, request.Kind);
                    var tools = EntityManager.GetBuffer<CombatPrototypeMapGatherTool>(request.Player);
                    var toolIndex = CombatPrototypeMapGatherToolUtility.FindOwned(tools, definition.ToolId);
                    if (toolIndex >= 0 && tools[toolIndex].Durability >= definition.DurabilityCostPerCompletion)
                    {
                        Report(feedback, request.Kind, CombatPrototypeMapToolCraftResult.AlreadyUsable);
                        Reject(map.MapDefinitionId, request, "AlreadyUsable");
                        continue;
                    }
                    var inventory = EntityManager.GetBuffer<CombatPrototypeInventoryItem>(request.Player);
                    var woodIndex = FindMaterial(inventory, WoodName);
                    var stoneIndex = FindMaterial(inventory, StoneName);
                    var woodQuantity = woodIndex >= 0 ? inventory[woodIndex].Quantity : 0;
                    var stoneQuantity = stoneIndex >= 0 ? inventory[stoneIndex].Quantity : 0;
                    if (woodQuantity < definition.CraftWoodQuantity || stoneQuantity < definition.CraftStoneQuantity)
                    {
                        Report(feedback, request.Kind, CombatPrototypeMapToolCraftResult.InsufficientMaterials);
                        Reject(map.MapDefinitionId, request, "InsufficientMaterials");
                        continue;
                    }
                    var nextWood = woodQuantity - definition.CraftWoodQuantity;
                    var nextStone = stoneQuantity - definition.CraftStoneQuantity;
                    var nextTool = new CombatPrototypeMapGatherTool { ToolId = definition.ToolId, Durability = definition.MaxDurability, Level = 1 };
                    if (toolIndex < 0) tools.EnsureCapacity(checked(tools.Length + 1));
                    var identity = EntityManager.GetComponentData<CombatPrototypePlayerIdentity>(request.Player).PlayerId;
                    var reward = EntityManager.GetComponentData<CombatPrototypePlayerReward>(request.Player);
                    var candidate = CombatPrototypePlayerSaveStore.PrepareToolCraft(identity, reward, inventory, tools,
                        EntityManager.GetComponentData<CombatPrototypeMapInventoryCapacityLevel>(request.Player).Level,
                        woodIndex, nextWood, stoneIndex, nextStone, toolIndex, nextTool);
                    stage = "SavePrepared";
                    CombatPrototypePlayerSaveStore.SavePrepared(candidate);
                    saved = true;
                    stage = "CommitCraft";
                    // Capacity and references are ready; commit performs only nonstructural writes.
                    CommitMaterials(inventory, woodIndex, nextWood, stoneIndex, nextStone);
                    if (toolIndex >= 0) tools[toolIndex] = nextTool;
                    else tools.Add(nextTool);
                    Report(feedback, request.Kind, CombatPrototypeMapToolCraftResult.Success);
                    Debug.Log("[CombatPrototype.Tools] Crafted and saved; map=" + map.MapDefinitionId + ", NetworkId=" +
                        request.NetworkId + ", PlayerId=" + identity + ", tool=" + definition.ToolId + ", durability=" + nextTool.Durability + ".");
                }
                catch (Exception exception)
                {
                    if (hasFeedback) Report(feedback, request.Kind, CombatPrototypeMapToolCraftResult.Failed);
                    Debug.LogError("[CombatPrototype.Tools] Craft failed; stage=" + stage + ", map=" + map.MapDefinitionId +
                        ", NetworkId=" + request.NetworkId + ", player=" + request.Player + ", tool=" + request.Kind +
                        ", saved=" + saved + ". " + exception);
                }
            }
        }

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

        private static void Report(RefRW<CombatPrototypeMapToolCraftFeedback> feedback,
            CombatPrototypeMapGatherToolKind kind, CombatPrototypeMapToolCraftResult result)
        {
            var next = feedback.ValueRO;
            next.Sequence = unchecked(next.Sequence + 1);
            next.Kind = kind; next.Result = result;
            feedback.ValueRW = next;
        }

        private static void Reject(FixedString64Bytes mapId, Request request, string reason)
        {
            Debug.Log("[CombatPrototype.Tools] Craft rejected; map=" + mapId + ", NetworkId=" + request.NetworkId +
                ", player=" + request.Player + ", tool=" + request.Kind + ", reason=" + reason + ".");
        }

        protected override void OnStopRunning() { _busy.Clear(); }
    }
}
