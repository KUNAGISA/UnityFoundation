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
        Front = 2,
        Back = 3,
    }

    public sealed class CharacterMotor
    {
        public Vector2 ActualDelta { get; private set; }
        public FacingDirection Facing => _spatial.Facing;

        private const float FacingThreshold = 0.0001f;
        private readonly SpatialState _spatial;
        private readonly Transform _root;
        private readonly MovementRouteModel _movementRoutes;
        private Vector2 _overrideDelta;
        private Vector2 _additiveDelta;
        private int _overridePriority = int.MinValue;
        private bool _hasRouteLocation;
        private RouteLocation _routeLocation;

        public CharacterMotor(SpatialState spatial, Transform root, MovementRouteModel movementRoutes, RouteLocation? routeLocation)
        {
            _spatial = spatial;
            _root = root;
            _movementRoutes = movementRoutes;
            _hasRouteLocation = routeLocation.HasValue;
            _routeLocation = routeLocation.GetValueOrDefault();
        }

        public void SubmitDelta(Vector2 delta, MotionMode mode, int priority = 0)
        {
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
            WarpToRoute(location, Facing);
        }

        public void WarpToRoute(in RouteLocation location, FacingDirection facing)
        {
            ResetMotion();
            _hasRouteLocation = true;
            _routeLocation = location;
            ApplyPose(location.RouteId, location.Position, facing);
        }

        public void WarpToWorld(Vector2 position)
        {
            WarpToWorld(position, Facing);
        }

        public void WarpToWorld(Vector2 position, FacingDirection facing)
        {
            ResetMotion();
            _hasRouteLocation = false;
            _routeLocation = default;
            ApplyPose(null, position, facing);
        }

        public void SetFacing(FacingDirection facing)
        {
            if ((uint)facing > (uint)FacingDirection.Back)
            {
                throw new ArgumentOutOfRangeException(nameof(facing), facing, "Unknown facing direction.");
            }

            _spatial.SetFacing(facing);
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
                var desiredDelta = _overrideDelta + _additiveDelta;
                if (!_hasRouteLocation || desiredDelta == Vector2.zero)
                {
                    return;
                }

                var previousPosition = _routeLocation.Position;
                if (!_movementRoutes.TryMove(_routeLocation, desiredDelta, desiredDelta.magnitude, out var next))
                {
                    return;
                }

                _routeLocation = next;
                ActualDelta = next.Position - previousPosition;
                var facing = TryGetFacingFromWorldDirection(ActualDelta, out var direction) ? direction : Facing;
                ApplyPose(next.RouteId, next.Position, facing);
            }
            finally
            {
                ClearSubmittedMotion();
            }
        }

        private static bool TryGetFacingFromWorldDirection(Vector2 worldDirection, out FacingDirection facing)
        {
            var horizontal = Mathf.Abs(worldDirection.x);
            var vertical = Mathf.Abs(worldDirection.y);
            if (Mathf.Max(horizontal, vertical) < FacingThreshold)
            {
                facing = default;
                return false;
            }

            if (horizontal >= vertical)
            {
                facing = worldDirection.x > 0f ? FacingDirection.Right : FacingDirection.Left;
            }
            else
            {
                facing = worldDirection.y > 0f ? FacingDirection.Back : FacingDirection.Front;
            }

            return true;
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

        private void ApplyPose(int? routeId, Vector2 position, FacingDirection facing)
        {
            _spatial.SetPosition(routeId, position);
            _spatial.SetFacing(facing);
            _root.position = new Vector3(position.x, position.y, _root.position.z);
        }
    }
}
