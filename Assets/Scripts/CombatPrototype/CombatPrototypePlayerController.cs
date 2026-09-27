using UnityEngine;
using UnityEngine.InputSystem;

namespace Code_01.CombatPrototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CombatPrototypePlayerController : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _characterController;
        private float _verticalVelocity;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            Debug.Assert(cameraTransform != null, "CombatPrototypePlayerController requires cameraTransform.");
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var input = keyboard == null
                ? Vector2.zero
                : new Vector2((keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                    (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            input = Vector2.ClampMagnitude(input, 1f);

            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            var movement = (forward * input.y + right * input.x) * moveSpeed;

            if (movement.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(movement, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            movement.y = _verticalVelocity;
            _characterController.Move(movement * Time.deltaTime);
        }
    }
}
