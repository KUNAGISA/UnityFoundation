using System;

namespace Gameplay
{
    public readonly struct MainCameraHideToken : Aoiro.ICancelToken, IEquatable<MainCameraHideToken>
    {
        public bool IsValid => _cameraSystem != null && OwnerId > 0;
        public long OwnerId { get; }

        private readonly CameraSystem _cameraSystem;

        internal MainCameraHideToken(CameraSystem cameraSystem, long ownerId)
        {
            OwnerId = ownerId;
            _cameraSystem = cameraSystem;
        }

        public void Cancel() => _cameraSystem?.CancelMainCameraHide(this);

        public bool Equals(MainCameraHideToken other)
        {
            return Equals(_cameraSystem, other._cameraSystem) && OwnerId == other.OwnerId;
        }

        public override bool Equals(object obj)
        {
            return obj is MainCameraHideToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_cameraSystem, OwnerId);
        }
    }
}
