using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [RequireComponent(typeof(Animator))]
    public sealed class CombatPrototypePlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator characterAnimator;
        private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
        private static readonly int Move = Animator.StringToHash("Base Layer.Move");
        private static readonly int Death = Animator.StringToHash("Base Layer.Death");
        private static readonly int Startup = Animator.StringToHash("Upper Body.Startup");
        private static readonly int Active = Animator.StringToHash("Upper Body.Active");
        private static readonly int Recovery = Animator.StringToHash("Upper Body.Recovery");
        private static readonly int Hit = Animator.StringToHash("Upper Body.Hit");
        private const float HitSeconds = 0.16f;
        private int _baseState;
        private int _actionState;
        private uint _attackSequence;
        private uint _hitSequence;
        private float _hitRemaining;
        private bool _initialized;
        private bool _wasDead;
        private bool _actionEnabled;

        public void Present(bool moving, CombatPrototypeAttackPhase phase, uint attackSequence,
            uint hitSequence, bool dead)
        {
            var first = !_initialized;
            var revived = !first && _wasDead && !dead;
            var newAttack = !first && !revived && attackSequence != _attackSequence;
            var newHit = !first && !revived && hitSequence != _hitSequence;
            _initialized = true;
            _attackSequence = attackSequence;
            _hitSequence = hitSequence;

            if (first)
                characterAnimator.SetLayerWeight(1, 0f);

            if (dead)
            {
                SetActionEnabled(false);
                _hitRemaining = 0f;
                if (first || !_wasDead)
                {
                    // A newly joined observer sees the final corpse pose, not a replay of old death.
                    characterAnimator.Play(Death, 0, first ? 1f : 0f);
                    _baseState = Death;
                }
                _wasDead = true;
                return;
            }

            _wasDead = false;
            var baseState = moving ? Move : Idle;
            if (first || revived)
            {
                _hitRemaining = 0f;
                _actionState = 0;
                SetActionEnabled(false);
                characterAnimator.Play(baseState, 0, 0f);
                _baseState = baseState;
            }
            else if (_baseState != baseState)
            {
                characterAnimator.CrossFadeInFixedTime(baseState, 0.1f, 0);
                _baseState = baseState;
            }

            if (newHit)
                _hitRemaining = HitSeconds;
            if (_hitRemaining > 0f)
            {
                PlayAction(Hit, newHit);
                _hitRemaining = Mathf.Max(0f, _hitRemaining - Time.deltaTime);
                return;
            }

            // Only received combat state drives actions; rejected inputs cannot play an attack.
            switch (phase)
            {
                case CombatPrototypeAttackPhase.Ready:
                    SetActionEnabled(false);
                    _actionState = 0;
                    break;
                case CombatPrototypeAttackPhase.Startup:
                    PlayAction(Startup, newAttack);
                    break;
                case CombatPrototypeAttackPhase.Active:
                    PlayAction(Active, newAttack);
                    break;
                case CombatPrototypeAttackPhase.Recovery:
                    PlayAction(Recovery, newAttack);
                    break;
            }
        }

        private void PlayAction(int state, bool restart)
        {
            SetActionEnabled(true);
            if (_actionState == state && !restart)
                return;
            characterAnimator.Play(state, 1, 0f);
            _actionState = state;
        }

        private void SetActionEnabled(bool enabled)
        {
            if (_actionEnabled == enabled)
                return;
            characterAnimator.SetLayerWeight(1, enabled ? 1f : 0f);
            _actionEnabled = enabled;
        }
    }
}