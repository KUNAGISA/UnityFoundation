using System;

namespace Gameplay
{
    public readonly struct InputDeviceInfo : IEquatable<InputDeviceInfo>
    {
        public int DeviceId { get; }
        public string Layout { get; }
        public string DisplayName { get; }
        public bool IsValid => DeviceId != 0;

        public InputDeviceInfo(int deviceId, string layout, string displayName)
        {
            DeviceId = deviceId;
            Layout = layout ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        public bool Equals(InputDeviceInfo other) => DeviceId == other.DeviceId;
        public override bool Equals(object obj) => obj is InputDeviceInfo other && Equals(other);
        public override int GetHashCode() => DeviceId;
        public static bool operator ==(InputDeviceInfo left, InputDeviceInfo right) => left.Equals(right);
        public static bool operator !=(InputDeviceInfo left, InputDeviceInfo right) => !left.Equals(right);
    }
}
