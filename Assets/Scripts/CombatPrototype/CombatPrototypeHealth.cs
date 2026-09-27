using UnityEngine;

namespace Code_01.CombatPrototype
{
    public sealed class CombatPrototypeHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath;

        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            if (IsDead)
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            if (destroyOnDeath)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
        }
    }
}
