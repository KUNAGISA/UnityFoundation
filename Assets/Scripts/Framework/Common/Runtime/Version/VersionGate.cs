using System;

namespace Gameplay.Common
{
    public struct VersionGate : IEquatable<VersionGate>
    {
        private int _version;

        public VersionToken Current => new VersionToken(_version);
        
        public VersionToken Bump() => new VersionToken(++_version);

        public bool Equals(VersionGate other) => _version == other._version;

        public override bool Equals(object obj) => obj is VersionGate other && Equals(other);

        public override int GetHashCode() => _version.GetHashCode();
    }
}
