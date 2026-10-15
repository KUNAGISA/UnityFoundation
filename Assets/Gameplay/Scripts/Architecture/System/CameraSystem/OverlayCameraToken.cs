using System;
using Aoiro;

namespace Gameplay
{
    public readonly struct OverlayCameraToken : ICancelToken, IEquatable<OverlayCameraToken>
    {
        public bool IsValid => _cameraSystem != null && OwnerId > 0;
        public long OwnerId { get; }

        private readonly CameraSystem _cameraSystem;

        internal OverlayCameraToken(CameraSystem cameraSystem, long ownerId)
        {
            OwnerId = ownerId;
            _cameraSystem = cameraSystem;
        }

        public void Cancel() => _cameraSystem?.CancelOverlayCamera(this);

        public bool Equals(OverlayCameraToken other)
        {
            return Equals(_cameraSystem, other._cameraSystem) && OwnerId == other.OwnerId;
        }

        public override bool Equals(object obj)
        {
            return obj is OverlayCameraToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_cameraSystem, OwnerId);
        }
    }
}
