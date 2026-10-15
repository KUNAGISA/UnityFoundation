using System.Diagnostics;

namespace Gameplay.Scheduler
{
    /// <summary>
    /// 分帧离散任务的动态时间预算
    /// </summary>
    public readonly struct TimeSliceBudget
    {
        private readonly long _deadlineTicks;

        internal TimeSliceBudget(long deadlineTicks)
        {
            _deadlineTicks = deadlineTicks;
        }

        /// <summary>
        /// 当前剩余时间预算（微秒），超支时返回负数
        /// </summary>
        public int RemainingBudgetUs
        {
            get
            {
                if (_deadlineTicks == 0L)
                {
                    return int.MinValue;
                }

                var remainingTicks = _deadlineTicks - Stopwatch.GetTimestamp();
                return (int)(remainingTicks * 1_000_000L / Stopwatch.Frequency);
            }
        }

        /// <summary>
        /// 是否应当让出当前时间片
        /// </summary>
        public bool ShouldYield => _deadlineTicks == 0L || Stopwatch.GetTimestamp() >= _deadlineTicks;
    }
}
