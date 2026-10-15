using System;

using Aoiro;

namespace Gameplay.Tags
{
    public readonly struct GameplayTagToken : IEquatable<GameplayTagToken>, ICancelToken
    {
        private readonly GameplayTagContainer _container;
        private readonly long _tokenId;

        internal GameplayTagToken(GameplayTagContainer container, long tokenId)
        {
            _container = container;
            _tokenId = tokenId;
        }

        public void Cancel()
        {
            _container?.Cancel(_tokenId);
        }

        public bool Equals(GameplayTagToken other)
        {
            return ReferenceEquals(_container, other._container) && _tokenId == other._tokenId;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTagToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_container, _tokenId);
        }

        public override string ToString()
        {
            return $"GameplayTagToken({_tokenId})";
        }
    }
}
