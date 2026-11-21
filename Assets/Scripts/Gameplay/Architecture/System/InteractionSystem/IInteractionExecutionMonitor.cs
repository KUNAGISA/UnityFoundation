namespace Gameplay
{
    public interface IInteractionExecutionMonitor
    {
        InteractionExecutionStatus Status { get; }
        void Cancel();
    }
}
