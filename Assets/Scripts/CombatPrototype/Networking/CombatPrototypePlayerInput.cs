using System;
using Code_01.CombatPrototype.Map;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine.InputSystem;

namespace Code_01.CombatPrototype.Networking
{
    public struct CombatPrototypePlayerInput : IInputComponentData
    {
        public float2 Move;
        public InputEvent Attack;
        public InputEvent Respawn;
        public InputEvent UseItem;
        public InputEvent Gather; // Unified F resource interaction reuses the original event.
        public InputEvent Pickup;
        public InputEvent SaveWorld;
        public InputEvent UpgradeInventoryCapacity;
        public InputEvent UpgradeAxe;
        public InputEvent UpgradePickaxe;
        public InputEvent CraftAxe;
        public InputEvent CraftPickaxe;
        public InputEvent RepairAxe;
        public InputEvent RepairPickaxe;
        public InputEvent DropInventory;
        public byte InventoryDropItem;
        public byte InventoryDropMode;
        public InputEvent HarvestTree; // Reserved; keyboard H is no longer submitted or consumed.
        public InputEvent Mine; // Reserved; keyboard J is no longer submitted or consumed.
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct CombatPrototypePlayerInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatPrototypePlayerSpawner>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var move = keyboard == null ? float2.zero : new float2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            move = math.normalizesafe(move);
            var panelBinding = state.World.GetExistingSystemManaged<CombatPrototypeMapInteractionHudBindingSystem>();
            if (panelBinding == null)
                throw new InvalidOperationException("Client input requires CombatPrototypeMapInteractionHudBindingSystem.");
            var blocksMouse = panelBinding.ReadPanelInput(keyboard, mouse, out var panelCraftAxe, out var panelCraftPickaxe,
                out var panelRepairAxe, out var panelRepairPickaxe, out var panelDrop, out var panelUpgrade,
                out var panelUpgradeAxe, out var panelUpgradePickaxe);
            var cameraBinding = state.World.GetExistingSystemManaged<CombatPrototypeCameraBindingSystem>();
            move = cameraBinding.ReadMove(move, keyboard, blocksMouse ? null : mouse);
            var attack = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                         (mouse != null && mouse.leftButton.wasPressedThisFrame && !blocksMouse);
            var respawn = keyboard != null && keyboard.rKey.wasPressedThisFrame;
            var useItem = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            var gather = keyboard != null && keyboard.fKey.wasPressedThisFrame;
            var pickup = keyboard != null && keyboard.gKey.wasPressedThisFrame;
            var saveWorld = keyboard != null && keyboard.f5Key.wasPressedThisFrame;
            var upgrade = panelUpgrade || (keyboard != null && keyboard.digit5Key.wasPressedThisFrame);
            var upgradeAxe = panelUpgradeAxe || (keyboard != null && keyboard.digit6Key.wasPressedThisFrame);
            var upgradePickaxe = panelUpgradePickaxe || (keyboard != null && keyboard.digit7Key.wasPressedThisFrame);
            var craftAxe = panelCraftAxe || (keyboard != null && keyboard.digit1Key.wasPressedThisFrame);
            var craftPickaxe = panelCraftPickaxe || (keyboard != null && keyboard.digit2Key.wasPressedThisFrame);
            var repairAxe = panelRepairAxe || (keyboard != null && keyboard.digit3Key.wasPressedThisFrame);
            var repairPickaxe = panelRepairPickaxe || (keyboard != null && keyboard.digit4Key.wasPressedThisFrame);

            foreach (var input in SystemAPI.Query<RefRW<CombatPrototypePlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                input.ValueRW = new CombatPrototypePlayerInput { Move = move };
                if (attack)
                    input.ValueRW.Attack.Set();
                if (respawn)
                    input.ValueRW.Respawn.Set();
                if (useItem)
                    input.ValueRW.UseItem.Set();
                if (gather)
                    input.ValueRW.Gather.Set();
                if (pickup)
                    input.ValueRW.Pickup.Set();
                if (saveWorld) input.ValueRW.SaveWorld.Set();
                if (upgrade) input.ValueRW.UpgradeInventoryCapacity.Set();
                if (upgradeAxe) input.ValueRW.UpgradeAxe.Set();
                if (upgradePickaxe) input.ValueRW.UpgradePickaxe.Set();
                if (craftAxe) input.ValueRW.CraftAxe.Set();
                if (craftPickaxe) input.ValueRW.CraftPickaxe.Set();
                if (repairAxe) input.ValueRW.RepairAxe.Set();
                if (repairPickaxe) input.ValueRW.RepairPickaxe.Set();
                if (panelDrop.Mode != CombatPrototypeMapInventoryDropMode.None)
                {
                    input.ValueRW.InventoryDropItem = (byte)panelDrop.Kind;
                    input.ValueRW.InventoryDropMode = (byte)panelDrop.Mode;
                    input.ValueRW.DropInventory.Set();
                }
            }
        }
    }
}
