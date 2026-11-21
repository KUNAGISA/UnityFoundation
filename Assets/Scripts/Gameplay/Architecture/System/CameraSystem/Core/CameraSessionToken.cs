namespace Gameplay
{
    /// <summary>
    /// CameraSessionToken 的内部生命周期所有者，用 SessionId 隔离已结束和后续创建的会话。
    /// </summary>
    internal interface ICameraSessionTokenOwner
    {
        bool IsSessionActive(long sessionId);
        void CancelSession(long sessionId);
    }

    public readonly struct CameraSessionToken : Aoiro.ICancelToken
    {
        public bool IsValid => _owner != null && _sessionId > 0 && _owner.IsSessionActive(_sessionId);
        public long SessionId => _sessionId;

        private readonly ICameraSessionTokenOwner _owner;
        private readonly long _sessionId;

        internal CameraSessionToken(ICameraSessionTokenOwner owner, long sessionId)
        {
            _owner = owner;
            _sessionId = sessionId;
        }

        public void Cancel()
        {
            _owner?.CancelSession(_sessionId);
        }
    }
}
