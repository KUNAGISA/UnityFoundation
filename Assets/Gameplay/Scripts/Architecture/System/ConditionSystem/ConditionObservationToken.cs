namespace Gameplay
{
    public readonly struct ConditionObservationToken
    {
        private readonly ConditionSystem _owner;
        private readonly long _id;

        public ConditionObservationToken(ConditionSystem owner, long id)
        {
            _owner = owner;
            _id = id;
        }

        public void Cancel() => _owner?.CancelObservation(_id);
        public void Pause() => _owner?.PauseObservation(_id);
        public void Resume() => _owner?.ResumeObservation(_id);
    }
}
