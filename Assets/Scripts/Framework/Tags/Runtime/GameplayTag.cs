using System;

namespace Gameplay.Tags
{
    public readonly struct GameplayTag : IEquatable<GameplayTag>
    {
        internal int Id { get; }
        public bool IsValid => Id > 0;

        public GameplayTag(int id)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Id = id;
        }

        public bool Equals(GameplayTag other) => Id == other.Id;
        public override bool Equals(object obj) => obj is GameplayTag other && Equals(other);
        public override int GetHashCode() => Id;
        public override string ToString() => $"GameplayTag({Id})";
        public static bool operator ==(GameplayTag left, GameplayTag right) => left.Equals(right);
        public static bool operator !=(GameplayTag left, GameplayTag right) => !left.Equals(right);
    }
}
