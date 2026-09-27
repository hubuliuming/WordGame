using UnityEngine;
using UnityEngine.InputSystem;

namespace Code_01.CombatPrototype
{
    public sealed class CombatPrototypeOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private float distance = 8f;
        [SerializeField] private float height = 4f;
        [SerializeField] private float rotationSpeed = 0.15f;
        [SerializeField] private float minPitch = 15f;
        [SerializeField] private float maxPitch = 70f;

        private float _yaw;
        private float _pitch = 35f;

        private void Awake()
        {
            Debug.Assert(followTarget != null, "CombatPrototypeOrbitCamera requires followTarget.");
            _yaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                _yaw += delta.x * rotationSpeed;
                _pitch = Mathf.Clamp(_pitch - delta.y * rotationSpeed, minPitch, maxPitch);
            }

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.SetPositionAndRotation(followTarget.position + rotation * new Vector3(0f, 0f, -distance) + Vector3.up * height, rotation);
        }
    }
}
