using System;

namespace Gameplay.Sequence
{
    public readonly struct SequenceToken : IEquatable<SequenceToken>
    {
        public static SequenceToken None => default;

        public readonly int PlaybackId;

        public bool IsValid => PlaybackId != 0;

        internal SequenceToken(int playbackId)
        {
            PlaybackId = playbackId;
        }

        public bool Equals(SequenceToken other)
        {
            return PlaybackId == other.PlaybackId;
        }

        public override bool Equals(object obj)
        {
            return obj is SequenceToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return PlaybackId.GetHashCode();
        }

        public override string ToString()
        {
            return IsValid ? $"SequenceToken({PlaybackId})" : "SequenceToken(None)";
        }
    }
}
