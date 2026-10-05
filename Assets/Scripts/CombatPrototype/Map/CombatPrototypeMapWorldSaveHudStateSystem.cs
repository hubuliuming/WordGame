using System;
using System.Collections.Generic;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial class CombatPrototypeMapWorldSaveHudStateSystem : SystemBase
    {
        private readonly Dictionary<Entity, CombatPrototypeMapWorldSaveHudState> _frames =
            new Dictionary<Entity, CombatPrototypeMapWorldSaveHudState>();

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            _frames.Clear();
            var source = SystemAPI.GetSingletonEntity<CombatPrototypeMapData>();
            var settings = EntityManager.GetComponentData<CombatPrototypeMapWorldSaveHudSettings>(source);
            if (settings.Enabled == 0) { CommitFrames(); return; }
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var save = World.GetExistingSystemManaged<CombatPrototypeMapResourceSaveSystem>();
            if (save == null) throw new InvalidOperationException("World save HUD requires the authoritative world save service.");
            foreach (var (stream, command, id) in SystemAPI.Query<RefRO<NetworkStreamConnection>, RefRO<CommandTarget>, RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>())
            {
                var player = command.ValueRO.targetEntity;
                if (stream.ValueRO.CurrentState != ConnectionState.State.Connected ||
                    !EntityManager.HasComponent<CombatPrototypePlayerNetCode>(player) ||
                    !EntityManager.HasComponent<Simulate>(player) || !EntityManager.IsComponentEnabled<Simulate>(player)) continue;
                try
                {
                    if (EntityManager.GetComponentData<GhostOwner>(player).NetworkId != id.ValueRO.Value ||
                        EntityManager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0) continue;
                    _frames[player] = save.ReadHud(source, player);
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] World save HUD frame failed; stage=ReadSaveResult, map=" +
                        map.MapDefinitionId + ", NetworkId=" + id.ValueRO.Value + ", player=" + player + ". " + exception);
                }
            }
            CommitFrames();
        }

        private void CommitFrames()
        {
            foreach (var (current, entity) in SystemAPI.Query<RefRW<CombatPrototypeMapWorldSaveHudState>>()
                         .WithAll<CombatPrototypePlayerNetCode>().WithEntityAccess())
            {
                var next = _frames.TryGetValue(entity, out var frame) ? frame : CombatPrototypeMapWorldSaveHudState.Hidden;
                var old = current.ValueRO;
                if (old.Mode != next.Mode || old.ManualSequence != next.ManualSequence || old.ManualResult != next.ManualResult)
                    current.ValueRW = next;
            }
        }

        protected override void OnStopRunning() { _frames.Clear(); CommitFrames(); }
        protected override void OnDestroy() { _frames.Clear(); }
    }
}
