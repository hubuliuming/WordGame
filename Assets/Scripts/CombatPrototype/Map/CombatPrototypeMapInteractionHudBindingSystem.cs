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
            _hud.Clear();
        }

        public void UnregisterHud(CombatPrototypeMapInteractionHud hud)
        {
            if (_hud != hud) return;
            _hud.Clear();
            _hud = null;
            _source = Entity.Null;
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            if (_hud != null) _hud.Clear();
            var source = _map.GetSingletonEntity();
            var settings = EntityManager.GetComponentData<CombatPrototypeMapInteractionHudSettings>(source);
            if (settings.Enabled == 0)
            {
                _source = Entity.Null;
                return;
            }
            var player = GetLocalPlayer();
            if (player == Entity.Null) return;
            if (_hud == null)
                throw new InvalidOperationException("[CombatPrototype.Map] World=" + World.Name + ", player=" + player +
                    " requires the HUD component registered by Main Camera.");
            if (EntityManager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0) return;
            if (source != _source)
            {
                _hud.Configure(settings);
                _source = source;
            }
            _hud.Show(EntityManager.GetComponentData<CombatPrototypeMapInteractionHudState>(player));
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

        protected override void OnStopRunning()
        {
            if (_hud != null) _hud.Clear();
            _source = Entity.Null;
        }

        protected override void OnDestroy()
        {
            if (_hud != null) _hud.Clear();
            _hud = null;
            _source = Entity.Null;
        }
    }
}
