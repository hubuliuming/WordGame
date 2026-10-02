using Unity.NetCode.Hybrid;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    [RequireComponent(typeof(GhostPresentationGameObjectEntityOwner))]
    public sealed class CombatPrototypeEnemyNetCodeView : MonoBehaviour
    {
        [SerializeField] private Renderer bodyRenderer;
        private GhostPresentationGameObjectEntityOwner _owner;

        private void Start()
        {
            _owner = GetComponent<GhostPresentationGameObjectEntityOwner>();
        }

        private void Update()
        {
            // Ghost cleanup and world teardown can precede destruction of the presentation GameObject.
            if (!_owner.World.IsCreated || !_owner.World.EntityManager.HasComponent<CombatPrototypeEnemyState>(_owner.Entity))
                return;
            var health = _owner.World.EntityManager.GetComponentData<CombatPrototypeEnemyState>(_owner.Entity);
            bodyRenderer.enabled = health.IsDead == 0;
        }
    }
}