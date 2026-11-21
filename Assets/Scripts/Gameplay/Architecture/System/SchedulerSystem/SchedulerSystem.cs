using System;
using Aoiro;
using Gameplay.Scheduler;

namespace Gameplay
{
    public class SchedulerSystem : SystemBase, IFrameTickable
    {
        private readonly TimeSliceScheduler _timeSliceScheduler = new TimeSliceScheduler(budgetUs: 500, maxSliceBudgetUs: 100);
        
        private readonly TimerScheduler _timerScheduler = new TimerScheduler();

        protected override void OnShutdown()
        {
            _timeSliceScheduler.Cleanup();
            _timerScheduler.Cleanup();
        }

        /// <summary>
        /// 添加一个分帧离散任务
        /// </summary>
        /// <param name="callback">分帧离散任务的回调函数</param>
        /// <returns>返回任务Token</returns>
        public TimeSliceToken AddTimeSliceTask(TimeSliceCallback callback)
        {
            return _timeSliceScheduler.Add(callback);
        }

        /// <summary>
        /// 添加定时任务
        /// </summary>
        /// <param name="intervalMs">任务执行的间隔时间（毫秒）</param>
        /// <param name="callback">定时执行的回调函数</param>
        /// <param name="times">任务执行的次数，默认为1次。如果设置为负数，则表示无限次触发</param>
        /// <param name="delayMs">首次执行前的延迟时间（毫秒），可选参数</param>
        /// <returns>返回定时器Token</returns>
        public TimerToken AddTimerTask(ulong intervalMs, Action callback, int times = 1, ulong? delayMs = null)
        {
            return _timerScheduler.Add(intervalMs, callback, times, delayMs);
        }

        void IFrameTickable.EarlyTick(in FrameTime time) { }

        void IFrameTickable.FrameTick(in FrameTime time)
        {
            _timeSliceScheduler.Schedule();
            _timerScheduler.Schedule((ulong)time.DeltaTimeMs);
        }

        void IFrameTickable.LateTick(in FrameTime time) { }
    }
}
