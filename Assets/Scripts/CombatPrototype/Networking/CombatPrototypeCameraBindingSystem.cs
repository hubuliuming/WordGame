using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.NetCode.Hybrid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code_01.CombatPrototype.Networking
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [CreateAfter(typeof(GhostPresentationGameObjectSystem))]
    [CreateAfter(typeof(GhostPresentationGameObjectTransformSystem))]
    public partial class CombatPrototypeCameraBindingSystem : SystemBase
    {
        private GhostPresentationGameObjectSystem _presentationSystem;
        private GhostPresentationGameObjectTransformSystem _presentationTransformSystem;
        private CombatPrototypeFollowCamera _camera;
        private Entity _inputPlayer;
        private Entity _viewPlayer;
        private Transform _followTarget;
        private bool _wasDead;

        protected override void OnCreate()
        {
            RequireForUpdate<CombatPrototypePlayerSpawner>();
            _presentationSystem = World.GetExistingSystemManaged<GhostPresentationGameObjectSystem>();
            _presentationTransformSystem = World.GetExistingSystemManaged<GhostPresentationGameObjectTransformSystem>();
        }

        public void RegisterCamera(CombatPrototypeFollowCamera camera)
        {
            if (_camera != null && _camera != camera)
                throw new InvalidOperationException($"[CombatPrototype.Camera] World={World.Name} already has a registered camera.");
            _camera = camera;
        }

        public void UnregisterCamera(CombatPrototypeFollowCamera camera)
        {
            if (_camera != camera)
                return;
            ClearTarget();
            _camera = null;
        }

        public float2 ReadMove(float2 move, Keyboard keyboard, Mouse mouse)
        {
            var camera = RequireCamera();
            var player = GetLocalPlayer();
            if (player == Entity.Null)
            {
                ClearTarget();
                return float2.zero;
            }
            if (player != _inputPlayer)
            {
                // Reset before converting commands so the first command matches the new view.
                camera.ResetView();
                _inputPlayer = player;
            }
            return camera.ReadMove(move, keyboard, mouse);
        }

        protected override void OnUpdate()
        {
            var player = GetLocalPlayer();
            if (player == Entity.Null)
            {
                ClearTarget();
                return;
            }

            var camera = RequireCamera();
            var dead = EntityManager.GetComponentData<CombatPrototypePlayerHealth>(player).IsDead != 0;
            var changed = player != _viewPlayer;
            if (changed)
            {
                var view = _presentationSystem.GetGameObjectForEntity(EntityManager, player);
                if (view == null)
                    throw new InvalidOperationException($"[CombatPrototype.Camera] World={World.Name}, player={player} requires its configured PlayerView presentation object.");
                _followTarget = view.transform;
                _viewPlayer = player;
                if (_inputPlayer != player)
                {
                    camera.ResetView();
                    _inputPlayer = player;
                }
            }

            // The official bridge schedules a Transform job; finish it before reading the displayed position.
            _presentationTransformSystem.CheckedStateRef.CompleteDependency();
            camera.ApplyTargetPosition(_followTarget.position, changed || (_wasDead && !dead));
            _wasDead = dead;
        }

        protected override void OnStopRunning()
        {
            ClearTarget();
        }

        protected override void OnDestroy()
        {
            ClearTarget();
            _camera = null;
        }

        private Entity GetLocalPlayer()
        {
            var player = Entity.Null;
            // GhostOwnerIsLocal is enableable; enumerate its enabled matches without a singleton API or temporary array.
            foreach (var (_, entity) in SystemAPI.Query<RefRO<CombatPrototypePlayerNetCode>>()
                         .WithAll<GhostOwnerIsLocal>().WithEntityAccess())
            {
                if (player != Entity.Null)
                    throw new InvalidOperationException($"[CombatPrototype.Camera] World={World.Name} requires one local player, found multiple owned players.");
                player = entity;
            }
            // No owned ghost during admission or after disconnect is an expected lifecycle state.
            return player;
        }

        private CombatPrototypeFollowCamera RequireCamera()
        {
            if (_camera == null)
                throw new InvalidOperationException($"[CombatPrototype.Camera] World={World.Name} requires the registered Main Camera component.");
            return _camera;
        }

        private void ClearTarget()
        {
            if ((_inputPlayer != Entity.Null || _viewPlayer != Entity.Null) && _camera != null)
                _camera.ResetView();
            _inputPlayer = Entity.Null;
            _viewPlayer = Entity.Null;
            _followTarget = null;
            _wasDead = false;
        }
    }
}
