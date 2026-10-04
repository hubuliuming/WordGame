using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatPrototypePlayerRespawnSystem))]
    public partial struct CombatPrototypeEnemyAnimationSyncSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Include the existing respawn cancellation before publishing presentation state.
            foreach (var (attack, animation) in SystemAPI.Query<
                         RefRO<CombatPrototypeEnemyAttackState>, RefRW<CombatPrototypeEnemyAnimationState>>())
            {
                animation.ValueRW.Phase = attack.ValueRO.Phase;
                animation.ValueRW.AttackSequence = attack.ValueRO.AttackSequence;
                animation.ValueRW.SwingStarted = attack.ValueRO.SwingStarted;
                animation.ValueRW.PhaseRemaining = math.max(0f, attack.ValueRO.PhaseTimer);
            }
        }
    }
}
