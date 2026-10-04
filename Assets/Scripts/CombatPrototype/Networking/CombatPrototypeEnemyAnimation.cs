using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [RequireComponent(typeof(Animator))]
    public sealed class CombatPrototypeEnemyAnimation : MonoBehaviour
    {
        [SerializeField] private Animator characterAnimator;
        private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
        private static readonly int Move = Animator.StringToHash("Base Layer.Move");
        private static readonly int Startup = Animator.StringToHash("Base Layer.Startup");
        private static readonly int Strike = Animator.StringToHash("Base Layer.Strike");
        private static readonly int Recovery = Animator.StringToHash("Base Layer.Recovery");
        private static readonly int Hit = Animator.StringToHash("Base Layer.Hit");
        private static readonly int Death = Animator.StringToHash("Base Layer.Death");
        private const float HitSeconds = 0.16f;
        private const float DeathSeconds = 0.75f;
        private const float StrikeRecoveryFraction = 0.12f;
        private int _state;
        private uint _hitSequence;
        private bool _initialized;
        private bool _wasDead;
        private float _hitRemaining;
        private float _deathRemaining;
        private bool _hasAttackSample;
        private CombatPrototypeEnemyAttackPhase _samplePhase;
        private uint _sampleAttackSequence;
        private float _sampleRemaining;
        private float _sampleElapsed;
        private float _startupProgress;
        private float _cancelledStartupProgress;
        private CombatPrototypeEnemyAttackPhase _observedPhase;
        private uint _observedAttackSequence;

        public void Present(bool moving, in CombatPrototypeEnemyAnimationState attack, uint hitSequence, bool dead)
        {
            var first = !_initialized;
            var newHit = !first && hitSequence != _hitSequence;
            _initialized = true;
            _hitSequence = hitSequence;

            if (dead)
            {
                _hitRemaining = 0f;
                if (first)
                {
                    // Joining observers must not replay a death that happened before their view existed.
                    gameObject.SetActive(false);
                    _wasDead = true;
                    return;
                }
                if (!_wasDead)
                    _deathRemaining = DeathSeconds;
                _wasDead = true;
                if (_deathRemaining > 0f)
                {
                    PresentPose(Death, 1f - _deathRemaining / DeathSeconds);
                    _deathRemaining = Mathf.Max(0f, _deathRemaining - Time.deltaTime);
                }
                else
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            var remaining = ObserveRemaining(in attack);
            if (attack.Phase == CombatPrototypeEnemyAttackPhase.Startup)
                _startupProgress = Mathf.Clamp01(1f - remaining / attack.StartupSeconds);
            if (attack.Phase == CombatPrototypeEnemyAttackPhase.Recovery && attack.SwingStarted == 0 &&
                (first || _observedPhase != attack.Phase || _observedAttackSequence != attack.AttackSequence))
                _cancelledStartupProgress = first ? 1f : _startupProgress;
            _observedPhase = attack.Phase;
            _observedAttackSequence = attack.AttackSequence;
            if (newHit)
                _hitRemaining = HitSeconds;
            if (_hitRemaining > 0f)
            {
                PresentPose(Hit, 1f - _hitRemaining / HitSeconds);
                _hitRemaining = Mathf.Max(0f, _hitRemaining - Time.deltaTime);
                return;
            }

            switch (attack.Phase)
            {
                case CombatPrototypeEnemyAttackPhase.Startup:
                    PresentPose(Startup, _startupProgress);
                    break;
                case CombatPrototypeEnemyAttackPhase.Recovery:
                    var progress = Mathf.Clamp01(1f - remaining / attack.RecoverySeconds);
                    if (attack.SwingStarted == 0)
                        PresentPose(Startup, _cancelledStartupProgress * (1f - progress));
                    else if (progress < StrikeRecoveryFraction)
                        PresentPose(Strike, progress / StrikeRecoveryFraction);
                    else
                        PresentPose(Recovery, (progress - StrikeRecoveryFraction) / (1f - StrikeRecoveryFraction));
                    break;
                default:
                    var state = moving ? Move : Idle;
                    characterAnimator.speed = 1f;
                    if (_state != state)
                    {
                        if (first)
                            characterAnimator.Play(state, 0, 0f);
                        else
                            characterAnimator.CrossFadeInFixedTime(state, 0.1f, 0);
                        _state = state;
                    }
                    break;
            }
        }

        private float ObserveRemaining(in CombatPrototypeEnemyAnimationState attack)
        {
            if (!_hasAttackSample || attack.Phase != _samplePhase ||
                attack.AttackSequence != _sampleAttackSequence || attack.PhaseRemaining != _sampleRemaining)
            {
                _hasAttackSample = true;
                _samplePhase = attack.Phase;
                _sampleAttackSequence = attack.AttackSequence;
                _sampleRemaining = attack.PhaseRemaining;
                _sampleElapsed = 0f;
            }
            else
            {
                _sampleElapsed += Time.deltaTime;
            }
            // Advance the pose between snapshots, without advancing any gameplay state.
            return Mathf.Max(0f, _sampleRemaining - _sampleElapsed);
        }

        private void PresentPose(int state, float normalizedTime)
        {
            characterAnimator.speed = 0f;
            characterAnimator.Play(state, 0, Mathf.Clamp01(normalizedTime));
            characterAnimator.Update(0f);
            _state = state;
        }
    }
}
