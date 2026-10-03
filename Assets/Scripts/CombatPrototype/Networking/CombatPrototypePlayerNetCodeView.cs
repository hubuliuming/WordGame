using Unity.NetCode.Hybrid;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [RequireComponent(typeof(GhostPresentationGameObjectEntityOwner))]
    public sealed class CombatPrototypePlayerNetCodeView : MonoBehaviour
    {
        [SerializeField] private CombatPrototypePlayerAnimation playerAnimation;
        private GhostPresentationGameObjectEntityOwner _owner;
        private GhostPresentationGameObjectTransformSystem _transformSystem;
        private Vector3 _previousPosition;
        private bool _hasPosition;
        private bool _wasDead;

        private void Start()
        {
            _owner = GetComponent<GhostPresentationGameObjectEntityOwner>();
            _transformSystem = _owner.World.GetExistingSystemManaged<GhostPresentationGameObjectTransformSystem>();
        }

        private void LateUpdate()
        {
            // The bridge can destroy the entity/world before its presentation object is released.
            var world = _owner.World;
            if (!world.IsCreated || !world.EntityManager.Exists(_owner.Entity))
                return;

            var manager = world.EntityManager;
            var attack = manager.GetComponentData<CombatPrototypeMeleeState>(_owner.Entity);
            var health = manager.GetComponentData<CombatPrototypePlayerHealth>(_owner.Entity);
            _transformSystem.CheckedStateRef.CompleteDependency();
            var position = transform.position;
            var displacement = position - _previousPosition;
            displacement.y = 0f;
            var dead = health.IsDead != 0;
            // A new view or the existing respawn teleport is not a locomotion step.
            var moving = _hasPosition && !dead && !_wasDead &&
                         displacement.sqrMagnitude > 0.0004f * Time.deltaTime * Time.deltaTime;
            playerAnimation.Present(moving, attack.Phase, attack.AttackSequence, health.HitSequence, dead);
            _previousPosition = position;
            _hasPosition = true;
            _wasDead = dead;
        }
    }
}