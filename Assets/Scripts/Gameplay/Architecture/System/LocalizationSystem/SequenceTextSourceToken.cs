namespace Gameplay
{
    public readonly struct SequenceTextSourceToken
    {
        public bool IsValid => _system != null && _system.IsSequenceTextSourceActive(_activationId);

        private readonly LocalizationSystem _system;
        private readonly long _activationId;

        internal SequenceTextSourceToken(LocalizationSystem system, long activationId)
        {
            _system = system;
            _activationId = activationId;
        }

        public void Cancel()
        {
            _system?.CancelSequenceTextSource(_activationId);
        }
    }
}
