using Unity.Cinemachine;

namespace Gameplay
{
    internal sealed class CameraOutputState
    {
        public long RegistrationId;
        public CameraProviderId ProviderId;
        public long SessionId;
        public int Priority;
        public CinemachineVirtualCameraBase Camera;
        public bool Active;
    }
}
