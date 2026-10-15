using System;
using Gameplay.Common;

namespace Gameplay.Sequence
{
    public readonly struct AdvChoiceToken : IEquatable<AdvChoiceToken>
    {
        public int ChoiceId { get; }
        public VersionToken Version { get; }

        internal AdvChoiceToken(int choiceId, VersionToken version)
        {
            ChoiceId = choiceId;
            Version = version;
        }

        public bool Equals(AdvChoiceToken other) => ChoiceId == other.ChoiceId && Version == other.Version;
        public override bool Equals(object obj) => obj is AdvChoiceToken other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ChoiceId, Version);
    }
}
