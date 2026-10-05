using Code_01.CombatPrototype.Map;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypeEnemySpatialSystem))]
    [UpdateBefore(typeof(CombatPrototypeMeleeServerSystem))]
    public partial struct CombatPrototypeItemUseSystem : ISystem
    {
        private const int PowerRecovery = 30;
        private FixedString64Bytes _itemName;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatPrototypePlayerSpawner>();
            _itemName = new FixedString64Bytes(global::Code_01.Msg.ItemName.小块肉);
        }

        public void OnUpdate(ref SystemState state)
        {
            var inputs = SystemAPI.GetComponentLookup<CombatPrototypePlayerInput>(true);
            var owners = SystemAPI.GetComponentLookup<GhostOwner>(true);
            var healths = SystemAPI.GetComponentLookup<CombatPrototypePlayerHealth>(true);
            var meleeStates = SystemAPI.GetComponentLookup<CombatPrototypeMeleeState>(true);
            var resources = SystemAPI.GetComponentLookup<CombatPrototypePlayerResource>();
            var inventories = SystemAPI.GetBufferLookup<CombatPrototypeInventoryItem>();
            var identities = SystemAPI.GetComponentLookup<CombatPrototypePlayerIdentity>(true);
            var rewards = SystemAPI.GetComponentLookup<CombatPrototypePlayerReward>(true);
            var ownedTools = SystemAPI.GetBufferLookup<Code_01.CombatPrototype.Map.CombatPrototypeMapGatherTool>(true);

            foreach (var (stream, command, id, connection) in SystemAPI.Query<
                         RefRO<NetworkStreamConnection>, RefRO<CommandTarget>, RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>()
                         .WithEntityAccess())
            {
                var player = command.ValueRO.targetEntity;
                if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !SystemAPI.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                    !SystemAPI.HasComponent<Simulate>(player) ||
                    !SystemAPI.IsComponentEnabled<Simulate>(player))
                    continue;

                var networkId = id.ValueRO.Value;
                var stage = "ReadInput";
                try
                {
                    if (!inputs[player].UseItem.IsSet)
                        continue;
                    stage = "ValidatePlayer";
                    if (owners[player].NetworkId != networkId)
                    {
                        Debug.LogWarning($"[CombatPrototype.NetCode] Server item use rejected; NetworkId={networkId}, player={player}, reason=CommandTargetOwnerMismatch.");
                        continue;
                    }
                    if (healths[player].IsDead != 0)
                    {
                        Debug.Log($"[CombatPrototype.NetCode] Server item use rejected; NetworkId={networkId}, player={player}, reason=PlayerDead.");
                        continue;
                    }
                    if (meleeStates[player].Phase != CombatPrototypeAttackPhase.Ready)
                    {
                        Debug.Log($"[CombatPrototype.NetCode] Server item use rejected; NetworkId={networkId}, player={player}, reason=AttackInProgress.");
                        continue;
                    }

                    // Acquire the component reference and inventory before saving either result.
                    var resource = resources.GetRefRW(player);
                    if (resource.ValueRO.CurrentPower >= resource.ValueRO.UpperPower)
                    {
                        Debug.Log($"[CombatPrototype.NetCode] Server item use rejected; NetworkId={networkId}, player={player}, reason=PowerFull, power={resource.ValueRO.CurrentPower}/{resource.ValueRO.UpperPower}.");
                        continue;
                    }
                    var inventory = inventories[player];
                    var itemIndex = -1;
                    for (var index = 0; index < inventory.Length; index++)
                    {
                        if (!inventory[index].ItemName.Equals(_itemName))
                            continue;
                        itemIndex = index;
                        break;
                    }
                    if (itemIndex < 0 || inventory[itemIndex].Quantity < 1)
                    {
                        Debug.Log($"[CombatPrototype.NetCode] Server item use rejected; NetworkId={networkId}, player={player}, reason=InsufficientItem, item={_itemName}.");
                        continue;
                    }

                    stage = "PrepareConsumption";
                    var playerId = identities[player].PlayerId;
                    var reward = rewards[player];
                    var nextItem = inventory[itemIndex];
                    nextItem.Quantity--;
                    var recoveredPower = global::System.Math.Min(PowerRecovery,
                        resource.ValueRO.UpperPower - resource.ValueRO.CurrentPower);
                    var nextPower = resource.ValueRO.CurrentPower + recoveredPower;
                    var candidate = CombatPrototypePlayerSaveStore.PrepareItemConsumption(
                        playerId, reward, inventory, ownedTools[player], SystemAPI.GetComponent<CombatPrototypeMapInventoryCapacityLevel>(player).Level, itemIndex, nextItem);

                    stage = "SavePrepared";
                    CombatPrototypePlayerSaveStore.SavePrepared(candidate);

                    // Commit uses acquired references and performs no allocation or structural change.
                    stage = "CommitConsumption";
                    if (nextItem.Quantity == 0)
                        inventory.RemoveAt(itemIndex);
                    else
                        inventory[itemIndex] = nextItem;
                    resource.ValueRW.CurrentPower = nextPower;
                    Debug.Log($"[CombatPrototype.NetCode] Server item use accepted; NetworkId={networkId}, PlayerId={playerId}, player={player}, item={_itemName}, consumed=1, remaining={nextItem.Quantity}, recoveredPower={recoveredPower}, power={nextPower}/{resource.ValueRO.UpperPower}.");
                }
                catch (global::System.Exception exception)
                {
                    Debug.LogError($"[CombatPrototype.NetCode] Server item use failed; connection={connection}, NetworkId={networkId}, player={player}, item={_itemName}, stage={stage}; {exception}");
                }
            }
        }
    }
}
