using Unity.NetCode.Hybrid;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [RequireComponent(typeof(GhostPresentationGameObjectEntityOwner))]
    public sealed class CombatPrototypeEnemyNetCodeView : MonoBehaviour
    {
        [SerializeField] private CombatPrototypeEnemyAnimation enemyAnimation;
        private GhostPresentationGameObjectEntityOwner _owner;
        private GhostPresentationGameObjectTransformSystem _transformSystem;
        private Vector3 _previousPosition;
        private bool _hasPosition;

        private void Start()
        {
            _owner = GetComponent<GhostPresentationGameObjectEntityOwner>();
            _transformSystem = _owner.World.GetExistingSystemManaged<GhostPresentationGameObjectTransformSystem>();
        }

        private void LateUpdate()
        {
            // Ghost cleanup and world teardown can precede destruction of the presentation GameObject.
            var world = _owner.World;
            if (!world.IsCreated || !world.EntityManager.Exists(_owner.Entity))
                return;
            var manager = world.EntityManager;
            var health = manager.GetComponentData<CombatPrototypeEnemyState>(_owner.Entity);
            var attack = manager.GetComponentData<CombatPrototypeEnemyAnimationState>(_owner.Entity);
            _transformSystem.CheckedStateRef.CompleteDependency();
            var position = transform.position;
            var displacement = position - _previousPosition;
            displacement.y = 0f;
            var moving = _hasPosition && health.IsDead == 0 &&
                         displacement.sqrMagnitude > 0.0004f * Time.deltaTime * Time.deltaTime;
            enemyAnimation.Present(moving, in attack, health.HitSequence, health.IsDead != 0);
            _previousPosition = position;
            _hasPosition = true;
        }
    }
}
