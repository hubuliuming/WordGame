using System;
using Code_01.CombatPrototype.Networking;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.InputSystem;

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
        private CombatPrototypeMapInteractionHighlightTargetResolver _highlightTargets;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypeMapData>();
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _map = GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapData>());
            _highlightTargets = new CombatPrototypeMapInteractionHighlightTargetResolver(
                GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapGatherState>()),
                GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapTreeState>()),
                GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapMineState>()),
                GetEntityQuery(ComponentType.ReadOnly<CombatPrototypeMapDropState>()));
        }

        public void RegisterHud(CombatPrototypeMapInteractionHud hud)
        {
            if (_hud != null && _hud != hud)
                throw new InvalidOperationException("[CombatPrototype.Map] World=" + World.Name + " already has a registered HUD.");
            _hud = hud;
            _highlightTargets.Reset();
            _source = Entity.Null;
            _player = Entity.Null;
            _hud.Reset();
        }

        public void UnregisterHud(CombatPrototypeMapInteractionHud hud)
        {
            if (_hud != hud) return;
            _hud.Reset();
            _highlightTargets.Reset();
            _hud = null;
            _source = Entity.Null;
            _player = Entity.Null;
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            if (_hud != null) _hud.Clear();
            var source = _map.GetSingletonEntity();
            var map = EntityManager.GetComponentData<CombatPrototypeMapData>(source);
            var settings = EntityManager.GetComponentData<CombatPrototypeMapInteractionHudSettings>(source);
            var inventorySettings = EntityManager.GetComponentData<CombatPrototypeMapInventoryPanelSettings>(source);
            var pickupSettings = EntityManager.GetComponentData<CombatPrototypeMapPickupHudSettings>(source);
            var highlightSettings = EntityManager.GetComponentData<CombatPrototypeMapInteractionHighlightSettings>(source);
            var resourceStatusSettings = EntityManager.GetComponentData<CombatPrototypeMapResourceStatusHudSettings>(source);
            var worldSaveSettings = EntityManager.GetComponentData<CombatPrototypeMapWorldSaveHudSettings>(source);
            var hasHighlight = highlightSettings.Enabled != 0 &&
                (highlightSettings.FTargetsEnabled != 0 || highlightSettings.GTargetsEnabled != 0);
            if (settings.Enabled == 0 && inventorySettings.Enabled == 0 && pickupSettings.Enabled == 0 && !hasHighlight &&
                resourceStatusSettings.Enabled == 0 && worldSaveSettings.Enabled == 0)
            {
                ResetBinding();
                return;
            }
            var player = GetLocalPlayer();
            if (player == Entity.Null || !HasInGameConnection(player)) { ResetBinding(); return; }
            if (_hud == null)
                throw new InvalidOperationException("[CombatPrototype.Map] World=" + World.Name + ", player=" + player +
                    " requires the HUD component registered by Main Camera.");
            if (EntityManager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0) { ResetBinding(); return; }
            if (source != _source || player != _player)
            {
                _highlightTargets.Reset();
                var toolSettings = EntityManager.GetComponentData<CombatPrototypeMapGatherToolSettings>(source);
                var definitions = EntityManager.GetBuffer<CombatPrototypeMapGatherToolDefinition>(source, true);
                _hud.Configure(settings, inventorySettings, toolSettings,
                    CombatPrototypeMapGatherToolUtility.RequireDefinition(definitions, CombatPrototypeMapGatherToolKind.Axe),
                    CombatPrototypeMapGatherToolUtility.RequireDefinition(definitions, CombatPrototypeMapGatherToolKind.Pickaxe),
                    EntityManager.GetComponentData<CombatPrototypeMapInventoryDropSettings>(source),
                    EntityManager.GetBuffer<CombatPrototypeMapInventoryDropDefinition>(source, true), pickupSettings,
                    highlightSettings, resourceStatusSettings, worldSaveSettings,
                    EntityManager.GetComponentData<CombatPrototypeMapResourcePersistenceSettings>(source), map.MapDefinitionId.ToString());
                _source = source;
                _player = player;
            }
            var interaction = EntityManager.GetComponentData<CombatPrototypeMapInteractionHudState>(player);
            var pickup = EntityManager.GetComponentData<CombatPrototypeMapPickupHudState>(player);
            _hud.Show(interaction,
                EntityManager.GetBuffer<CombatPrototypeMapGatherTool>(player, true),
                EntityManager.GetComponentData<CombatPrototypeMapToolCraftFeedback>(player),
                EntityManager.GetComponentData<CombatPrototypeMapToolRepairFeedback>(player),
                EntityManager.GetComponentData<CombatPrototypeMapInventoryDropFeedback>(player),
                EntityManager.GetBuffer<CombatPrototypeInventoryItem>(player, true), source, player,
                pickup);
            _highlightTargets.Resolve(EntityManager, World.Name, map, highlightSettings,
                EntityManager.GetComponentData<GhostOwner>(player).NetworkId, interaction, pickup, out var f, out var g);
            _hud.ShowHighlight(f, g);
            _hud.ShowResourceStatus(EntityManager.GetComponentData<CombatPrototypeMapResourceStatusHudState>(player));
            _hud.ShowWorldSave(EntityManager.GetComponentData<CombatPrototypeMapWorldSaveHudState>(player));
        }

        internal bool ReadPanelInput(Keyboard keyboard, Mouse mouse, out bool craftAxe, out bool craftPickaxe, out bool repairAxe, out bool repairPickaxe,
            out CombatPrototypeMapInventoryDropRequest dropRequest)
        {
            craftAxe = craftPickaxe = repairAxe = repairPickaxe = false;
            dropRequest = default;
            Dependency.Complete();
            if (_map.IsEmptyIgnoreFilter) { ResetBinding(); return false; }
            var source = _map.GetSingletonEntity();
            var player = GetLocalPlayer();
            if (player == Entity.Null || !HasInGameConnection(player) ||
                EntityManager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0 ||
                source != _source || player != _player)
            {
                ResetBinding();
                return false;
            }
            if (_hud == null)
                throw new InvalidOperationException("[CombatPrototype.Map] World=" + World.Name +
                    ", player=" + player + " requires the Main Camera HUD for panel input.");
            return _hud.ReadPanelInput(keyboard, mouse, out craftAxe, out craftPickaxe, out repairAxe, out repairPickaxe, out dropRequest);
        }

        private bool HasInGameConnection(Entity player)
        {
            var owner = EntityManager.GetComponentData<GhostOwner>(player).NetworkId;
            foreach (var (stream, id) in SystemAPI.Query<RefRO<NetworkStreamConnection>, RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>().WithNone<NetworkStreamRequestDisconnect>())
                if (stream.ValueRO.CurrentState == ConnectionState.State.Connected && id.ValueRO.Value == owner) return true;
            return false;
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
            _highlightTargets.Reset();
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
