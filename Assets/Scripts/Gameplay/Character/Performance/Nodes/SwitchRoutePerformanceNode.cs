using UnityEngine;

namespace Gameplay
{
    public sealed class SwitchRoutePerformanceNode : IPerformanceNode
    {
        public int TargetRouteId { get; }
        public Vector2 TargetPosition { get; }

        public SwitchRoutePerformanceNode(int targetRouteId, Vector2 targetPosition)
        {
            TargetRouteId = targetRouteId;
            TargetPosition = targetPosition;
        }

        void IPerformanceNode.Enter(in PerformanceContext context) { }

        PerformanceNodeStatus IPerformanceNode.Tick(in PerformanceContext context, float deltaTime)
        {
            if (!context.MovementRoutes.TryProject(TargetRouteId, TargetPosition, out var targetLocation))
            {
                Debug.LogError($"[Performance] Can not project route transfer target [{TargetRouteId}] at [{TargetPosition}].");
                return PerformanceNodeStatus.Failed;
            }

            context.Motor.WarpToRoute(targetLocation);
            return PerformanceNodeStatus.Succeeded;
        }

        void IPerformanceNode.Exit(in PerformanceContext context, PerformanceStatus status) { }
    }
}
