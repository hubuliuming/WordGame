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
        public InputEvent Gather;
        public InputEvent Pickup;
        public InputEvent HarvestTree;
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
            var cameraBinding = state.World.GetExistingSystemManaged<CombatPrototypeCameraBindingSystem>();
            move = cameraBinding.ReadMove(move, keyboard, mouse);
            var attack = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                         (mouse != null && mouse.leftButton.wasPressedThisFrame);
            var respawn = keyboard != null && keyboard.rKey.wasPressedThisFrame;
            var useItem = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            var gather = keyboard != null && keyboard.fKey.wasPressedThisFrame;
            var pickup = keyboard != null && keyboard.gKey.wasPressedThisFrame;
            var harvestTree = keyboard != null && keyboard.hKey.wasPressedThisFrame;

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
                if (harvestTree)
                    input.ValueRW.HarvestTree.Set();
            }
        }
    }
}
