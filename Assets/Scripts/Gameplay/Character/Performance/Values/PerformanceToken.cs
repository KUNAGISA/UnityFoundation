using System;

namespace Gameplay
{
    public readonly struct PerformanceToken : IEquatable<PerformanceToken>, Aoiro.ICancelToken
    {
        public static PerformanceToken None => default;

        private readonly PerformanceRunner _runner;
        private readonly int _executionId;

        public bool IsValid => Status != PerformanceStatus.None;
        public PerformanceStatus Status => _runner?.GetStatus(_executionId) ?? PerformanceStatus.None;
        public bool IsRunning => Status == PerformanceStatus.Running;

        internal PerformanceToken(PerformanceRunner runner, int executionId)
        {
            _runner = runner;
            _executionId = executionId;
        }

        public void Cancel()
        {
            _runner?.Cancel(_executionId);
        }

        public bool Equals(PerformanceToken other)
        {
            return ReferenceEquals(_runner, other._runner) && _executionId == other._executionId;
        }

        public override bool Equals(object obj)
        {
            return obj is PerformanceToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_runner, _executionId);
        }

        public override string ToString()
        {
            return IsValid ? $"PerformanceToken({_executionId}, {Status})" : "PerformanceToken(None)";
        }
    }
}
