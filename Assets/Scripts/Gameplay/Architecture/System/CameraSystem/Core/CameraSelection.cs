namespace Gameplay
{
    public readonly struct CameraSelection
    {
        public static readonly CameraSelection None = default;

        public readonly CameraProviderId ProviderId;
        public readonly long SessionId;
        public readonly int Priority;

        public bool IsValid => ProviderId != CameraProviderId.None && SessionId > 0;

        public CameraSelection(CameraProviderId providerId, long sessionId, int priority)
        {
            ProviderId = providerId;
            SessionId = sessionId;
            Priority = priority;
        }
    }
}
