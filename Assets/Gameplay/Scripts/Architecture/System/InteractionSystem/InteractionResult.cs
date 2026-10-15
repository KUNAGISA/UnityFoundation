using System;

namespace Gameplay
{
    public readonly struct InteractionResult
    {
        public static InteractionResult Succeeded => new(InteractionExecutionStatus.Succeeded, null);
        public static InteractionResult Failed => new(InteractionExecutionStatus.Failed, null);

        public InteractionExecutionStatus Status { get; }
        public IInteractionExecutionMonitor Monitor { get; }

        private InteractionResult(InteractionExecutionStatus status, IInteractionExecutionMonitor monitor)
        {
            Status = status;
            Monitor = monitor;
        }

        public static InteractionResult Running(IInteractionExecutionMonitor monitor)
        {
            return new InteractionResult(InteractionExecutionStatus.Running, monitor ?? throw new ArgumentNullException(nameof(monitor)));
        }
    }
}
