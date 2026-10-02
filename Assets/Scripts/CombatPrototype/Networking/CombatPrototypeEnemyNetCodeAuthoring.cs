using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    public sealed class CombatPrototypeEnemyNetCodeAuthoring : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float MoveSpeed = 2f;
        public float StopDistance = 1.5f;
        public float AttackDamage = 10f;
        public float AttackRange = 1.75f;
        public float AttackStartupSeconds = 0.5f;
        public float AttackRecoverySeconds = 1f;
        public int RewardCoin = 1;
        public int RewardExperience = 10;
        public string RewardItemName = Msg.ItemName.小块肉;
        public int RewardItemQuantity = 1;

        private sealed class Baker : Baker<CombatPrototypeEnemyNetCodeAuthoring>
        {
            public override void Bake(CombatPrototypeEnemyNetCodeAuthoring authoring)
            {
                if (!math.isfinite(authoring.AttackDamage) || authoring.AttackDamage <= 0f ||
                    !math.isfinite(authoring.AttackRange) || authoring.AttackRange <= 0f ||
                    !math.isfinite(authoring.AttackStartupSeconds) || authoring.AttackStartupSeconds <= 0f ||
                    !math.isfinite(authoring.AttackRecoverySeconds) || authoring.AttackRecoverySeconds <= 0f ||
                    !math.isfinite(authoring.StopDistance) || authoring.StopDistance < 0f ||
                    authoring.AttackRange < authoring.StopDistance)
                    throw new global::System.InvalidOperationException(
                        "[CombatPrototype.NetCode] Enemy attack configuration requires finite positive damage, range and durations, with 0 <= StopDistance <= AttackRange.");

                if (authoring.RewardCoin < 0 || authoring.RewardExperience < 0 ||
                    string.IsNullOrWhiteSpace(authoring.RewardItemName) || authoring.RewardItemQuantity <= 0)
                    throw new global::System.InvalidOperationException(
                        "[CombatPrototype.NetCode] Enemy reward configuration requires nonnegative Coin/Experience, a nonempty item name and positive item quantity.");

                var itemName = new FixedString64Bytes(authoring.RewardItemName);

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CombatPrototypeEnemyMovement
                {
                    MoveSpeed = authoring.MoveSpeed,
                    StopDistance = authoring.StopDistance
                });
                AddComponent<CombatPrototypeEnemyTarget>(entity);
                AddComponent(entity, new CombatPrototypeEnemyAttackConfig
                {
                    Damage = authoring.AttackDamage,
                    Range = authoring.AttackRange,
                    StartupSeconds = authoring.AttackStartupSeconds,
                    RecoverySeconds = authoring.AttackRecoverySeconds
                });
                AddComponent<CombatPrototypeEnemyAttackState>(entity);
                AddBuffer<CombatPrototypeDamageEvent>(entity);
                AddComponent(entity, new CombatPrototypeKillRewardConfig
                {
                    Coin = authoring.RewardCoin,
                    Experience = authoring.RewardExperience,
                    ItemName = itemName,
                    ItemQuantity = authoring.RewardItemQuantity
                });
                AddBuffer<CombatPrototypeKillRewardEvent>(entity);
                AddComponent(entity, new CombatPrototypeEnemyState
                {
                    CurrentHealth = authoring.MaxHealth
                });
            }
        }
    }

    public struct CombatPrototypeEnemyState : IComponentData
    {
        [GhostField] public float CurrentHealth;
        [GhostField] public uint HitSequence;
        [GhostField] public byte IsDead;
    }
}
