using System;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Map
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class CombatPrototypeMapInteractionHudBindingSystem : SystemBase
    {
        private EntityQuery _map;
        private CombatPrototypeMapInteractionHud _hud;
        private Entity _source;
        private Entity _player;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _map = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapData>());
        }

        public void RegisterHud(CombatPrototypeMapInteractionHud hud)
        {
            if (_hud != null && _hud != hud)
                throw new InvalidOperationException("[CombatPrototype.Map] World=" + World.Name + " already has a registered HUD.");
            _hud = hud;
            _source = Entity.Null;
            _player = Entity.Null;
            _hud.Reset();
        }

        public void UnregisterHud(CombatPrototypeMapInteractionHud hud)
        {
            if (_hud != hud) return;
            _hud.Reset();
            _hud = null;
            _source = Entity.Null;
            _player = Entity.Null;
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            if (_hud != null) _hud.Clear();
            var source = _map.GetSingletonEntity();
            var settings = EntityManager.GetComponentData<CombatPrototypeMapInteractionHudSettings>(source);
            if (settings.Enabled == 0)
            {
                ResetBinding();
                return;
            }
            var player = GetLocalPlayer();
            if (player == Entity.Null) { ResetBinding(); return; }
            if (_hud == null)
                throw new InvalidOperationException("[CombatPrototype.Map] World=" + World.Name + ", player=" + player +
                    " requires the HUD component registered by Main Camera.");
            if (EntityManager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0) { ResetBinding(); return; }
            if (source != _source || player != _player)
            {
                var toolSettings = EntityManager.GetComponentData<CombatPrototypeMapGatherToolSettings>(source);
                var definitions = EntityManager.GetBuffer<CombatPrototypeMapGatherToolDefinition>(source, true);
                _hud.Configure(settings, toolSettings,
                    CombatPrototypeMapGatherToolUtility.RequireDefinition(definitions, CombatPrototypeMapGatherToolKind.Axe),
                    CombatPrototypeMapGatherToolUtility.RequireDefinition(definitions, CombatPrototypeMapGatherToolKind.Pickaxe));
                _source = source;
                _player = player;
            }
            _hud.Show(EntityManager.GetComponentData<CombatPrototypeMapInteractionHudState>(player),
                EntityManager.GetBuffer<CombatPrototypeMapGatherTool>(player, true),
                EntityManager.GetComponentData<CombatPrototypeMapToolCraftFeedback>(player));
        }

        private Entity GetLocalPlayer()
        {
            var player = Entity.Null;
            // GhostOwnerIsLocal is enableable: enumerate its enabled matches instead of using a singleton.
            foreach (var (_, entity) in SystemAPI.Query<RefRO<CombatPrototypePlayerNetCode>>()
                         .WithAll<GhostOwnerIsLocal>().WithEntityAccess())
            {
                if (player != Entity.Null)
                    throw new InvalidOperationException("[CombatPrototype.Map] World=" + World.Name + " requires one local HUD player.");
                player = entity;
            }
            return player;
        }

        private void ResetBinding()
        {
            if (_hud != null) _hud.Reset();
            _source = Entity.Null;
            _player = Entity.Null;
        }

        protected override void OnStopRunning()
        {
            ResetBinding();
        }

        protected override void OnDestroy()
        {
            ResetBinding();
            _hud = null;
        }
    }
}
