namespace Gameplay
{
    public readonly struct CameraDebugSnapshot
    {
        public readonly CameraSelection Selected;
        public readonly bool IsBlending;
        public readonly string LiveCameraName;
        public readonly int ActiveOutputCount;
        public readonly CameraOutputDebugInfo[] Outputs;

        public CameraDebugSnapshot(in CameraSelection selected, bool isBlending, string liveCameraName, int activeOutputCount, CameraOutputDebugInfo[] outputs)
        {
            Selected = selected;
            IsBlending = isBlending;
            LiveCameraName = liveCameraName;
            ActiveOutputCount = activeOutputCount;
            Outputs = outputs;
        }
    }

    public readonly struct CameraOutputDebugInfo
    {
        public readonly CameraProviderId ProviderId;
        public readonly long SessionId;
        public readonly int Priority;
        public readonly string CameraName;
        public readonly bool Active;
        public readonly bool Selected;
        public readonly bool Live;

        public CameraOutputDebugInfo(CameraProviderId providerId, long sessionId, int priority, string cameraName, bool active, bool selected, bool live)
        {
            ProviderId = providerId;
            SessionId = sessionId;
            Priority = priority;
            CameraName = cameraName;
            Active = active;
            Selected = selected;
            Live = live;
        }
    }
}
