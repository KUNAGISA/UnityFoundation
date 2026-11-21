namespace Gameplay
{
    public readonly struct ProcedurePauseChangedEvent
    {
        public readonly bool IsPaused;

        public ProcedurePauseChangedEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }
}
