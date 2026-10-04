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
    [UpdateAfter(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapPickupHudStateSystem : SystemBase
    {
        private EntityQuery _drops;
        private readonly Dictionary<Entity, CombatPrototypeMapPickupHudState> _frames =
            new Dictionary<Entity, CombatPrototypeMapPickupHudState>();

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _drops = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapDropState>(),
                ComponentType.ReadOnly<CombatPrototypeMapDropProgress>(), ComponentType.ReadOnly<LocalTransform>());
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            _frames.Clear();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapPickupHudSettings>(source);
            var highlight = EntityManager.GetComponentData<CombatPrototypeMapInteractionHighlightSettings>(source);
            if (settings.Enabled == 0 && (highlight.Enabled == 0 || highlight.GTargetsEnabled == 0))
            {
                CommitFrames();
                return;
            }

            var dropSettings = EntityManager.GetComponentData<CombatPrototypeMapDropSettings>(source);
            var time = SystemAPI.Time.ElapsedTime;
            using var drops = _drops.ToEntityArray(Allocator.Temp);
            var inputs = SystemAPI.GetComponentLookup<CombatPrototypePlayerInput>(true);
            var owners = SystemAPI.GetComponentLookup<GhostOwner>(true);
            var healths = SystemAPI.GetComponentLookup<CombatPrototypePlayerHealth>(true);
            var melees = SystemAPI.GetComponentLookup<CombatPrototypeMeleeState>(true);
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var states = SystemAPI.GetComponentLookup<CombatPrototypeMapDropState>(true);
            var progresses = SystemAPI.GetComponentLookup<CombatPrototypeMapDropProgress>(true);
            foreach (var (stream, command, id) in SystemAPI.Query<RefRO<NetworkStreamConnection>,
                         RefRO<CommandTarget>, RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>())
            {
                var player = command.ValueRO.targetEntity;
                if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !EntityManager.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                    !EntityManager.HasComponent<Simulate>(player) || !EntityManager.IsComponentEnabled<Simulate>(player))
                    continue;
                var stage = "ValidatePlayer";
                var dropId = 0;
                try
                {
                    var rejection = CombatPrototypeMapDropPickupSystem.GetPickupHintRejection(player,
                        id.ValueRO.Value, inputs[player], owners, healths, melees);
                    // A foreign connection must not replace the actual owner's display state.
                    if (rejection == "CommandTargetOwnerMismatch") continue;
                    var frame = CombatPrototypeMapPickupHudState.Hidden;
                    if (rejection == null)
                    {
                        stage = "SelectDrop";
                        var target = CombatPrototypeMapDropTargetSelector.Select(transforms[player].Position.xz,
                            dropSettings.PickupDistance, time, drops, states, progresses, transforms);
                        if (target != Entity.Null)
                        {
                            var drop = states[target];
                            dropId = drop.DropId;
                            frame = new CombatPrototypeMapPickupHudState
                            {
                                Mode = CombatPrototypeMapPickupHudMode.Ready, DropId = dropId,
                                ItemId = drop.ItemId, Quantity = drop.Quantity
                            };
                        }
                    }
                    _frames[player] = frame;
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Pickup HUD frame failed; stage=" + stage +
                        ", map=" + map.MapDefinitionId + ", NetworkId=" + id.ValueRO.Value +
                        ", player=" + player + ", DropId=" + dropId + ". " + exception);
                }
            }
            CommitFrames();
        }

        private void CommitFrames()
        {
            foreach (var (current, entity) in SystemAPI.Query<RefRW<CombatPrototypeMapPickupHudState>>()
                         .WithAll<CombatPrototypePlayerNetCode>().WithEntityAccess())
            {
                var next = _frames.TryGetValue(entity, out var frame) ? frame : CombatPrototypeMapPickupHudState.Hidden;
                var old = current.ValueRO;
                if (old.Mode != next.Mode || old.DropId != next.DropId || !old.ItemId.Equals(next.ItemId) ||
                    old.Quantity != next.Quantity) current.ValueRW = next;
            }
        }

        protected override void OnStopRunning()
        {
            Dependency.Complete();
            _frames.Clear();
            CommitFrames();
        }
    }
}
