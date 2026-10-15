using System;

namespace Gameplay.Common
{
    public readonly struct VersionToken : IEquatable<VersionToken>
    {
        private readonly int _version;
        
        internal VersionToken(int version) => _version = version;

        public bool Equals(VersionToken other) => _version == other._version;

        public override bool Equals(object obj) => obj is VersionToken other && Equals(other);

        public override int GetHashCode() => _version.GetHashCode();

        public static bool operator ==(VersionToken left, VersionToken right) => left.Equals(right);

        public static bool operator !=(VersionToken left, VersionToken right) => !left.Equals(right);
    }
}
