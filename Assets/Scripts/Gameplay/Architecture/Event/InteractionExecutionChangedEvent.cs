namespace Gameplay
{
    public readonly struct InteractionExecutionChangedEvent
    {
        public InteractionToken Interaction { get; }
        public InteractionExecutionStatus Status { get; }

        public InteractionExecutionChangedEvent(InteractionToken interaction, InteractionExecutionStatus status)
        {
            Interaction = interaction;
            Status = status;
        }
    }
}
