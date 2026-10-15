using System;

namespace Gameplay
{
    public readonly struct PerformanceToken : IEquatable<PerformanceToken>, Aoiro.ICancelToken
    {
        public static PerformanceToken None => default;

        private readonly PerformanceAbility _ability;
        private readonly int _executionId;

        public bool IsValid => Status != PerformanceStatus.None;
        public PerformanceStatus Status => _ability?.GetStatus(_executionId) ?? PerformanceStatus.None;
        public bool IsRunning => Status == PerformanceStatus.Running;

        internal PerformanceToken(PerformanceAbility ability, int executionId)
        {
            _ability = ability;
            _executionId = executionId;
        }

        public void Cancel()
        {
            _ability?.Cancel(_executionId);
        }

        public bool Equals(PerformanceToken other)
        {
            return ReferenceEquals(_ability, other._ability) && _executionId == other._executionId;
        }

        public override bool Equals(object obj)
        {
            return obj is PerformanceToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_ability, _executionId);
        }

        public override string ToString()
        {
            return IsValid ? $"PerformanceToken({_executionId}, {Status})" : "PerformanceToken(None)";
        }
    }
}
