namespace Gameplay
{
    public interface IPerformanceNode
    {
        protected internal void Enter(in PerformanceContext context);
        protected internal PerformanceNodeStatus Tick(in PerformanceContext context, float deltaTime);
        protected internal void Exit(in PerformanceContext context, PerformanceStatus status);
    }
}
