using System;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code_01.CombatPrototype.Networking
{
    public sealed class CombatPrototypeFollowCamera : MonoBehaviour
    {
        [SerializeField] private Camera controlledCamera;
        [SerializeField] private float pitch = 40f;
        [SerializeField] private float fieldOfView = 35f;
        [SerializeField] private float targetHeight = 0.5f;
        [SerializeField] private float defaultDistance = 18f;
        [SerializeField] private float minDistance = 12f;
        [SerializeField] private float maxDistance = 26f;
        [SerializeField] private float zoomStep = 1f;
        [SerializeField] private float followSmoothTime = 0.12f;
        [SerializeField] private float rotationStep = 45f;
        [SerializeField] private float rotationDuration = 0.18f;

        private Unity.Entities.World _clientWorld;
        private CombatPrototypeCameraBindingSystem _bindingSystem;
        private Transform _cameraTransform;
        private Vector3 _focusPosition;
        private Vector3 _focusVelocity;
        private float _yaw;
        private float _startYaw;
        private float _targetYaw;
        private float _rotationElapsed;
        private float _distance;
        private float _targetDistance;
        private float _zoomVelocity;
        private int _lastInputFrame = -1;

        private void Awake()
        {
            if (controlledCamera == null || controlledCamera.transform != transform)
                throw new InvalidOperationException("[CombatPrototype.Camera] Main Camera requires an explicit reference to its Camera component.");
            if (!math.all(math.isfinite(new float4(pitch, fieldOfView, targetHeight, defaultDistance))) ||
                !math.all(math.isfinite(new float4(minDistance, maxDistance, zoomStep, followSmoothTime))) ||
                !math.all(math.isfinite(new float2(rotationStep, rotationDuration))) ||
                pitch <= 0f || pitch >= 90f || fieldOfView <= 0f || fieldOfView >= 180f ||
                minDistance <= 0f || maxDistance < minDistance ||
                defaultDistance < minDistance || defaultDistance > maxDistance ||
                zoomStep <= 0f || followSmoothTime <= 0f || rotationStep <= 0f || rotationDuration <= 0f)
                throw new InvalidOperationException("[CombatPrototype.Camera] Camera configuration requires finite angles, valid distance bounds and positive step/smoothing values.");

            _cameraTransform = controlledCamera.transform;
            controlledCamera.orthographic = false;
            controlledCamera.fieldOfView = fieldOfView;
            ResetView();
        }

        private void Start()
        {
            // Server-only startup has no local camera target or client input.
            _clientWorld = ClientServerBootstrap.ClientWorld;
            if (_clientWorld == null)
                return;
            _bindingSystem = _clientWorld.GetExistingSystemManaged<CombatPrototypeCameraBindingSystem>();
            if (_bindingSystem == null)
                throw new InvalidOperationException("[CombatPrototype.Camera] Client world requires CombatPrototypeCameraBindingSystem.");
            _bindingSystem.RegisterCamera(this);
        }

        private void OnDisable()
        {
            // World disposal and scene unloading can precede destruction of this component.
            if (_bindingSystem != null && _clientWorld.IsCreated)
                _bindingSystem.UnregisterCamera(this);
            _bindingSystem = null;
            _clientWorld = null;
        }

        public void ResetView()
        {
            _yaw = 0f;
            _startYaw = 0f;
            _targetYaw = 0f;
            _rotationElapsed = rotationDuration;
            _distance = defaultDistance;
            _targetDistance = defaultDistance;
            _zoomVelocity = 0f;
            _focusVelocity = Vector3.zero;
            _lastInputFrame = -1;
        }

        public float2 ReadMove(float2 move, Keyboard keyboard, Mouse mouse)
        {
            // Advance once per rendered frame even if input gathering runs more than once.
            if (_lastInputFrame != Time.frameCount)
            {
                _lastInputFrame = Time.frameCount;
                var rotationInput = keyboard == null ? 0 :
                    (keyboard.xKey.wasPressedThisFrame ? 1 : 0) - (keyboard.zKey.wasPressedThisFrame ? 1 : 0);
                if (rotationInput != 0)
                {
                    _startYaw = _yaw;
                    _targetYaw += rotationInput * rotationStep;
                    _rotationElapsed = 0f;
                }

                var deltaTime = Time.unscaledDeltaTime;
                _rotationElapsed = Mathf.Min(_rotationElapsed + deltaTime, rotationDuration);
                var progress = _rotationElapsed / rotationDuration;
                progress = progress * progress * (3f - 2f * progress);
                _yaw = Mathf.Lerp(_startYaw, _targetYaw, progress);

                var scroll = mouse == null ? 0f : mouse.scroll.ReadValue().y;
                if (scroll != 0f)
                    _targetDistance = Mathf.Clamp(_targetDistance - Mathf.Sign(scroll) * zoomStep, minDistance, maxDistance);
                _distance = Mathf.SmoothDamp(_distance, _targetDistance, ref _zoomVelocity,
                    followSmoothTime, Mathf.Infinity, deltaTime);
            }

            var worldMove = math.mul(quaternion.RotateY(math.radians(_yaw)), new float3(move.x, 0f, move.y));
            return worldMove.xz;
        }

        public void ApplyTargetPosition(Vector3 targetPosition, bool snap)
        {
            var focus = targetPosition + Vector3.up * targetHeight;
            if (snap)
            {
                _focusPosition = focus;
                _focusVelocity = Vector3.zero;
            }
            else
            {
                _focusPosition = Vector3.SmoothDamp(_focusPosition, focus, ref _focusVelocity,
                    followSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            }

            var rotation = Quaternion.Euler(pitch, _yaw, 0f);
            _cameraTransform.SetPositionAndRotation(_focusPosition + rotation * Vector3.back * _distance, rotation);
        }
    }
}
