using Aoiro;
using Gameplay.Entities;
using System;
using UnityEngine;

namespace Gameplay
{
    public enum MotionMode : byte
    {
        Override = 0,
        Additive = 1,
    }

    public enum FacingDirection : byte
    {
        Left = 0,
        Right = 1,
    }

    public sealed class CharacterMotorAbility : Ability
    {
        public Vector2 ActualDelta { get; private set; }
        public FacingDirection Facing => (_spatial.Rotation * Vector3.forward).z < 0f ? FacingDirection.Right : FacingDirection.Left;

        private const float FacingThreshold = 0.0001f;
        private static readonly Quaternion FacingFlip = Quaternion.Euler(0f, 180f, 0f);
        private readonly SpatialState _spatial;
        private Vector2 _overrideDelta;
        private Vector2 _additiveDelta;
        private int _overridePriority = int.MinValue;
        private bool _hasRouteLocation;
        private RouteLocation _routeLocation;

        public CharacterMotorAbility(SpatialState spatial)
        {
            _spatial = spatial ?? throw new ArgumentNullException(nameof(spatial));
        }

        protected override void OnDisabled()
        {
            ResetMotion();
        }

        protected override void OnDestroy()
        {
            ResetMotion();
            _hasRouteLocation = false;
            _routeLocation = default;
        }

        public void SubmitDelta(Vector2 delta, MotionMode mode, int priority = 0)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (mode == MotionMode.Additive)
            {
                _additiveDelta += delta;
                return;
            }

            if (priority > _overridePriority)
            {
                _overridePriority = priority;
                _overrideDelta = delta;
            }
            else if (priority == _overridePriority)
            {
                _overrideDelta += delta;
            }
        }

        public bool TryGetRouteLocation(out RouteLocation location)
        {
            location = _routeLocation;
            return _hasRouteLocation;
        }

        public void WarpToRoute(in RouteLocation location)
        {
            WarpToRoute(location, _spatial.Rotation);
        }

        public void WarpToRoute(in RouteLocation location, Quaternion rotation)
        {
            ResetMotion();
            _hasRouteLocation = true;
            _routeLocation = location;
            ApplyPose(location.RouteId, location.Position, rotation);
        }

        public void WarpToWorld(Vector2 position)
        {
            WarpToWorld(position, _spatial.Rotation);
        }

        public void WarpToWorld(Vector2 position, Quaternion rotation)
        {
            ResetMotion();
            _hasRouteLocation = false;
            _routeLocation = default;
            ApplyPose(null, position, rotation);
        }

        public void SetRotation(Quaternion rotation)
        {
            ApplyPose(_spatial.RouteId, _spatial.Position, rotation);
        }

        public void SetFacing(FacingDirection facing)
        {
            if (facing != FacingDirection.Left && facing != FacingDirection.Right)
            {
                throw new ArgumentOutOfRangeException(nameof(facing), facing, "Facing direction must be left or right.");
            }

            ApplyFacing(facing);
        }

        public void FaceTowards(Vector2 worldPosition)
        {
            var direction = worldPosition - _spatial.Position;
            if (TryGetFacingFromWorldDirection(direction, out var facing))
            {
                SetFacing(facing);
            }
        }

        public void ResolveMotion()
        {
            ActualDelta = Vector2.zero;
            try
            {
                ApplyFacingFromWorldDirection(_overrideDelta);

                var desiredDelta = _overrideDelta + _additiveDelta;
                if (!_hasRouteLocation || desiredDelta == Vector2.zero)
                {
                    return;
                }

                var previousPosition = _routeLocation.Position;
                var routeModel = Context.GetModel<MovementRouteModel>();
                if (!routeModel.TryMove(_routeLocation, desiredDelta, desiredDelta.magnitude, out var next))
                {
                    return;
                }

                _routeLocation = next;
                ApplyPose(next.RouteId, next.Position, _spatial.Rotation);
                ActualDelta = next.Position - previousPosition;
            }
            finally
            {
                ClearSubmittedMotion();
            }
        }

        private void ApplyFacingFromWorldDirection(Vector2 worldDirection)
        {
            if (TryGetFacingFromWorldDirection(worldDirection, out var facing))
            {
                ApplyFacing(facing);
            }
        }

        private void ApplyFacing(FacingDirection facing)
        {
            if (Facing == facing)
            {
                return;
            }

            ApplyPose(_spatial.RouteId, _spatial.Position, _spatial.Rotation * FacingFlip);
        }

        private bool TryGetFacingFromWorldDirection(Vector2 worldDirection, out FacingDirection facing)
        {
            var rotation = Facing == FacingDirection.Right ? _spatial.Rotation * FacingFlip : _spatial.Rotation;
            var localX = (Quaternion.Inverse(rotation) * worldDirection).x;
            if (localX > FacingThreshold)
            {
                facing = FacingDirection.Right;
                return true;
            }
            if (localX < -FacingThreshold)
            {
                facing = FacingDirection.Left;
                return true;
            }

            facing = default;
            return false;
        }

        private void ResetMotion()
        {
            ClearSubmittedMotion();
            ActualDelta = Vector2.zero;
        }

        private void ClearSubmittedMotion()
        {
            _overrideDelta = Vector2.zero;
            _additiveDelta = Vector2.zero;
            _overridePriority = int.MinValue;
        }

        private void ApplyPose(int? routeId, Vector2 position, Quaternion rotation)
        {
            _spatial.SetPose(routeId, position, rotation);
            var root = Owner.Root;
            root.SetPositionAndRotation(new Vector3(position.x, position.y, root.position.z), rotation);
        }
    }
}
