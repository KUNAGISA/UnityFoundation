namespace Gameplay
{
    public readonly struct ConditionWatchToken
    {
        private readonly ConditionSystem _system;
        private readonly long _registrationId;

        internal ConditionWatchToken(ConditionSystem system, long registrationId)
        {
            _system = system;
            _registrationId = registrationId;
        }

        public void Cancel()
        {
            _system?.CancelWatch(_registrationId);
        }
    }
}
