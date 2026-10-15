using Aoiro;
using UnityEngine;

namespace Gameplay
{
    public sealed class SwitchRoutePerformanceNode : IPerformanceNode
    {
        public int TargetRouteId { get; }
        public Vector2 TargetPosition { get; }
        public Quaternion TargetRotation { get; }

        private CharacterMotorAbility _motor;
        private MovementRouteModel _movementRoutes;

        public SwitchRoutePerformanceNode(int targetRouteId, Vector2 targetPosition, Quaternion targetRotation)
        {
            TargetRouteId = targetRouteId;
            TargetPosition = targetPosition;
            TargetRotation = targetRotation;
        }

        void IPerformanceNode.Enter(in PerformanceContext context)
        {
            _motor = context.Owner.RequireAbility<CharacterMotorAbility>();
            _movementRoutes = context.EntityContext.GetModel<MovementRouteModel>();
        }

        PerformanceNodeStatus IPerformanceNode.Tick(in PerformanceContext context, float deltaTime)
        {
            if (!_movementRoutes.TryProject(TargetRouteId, TargetPosition, out var targetLocation))
            {
                Debug.LogError($"[Performance] Can not project route transfer target [{TargetRouteId}] at [{TargetPosition}].");
                return PerformanceNodeStatus.Failed;
            }

            var facing = _motor.Facing;
            _motor.WarpToRoute(targetLocation, TargetRotation);
            _motor.SetFacing(facing);
            return PerformanceNodeStatus.Succeeded;
        }

        void IPerformanceNode.Exit(in PerformanceContext context, PerformanceStatus status)
        {
            _motor = null;
            _movementRoutes = null;
        }
    }
}
