using Code_01.CombatPrototype.Map;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    public sealed class CombatPrototypePlayerNetCodeAuthoring : MonoBehaviour
    {
        public float MoveSpeed = 5f;
        public float InitialHealth = 100f;
        public float MaxHealth = 100f;
        public int InitialPower = 100;
        public int UpperPower = 100;
        public int AttackPowerCost = 10;
        public float Damage = 25f;
        public float Range = 2f;
        public float Angle = 100f;
        public float StartupSeconds = 0.18f;
        public float ActiveSeconds = 0.08f;
        public float RecoverySeconds = 0.3f;

        private sealed class Baker : Baker<CombatPrototypePlayerNetCodeAuthoring>
        {
            public override void Bake(CombatPrototypePlayerNetCodeAuthoring authoring)
            {
                if (!math.isfinite(authoring.InitialHealth) || !math.isfinite(authoring.MaxHealth) ||
                    authoring.InitialHealth <= 0f || authoring.MaxHealth <= 0f ||
                    authoring.InitialHealth > authoring.MaxHealth)
                    throw new global::System.InvalidOperationException(
                        "[CombatPrototype.NetCode] Player health configuration requires finite 0 < InitialHealth <= MaxHealth.");

                if (authoring.UpperPower < 0 || authoring.InitialPower < 0 ||
                    authoring.InitialPower > authoring.UpperPower || authoring.AttackPowerCost < 0)
                    throw new global::System.InvalidOperationException(
                        "[CombatPrototype.NetCode] Player power configuration requires 0 <= InitialPower <= UpperPower and AttackPowerCost >= 0.");

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CombatPrototypePlayerNetCode { MoveSpeed = authoring.MoveSpeed });
                AddComponent<CombatPrototypePlayerInput>(entity);
                AddComponent(entity, new CombatPrototypePlayerHealth
                {
                    CurrentHealth = authoring.InitialHealth,
                    MaxHealth = authoring.MaxHealth
                });
                AddBuffer<CombatPrototypePlayerDamageEvent>(entity);
                AddComponent<CombatPrototypePlayerReward>(entity);
                AddBuffer<CombatPrototypeInventoryItem>(entity);
                AddBuffer<CombatPrototypeMapGatherTool>(entity);
                AddComponent<CombatPrototypeMapToolCraftFeedback>(entity);
                AddComponent<CombatPrototypeMapToolRepairFeedback>(entity);
                AddComponent<CombatPrototypeMapInventoryDropFeedback>(entity);
                AddComponent(entity, new CombatPrototypeMapInventoryCapacityLevel { Level = 1 });
                AddComponent<CombatPrototypeMapInventoryCapacityUpgradeFeedback>(entity);
                AddComponent<CombatPrototypeMapToolUpgradeFeedback>(entity);
                AddComponent(entity, new CombatPrototypePlayerResource
                {
                    CurrentPower = authoring.InitialPower,
                    UpperPower = authoring.UpperPower
                });
                AddComponent(entity, new CombatPrototypeMeleeConfig
                {
                    AttackPowerCost = authoring.AttackPowerCost,
                    Damage = authoring.Damage,
                    Range = authoring.Range,
                    Angle = authoring.Angle,
                    StartupSeconds = authoring.StartupSeconds,
                    ActiveSeconds = authoring.ActiveSeconds,
                    RecoverySeconds = authoring.RecoverySeconds
                });
                AddComponent<CombatPrototypeMeleeState>(entity);
                AddComponent(entity, CombatPrototypeMapInteractionHudState.Hidden);
                AddComponent(entity, CombatPrototypeMapPickupHudState.Hidden);
                AddComponent(entity, CombatPrototypeMapResourceStatusHudState.Hidden);
                AddComponent(entity, CombatPrototypeMapWorldSaveHudState.Hidden);
            }
        }
    }
}
