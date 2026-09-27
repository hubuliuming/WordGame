using UnityEngine;
using UnityEngine.InputSystem;

namespace Code_01.CombatPrototype
{
    public sealed class CombatPrototypeMeleeAttack : MonoBehaviour
    {
        [SerializeField] private float damage = 25f;
        [SerializeField] private float range = 2f;
        [SerializeField] private float angle = 100f;
        [SerializeField] private float startupSeconds = 0.18f;
        [SerializeField] private float activeSeconds = 0.08f;
        [SerializeField] private float recoverySeconds = 0.3f;
        [SerializeField] private LayerMask targetMask;

        private readonly Collider[] _hitBuffer = new Collider[16];
        private float _phaseTimer;
        private AttackPhase _phase;

        private enum AttackPhase
        {
            Ready,
            Startup,
            Active,
            Recovery
        }

        private void Update()
        {
            if (_phase == AttackPhase.Ready)
            {
                var mouse = Mouse.current;
                var keyboard = Keyboard.current;
                if ((mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                    (keyboard != null && keyboard.spaceKey.wasPressedThisFrame))
                {
                    _phase = AttackPhase.Startup;
                    _phaseTimer = startupSeconds;
                }
                return;
            }

            _phaseTimer -= Time.deltaTime;
            if (_phaseTimer > 0f)
            {
                return;
            }

            switch (_phase)
            {
                case AttackPhase.Startup:
                    _phase = AttackPhase.Active;
                    _phaseTimer = activeSeconds;
                    ResolveHit();
                    break;
                case AttackPhase.Active:
                    _phase = AttackPhase.Recovery;
                    _phaseTimer = recoverySeconds;
                    break;
                case AttackPhase.Recovery:
                    _phase = AttackPhase.Ready;
                    break;
            }
        }

        private void ResolveHit()
        {
            var count = Physics.OverlapSphereNonAlloc(transform.position, range, _hitBuffer, targetMask);
            var forward = transform.forward;
            for (var i = 0; i < count; i++)
            {
                var target = _hitBuffer[i].GetComponentInParent<CombatPrototypeHealth>();
                if (target == null || target.transform == transform || target.IsDead)
                {
                    continue;
                }

                var direction = target.transform.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > range * range || Vector3.Angle(forward, direction) > angle * 0.5f)
                {
                    continue;
                }

                target.ApplyDamage(damage);
            }
        }
    }
}
