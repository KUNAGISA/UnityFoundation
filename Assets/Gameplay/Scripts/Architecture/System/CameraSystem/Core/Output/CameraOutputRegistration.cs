namespace Gameplay
{
    public readonly struct CameraOutputRegistration : Aoiro.ICancelToken
    {
        public bool IsValid => _owner != null && _registrationId > 0;

        private readonly ICameraOutputRegistrationOwner _owner;
        private readonly long _registrationId;

        internal CameraOutputRegistration(ICameraOutputRegistrationOwner owner, long registrationId)
        {
            _owner = owner;
            _registrationId = registrationId;
        }

        public void SetActive(bool active) => _owner?.SetOutputActive(_registrationId, active);
        public void UpdateSession(long sessionId) => _owner?.UpdateOutputSession(_registrationId, sessionId);
        public bool IsLive() => _owner != null && _owner.IsOutputLive(_registrationId);
        public void Cancel() => _owner?.UnregisterOutput(_registrationId);
    }
}
