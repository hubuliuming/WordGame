using System;
using Code_01.CombatPrototype.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeMapDropMotionSystem))]
    [UpdateBefore(typeof(CombatPrototypeMapGatherSystem))]
    public partial class CombatPrototypeMapDropPickupSystem : SystemBase
    {
        private struct OnlinePlayer
        {
            public Entity Entity;
            public int NetworkId;
        }

        private EntityQuery _drops;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypeMapDropSettings>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _drops = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapDropState>(),
                ComponentType.ReadOnly<CombatPrototypeMapDropProgress>(), ComponentType.ReadOnly<LocalTransform>());
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var map = SystemAPI.GetSingleton<CombatPrototypeMapData>();
            var settings = SystemAPI.GetSingleton<CombatPrototypeMapDropSettings>();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var capacity = EntityManager.GetComponentData<CombatPrototypeMapInventoryCapacitySettings>(source);
            var capacityDefinitions = EntityManager.GetBuffer<CombatPrototypeMapInventoryCapacityDefinition>(source, true);
            var time = SystemAPI.Time.ElapsedTime;
            using var drops = _drops.ToEntityArray(Allocator.Temp);
            var players = new NativeList<OnlinePlayer>(Allocator.Temp);
            try
            {
                foreach (var (stream, command, id) in SystemAPI.Query<
                             RefRO<NetworkStreamConnection>, RefRO<CommandTarget>, RefRO<NetworkId>>()
                             .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>())
                {
                    var player = command.ValueRO.targetEntity;
                    if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                        !SystemAPI.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                        !SystemAPI.HasComponent<Simulate>(player) || !SystemAPI.IsComponentEnabled<Simulate>(player)) continue;
                    var value = new OnlinePlayer { Entity = player, NetworkId = id.ValueRO.Value };
                    var index = players.Length;
                    players.Add(value);
                    while (index > 0 && players[index - 1].NetworkId > value.NetworkId)
                    {
                        players[index] = players[index - 1];
                        index--;
                    }
                    players[index] = value;
                }

                var inputs = SystemAPI.GetComponentLookup<CombatPrototypePlayerInput>(true);
                var owners = SystemAPI.GetComponentLookup<GhostOwner>(true);
                var healths = SystemAPI.GetComponentLookup<CombatPrototypePlayerHealth>(true);
                var melees = SystemAPI.GetComponentLookup<CombatPrototypeMeleeState>(true);
                var transforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
                var identities = SystemAPI.GetComponentLookup<CombatPrototypePlayerIdentity>(true);
                var rewards = SystemAPI.GetComponentLookup<CombatPrototypePlayerReward>(true);
                var inventories = SystemAPI.GetBufferLookup<CombatPrototypeInventoryItem>();
                var states = SystemAPI.GetComponentLookup<CombatPrototypeMapDropState>();
                var progresses = SystemAPI.GetComponentLookup<CombatPrototypeMapDropProgress>(true);
                foreach (var player in players)
                {
                    var stage = "ReadInput";
                    var dropId = 0;
                    FixedString64Bytes itemId = default;
                    try
                    {
                        var input = inputs[player.Entity];
                        if (!input.Pickup.IsSet) continue;
                        stage = "ValidatePlayer";
                        var reason = GetPickupHintRejection(player.Entity, player.NetworkId, input, owners, healths, melees);
                        if (reason != null)
                        {
                            Reject(map.MapDefinitionId, player, reason);
                            continue;
                        }

                        stage = "SelectDrop";
                        var target = CombatPrototypeMapDropTargetSelector.Select(transforms[player.Entity].Position.xz,
                            settings.PickupDistance, time, drops, states, progresses, transforms);
                        if (target == Entity.Null)
                        {
                            Reject(map.MapDefinitionId, player, "NoLandedTarget");
                            continue;
                        }

                        stage = "PrepareReward";
                        var current = states.GetRefRW(target);
                        dropId = current.ValueRO.DropId;
                        var quantity = current.ValueRO.Quantity;
                        itemId = current.ValueRO.ItemId;
                        var itemName = CombatPrototypeMapYieldItemResolver.Resolve(itemId.ToString());
                        var playerId = identities[player.Entity].PlayerId;
                        var inventory = inventories[player.Entity];
                        stage = "CheckCapacity";
                        var capacityReason = CombatPrototypeMapInventoryCapacityUtility.GetRejection(capacity, capacityDefinitions,
                            inventory, itemName, quantity);
                        if (capacityReason != null)
                        {
                            Reject(map.MapDefinitionId, player, capacityReason + ", DropId=" + dropId + ", itemId=" + itemId);
                            continue;
                        }
                        stage = "PrepareReward";
                        var itemIndex = -1;
                        for (var i = 0; i < inventory.Length; i++)
                            if (inventory[i].ItemName.Equals(itemName)) { itemIndex = i; break; }
                        var next = new CombatPrototypeInventoryItem { ItemName = itemName, Quantity = quantity };
                        if (itemIndex >= 0) next.Quantity = checked(inventory[itemIndex].Quantity + quantity);
                        else inventory.EnsureCapacity(checked(inventory.Length + 1));
                        var candidate = CombatPrototypePlayerSaveStore.PrepareReward(
                            playerId, rewards[player.Entity], inventory, EntityManager.GetBuffer<CombatPrototypeMapGatherTool>(player.Entity, true), itemIndex, next);

                        stage = "SavePrepared";
                        CombatPrototypePlayerSaveStore.SavePrepared(candidate);

                        // References and capacity were acquired before saving; commit makes no structural change or allocation.
                        stage = "CommitPickup";
                        if (itemIndex >= 0) inventory[itemIndex] = next;
                        else inventory.Add(next);
                        current.ValueRW.Phase = CombatPrototypeMapDropPhase.Consumed;
                        Debug.Log("[CombatPrototype.Map] Pickup granted and saved; map=" + map.MapDefinitionId +
                            ", DropId=" + dropId + ", NetworkId=" + player.NetworkId + ", PlayerId=" + playerId +
                            ", item=" + itemName + ", quantity=+" + quantity +
                            ", totalItemQuantity=" + next.Quantity + ".");
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("[CombatPrototype.Map] Pickup settlement or save failed; map=" + map.MapDefinitionId +
                            ", DropId=" + dropId + ", NetworkId=" + player.NetworkId + ", player=" + player.Entity +
                            ", itemId=" + itemId + ", stage=" + stage + ". " + exception);
                    }
                }
            }
            finally { players.Dispose(); }
        }

        internal static string GetPickupHintRejection(Entity player, int networkId, CombatPrototypePlayerInput input,
            ComponentLookup<GhostOwner> owners, ComponentLookup<CombatPrototypePlayerHealth> healths,
            ComponentLookup<CombatPrototypeMeleeState> melees)
        {
            if (owners[player].NetworkId != networkId) return "CommandTargetOwnerMismatch";
            if (healths[player].IsDead != 0) return "PlayerDead";
            if (input.Attack.IsSet || melees[player].Phase != CombatPrototypeAttackPhase.Ready) return "AttackInProgress";
            if (!math.all(math.isfinite(input.Move))) return "InvalidMoveInput";
            if (math.lengthsq(input.Move) != 0f) return "PlayerMoving";
            return null;
        }

        private static void Reject(FixedString64Bytes mapId, OnlinePlayer player, string reason)
        {
            Debug.Log("[CombatPrototype.Map] Pickup rejected; map=" + mapId + ", NetworkId=" + player.NetworkId +
                ", player=" + player.Entity + ", reason=" + reason + ".");
        }
    }
}
