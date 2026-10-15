using System;
using Aoiro;
using UnityEngine;

namespace Gameplay
{
    public sealed class MoveToRoutePositionPerformanceNode : IPerformanceNode
    {
        private const float DefaultSpeed = 10f;
        private const float DefaultArrivalDistance = 0.05f;
        private const int DefaultMaxStuckFrameCount = 10;
        private const int MotionPriority = 100;
        private const float DirectionSampleDistance = 0.01f;

        public Vector2 TargetPosition { get; }
        public float Speed { get; }

        private readonly float _arrivalDistance;
        private readonly float _arrivalDistanceSq;
        private readonly int _maxStuckFrameCount;
        private CharacterMotorAbility _motor;
        private MovementRouteModel _movementRoutes;
        private RouteLocation _targetLocation;
        private int _stuckFrameCount;
        private bool _waitingForMotionResult;
        private float _previousRemainingDistance;

        public MoveToRoutePositionPerformanceNode(Vector2 targetPosition, float arrivalDistance = DefaultArrivalDistance, int maxStuckFrameCount = DefaultMaxStuckFrameCount, float speed = DefaultSpeed)
        {
            if (arrivalDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(arrivalDistance), arrivalDistance, "Arrival distance must be greater than zero.");
            }

            if (maxStuckFrameCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStuckFrameCount), maxStuckFrameCount, "Maximum stuck frame count must be greater than zero.");
            }
            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "Speed must be greater than zero.");
            }

            TargetPosition = targetPosition;
            Speed = speed;
            _arrivalDistance = arrivalDistance;
            _arrivalDistanceSq = arrivalDistance * arrivalDistance;
            _maxStuckFrameCount = maxStuckFrameCount;
        }

        void IPerformanceNode.Enter(in PerformanceContext context)
        {
            _motor = context.Owner.RequireAbility<CharacterMotorAbility>();
            _movementRoutes = context.EntityContext.GetModel<MovementRouteModel>();
            _stuckFrameCount = 0;
            _waitingForMotionResult = false;
            _previousRemainingDistance = 0f;

            if (!_motor.TryGetRouteLocation(out var currentLocation) ||
                !_movementRoutes.TryProject(currentLocation.RouteId, TargetPosition, out _targetLocation) ||
                (_targetLocation.Position - TargetPosition).sqrMagnitude > _arrivalDistanceSq)
            {
                throw new InvalidOperationException($"[Performance] Can not project route movement target [{TargetPosition}] onto the current route.");
            }
        }

        PerformanceNodeStatus IPerformanceNode.Tick(in PerformanceContext context, float deltaTime)
        {
            if (!_motor.TryGetRouteLocation(out var currentLocation) || currentLocation.RouteId != _targetLocation.RouteId)
            {
                Debug.LogError($"[Performance] Entity [{context.Owner.EntityId}] is no longer on route [{_targetLocation.RouteId}].");
                return PerformanceNodeStatus.Failed;
            }

            var remainingDistance = _targetLocation.Distance - currentLocation.Distance;
            var distanceToTarget = Mathf.Abs(remainingDistance);
            if (distanceToTarget <= _arrivalDistance &&
                (context.Spatial.Position - TargetPosition).sqrMagnitude <= _arrivalDistanceSq)
            {
                return PerformanceNodeStatus.Succeeded;
            }

            if (_waitingForMotionResult && distanceToTarget >= _previousRemainingDistance)
            {
                ++_stuckFrameCount;
                if (_stuckFrameCount >= _maxStuckFrameCount)
                {
                    Debug.LogError($"[Performance] Entity [{context.Owner.EntityId}] can not move to route position [{TargetPosition}].");
                    return PerformanceNodeStatus.Failed;
                }
            }
            else if (_waitingForMotionResult)
            {
                _stuckFrameCount = 0;
            }

            var step = Mathf.Min(Speed * deltaTime, distanceToTarget);
            if (step <= 0f)
            {
                _waitingForMotionResult = false;
                return PerformanceNodeStatus.Running;
            }

            var sampleDistance = currentLocation.Distance + Mathf.Sign(remainingDistance) * Mathf.Min(distanceToTarget, DirectionSampleDistance);
            if (!_movementRoutes.TryEvaluateAtDistance(currentLocation.RouteId, sampleDistance, out var sampleLocation))
            {
                Debug.LogError($"[Performance] Can not sample route [{currentLocation.RouteId}] at distance [{sampleDistance}].");
                return PerformanceNodeStatus.Failed;
            }

            var direction = sampleLocation.Position - currentLocation.Position;
            _motor.SubmitDelta(direction.normalized * step, MotionMode.Override, MotionPriority);
            _previousRemainingDistance = distanceToTarget;
            _waitingForMotionResult = true;
            return PerformanceNodeStatus.Running;
        }

        void IPerformanceNode.Exit(in PerformanceContext context, PerformanceStatus status)
        {
            _motor = null;
            _movementRoutes = null;
            _targetLocation = default;
            _waitingForMotionResult = false;
        }
    }
}
