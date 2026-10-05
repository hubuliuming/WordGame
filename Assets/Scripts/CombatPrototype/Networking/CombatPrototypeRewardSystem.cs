using Code_01.CombatPrototype.Map;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeDamageSystem))]
    public partial struct CombatPrototypeRewardSystem : ISystem
    {
        private struct OnlinePlayer
        {
            public Entity Entity;
            public int NetworkId;
        }

        public void OnUpdate(ref SystemState state)
        {
            using var players = new NativeList<OnlinePlayer>(Allocator.Temp);
            foreach (var (connection, command, id) in SystemAPI.Query<
                         RefRO<NetworkStreamConnection>, RefRO<CommandTarget>, RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>())
            {
                var player = command.ValueRO.targetEntity;
                if (connection.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !SystemAPI.HasComponent<CombatPrototypePlayerNetCode>(player))
                    continue;
                players.Add(new OnlinePlayer { Entity = player, NetworkId = id.ValueRO.Value });
            }

            var rewards = SystemAPI.GetComponentLookup<CombatPrototypePlayerReward>();
            var inventories = SystemAPI.GetBufferLookup<CombatPrototypeInventoryItem>();
            var identities = SystemAPI.GetComponentLookup<CombatPrototypePlayerIdentity>(true);
            var ownedTools = SystemAPI.GetBufferLookup<Code_01.CombatPrototype.Map.CombatPrototypeMapGatherTool>(true);
            foreach (var (events, enemy) in SystemAPI.Query<DynamicBuffer<CombatPrototypeKillRewardEvent>>()
                         .WithEntityAccess())
            {
                for (var index = 0; index < events.Length; index++)
                {
                    var reward = events[index];
                    var player = Entity.Null;
                    for (var playerIndex = 0; playerIndex < players.Length; playerIndex++)
                    {
                        if (players[playerIndex].NetworkId != reward.AttackerNetworkId)
                            continue;
                        player = players[playerIndex].Entity;
                        break;
                    }
                    if (player == Entity.Null)
                    {
                        Debug.Log($"[CombatPrototype.NetCode] Server reward skipped; enemy={enemy}, NetworkId={reward.AttackerNetworkId}, attack={reward.AttackSequence}, reason=AttackerOffline, coin={reward.Coin}, experience={reward.Experience}, item={reward.ItemName}, quantity={reward.ItemQuantity}.");
                        continue;
                    }
                    if (!rewards.HasComponent(player) || !inventories.HasBuffer(player) || !identities.HasComponent(player))
                    {
                        Debug.LogError($"[CombatPrototype.NetCode] Server reward settlement failed; enemy={enemy}, NetworkId={reward.AttackerNetworkId}, attack={reward.AttackSequence}, player={player}, reason=MissingPlayerRewardInventoryOrIdentity.");
                        continue;
                    }
                    if (reward.Coin < 0 || reward.Experience < 0 || reward.ItemName.IsEmpty || reward.ItemQuantity <= 0)
                    {
                        Debug.LogError($"[CombatPrototype.NetCode] Server reward settlement failed; enemy={enemy}, NetworkId={reward.AttackerNetworkId}, attack={reward.AttackSequence}, player={player}, reason=InvalidRewardData, item={reward.ItemName}, quantity={reward.ItemQuantity}.");
                        continue;
                    }

                    var playerId = identities[player].PlayerId;
                    try
                    {
                        ref var current = ref rewards.GetRefRW(player).ValueRW;
                        var next = current;
                        var inventory = inventories[player];
                        var itemIndex = -1;
                        for (var inventoryIndex = 0; inventoryIndex < inventory.Length; inventoryIndex++)
                        {
                            if (!inventory[inventoryIndex].ItemName.Equals(reward.ItemName))
                                continue;
                            itemIndex = inventoryIndex;
                            break;
                        }
                        var nextItem = new CombatPrototypeInventoryItem
                        {
                            ItemName = reward.ItemName,
                            Quantity = reward.ItemQuantity
                        };
                        // Prepare all values and any buffer allocation before awarding anything.
                        next.Coin = checked(next.Coin + reward.Coin);
                        next.Experience = checked(next.Experience + reward.Experience);
                        if (itemIndex >= 0)
                            nextItem.Quantity = checked(inventory[itemIndex].Quantity + reward.ItemQuantity);
                        else
                            inventory.EnsureCapacity(checked(inventory.Length + 1));

                        var tools = ownedTools[player];
                        var candidate = CombatPrototypePlayerSaveStore.PrepareReward(playerId, next, inventory, tools, SystemAPI.GetComponent<CombatPrototypeMapInventoryCapacityLevel>(player).Level, itemIndex, nextItem);
                        CombatPrototypePlayerSaveStore.SavePrepared(candidate);

                        // No structural changes or allocations occur between these writes.
                        // The component ref is acquired above so commit needs no second lookup.
                        if (itemIndex >= 0)
                            inventory[itemIndex] = nextItem;
                        else
                            inventory.Add(nextItem);
                        current = next;
                        Debug.Log($"[CombatPrototype.NetCode] Server reward granted and saved; enemy={enemy}, NetworkId={reward.AttackerNetworkId}, PlayerId={playerId}, attack={reward.AttackSequence}, coin=+{reward.Coin}, experience=+{reward.Experience}, item={reward.ItemName}, quantity=+{reward.ItemQuantity}, totalCoin={next.Coin}, totalExperience={next.Experience}, totalItemQuantity={nextItem.Quantity}, path={CombatPrototypePlayerSaveStore.GetSavePath(playerId.ToString())}.");
                    }
                    catch (global::System.Exception exception)
                    {
                        Debug.LogError($"[CombatPrototype.NetCode] Server reward settlement or save failed; enemy={enemy}, NetworkId={reward.AttackerNetworkId}, PlayerId={playerId}, attack={reward.AttackSequence}, player={player}, path={CombatPrototypePlayerSaveStore.GetSavePath(playerId.ToString())}; {exception}");
                    }
                }
                // A skipped or failed reward is consumed too; reconnecting never replays it.
                events.Clear();
            }
        }
    }
}
