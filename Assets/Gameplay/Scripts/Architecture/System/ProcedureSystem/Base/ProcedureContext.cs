using System;
using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Gameplay.Common;
using Gameplay.Scheduler;

namespace Gameplay
{
    public interface IProcedureContext
    {
        /// <summary>
        /// 获取当前上下文的时间源。
        /// </summary>
        ITimeSource Time { get; }

        /// <summary>
        /// 获取与当前上下文生命周期相关的取消令牌。
        /// 该令牌用于在上下文销毁时通知依赖于该上下文的任务或操作应当被取消。
        /// </summary>
        CancellationToken LifetimeCancellationToken { get; }

        /// <summary>
        /// 添加一个帧任务，该任务会在指定的时机执行。
        /// </summary>
        /// <param name="callback">要执行的任务回调，它接受一个FrameTime参数。</param>
        /// <param name="timing">任务执行的时机，默认为ProcedureTiming.FrameTick。</param>
        /// <returns>返回任务Token</returns>
        FrameTickToken AddFrameTickTask(FrameTickAction callback, ProcedureTiming timing = ProcedureTiming.FrameTick);

        /// <summary>
        /// 添加定时任务
        /// </summary>
        /// <param name="intervalMs">任务执行的间隔时间（毫秒）</param>
        /// <param name="callback">定时执行的回调函数</param>
        /// <param name="times">任务执行的次数，默认为1次。如果设置为负数，则表示无限次触发</param>
        /// <param name="delayMs">首次执行前的延迟时间（毫秒），可选参数</param>
        /// <returns>返回定时器Token</returns>
        TimerToken AddTimerTask(ulong intervalMs, Action callback, int times = 1, ulong? delayMs = null);

        /// <summary>
        /// 注册一个取消令牌，当上下文销毁时自动取消该令牌。
        /// </summary>
        /// <param name="cancelToken">要注册的ICancelToken实例。</param>
        void CancelWhenDestroy(ICancelToken cancelToken);

        /// <summary>
        /// 报告当前流程中无法恢复的异步操作失败，并使流程进入终止态。
        /// </summary>
        /// <param name="exception">导致流程无法继续的异常。</param>
        void Fail(Exception exception);
    }

    public sealed class ProcedureContext : ArchitectureContext, IProcedureContext, ISystemAccessor, IModelAccessor, IServiceAccessor, IEventSubscriber, IEventPublisher, ICommandExecutor, IQueryExecutor
    {
        private readonly Action<Exception> _failureHandler;
        private readonly FrameTickScheduler _earlyTickScheduler = new FrameTickScheduler();
        private readonly FrameTickScheduler _frameTickScheduler = new FrameTickScheduler();
        private readonly FrameTickScheduler _lateTickScheduler = new FrameTickScheduler();
        private readonly TimerScheduler _timerScheduler = new TimerScheduler();
        private readonly HashSet<ICancelToken> _cancelWhenDestroy = new();

        private CancellationTokenSource _lifetimeCancellationTokenSource;

        public ITimeSource Time { get; }
        public CancellationToken LifetimeCancellationToken => _lifetimeCancellationTokenSource?.Token ?? new CancellationToken(true);

        public ProcedureContext(ITimeSource timeSource, CancellationToken token, Action<Exception> failureHandler)
        {
            Time = timeSource;
            _failureHandler = failureHandler;
            _lifetimeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        }
        
        public void Destroy()
        {
            var exceptions = new CleanupExceptionCollector();
            var tokens = new ICancelToken[_cancelWhenDestroy.Count];
            _cancelWhenDestroy.CopyTo(tokens);

            for (var index = 0; index < tokens.Length; ++index)
            {
                try
                {
                    tokens[index].Cancel();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }
            _cancelWhenDestroy.Clear();

            _earlyTickScheduler.Cleanup();
            _frameTickScheduler.Cleanup();
            _lateTickScheduler.Cleanup();
            _timerScheduler.Cleanup();

            var lifetimeCancellationTokenSource = _lifetimeCancellationTokenSource;
            _lifetimeCancellationTokenSource = null;
            if (lifetimeCancellationTokenSource != null)
            {
                try
                {
                    lifetimeCancellationTokenSource.Cancel();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
                finally
                {
                    lifetimeCancellationTokenSource.Dispose();
                }
            }

            exceptions.ThrowIfAny();
        }
        
        public FrameTickToken AddFrameTickTask(FrameTickAction callback, ProcedureTiming timing = ProcedureTiming.FrameTick)
        {
            var frameTickTask = timing switch
            {
                ProcedureTiming.EarlyTick => _earlyTickScheduler,
                ProcedureTiming.FrameTick => _frameTickScheduler,
                ProcedureTiming.LateTick => _lateTickScheduler,
                _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, null)
            };
            return frameTickTask.Add(callback);
        }

        public TimerToken AddTimerTask(ulong intervalMs, Action callback, int times = 1, ulong? delayMs = null)
        {
            return _timerScheduler.Add(intervalMs, callback, times, delayMs);
        }

        public void CancelWhenDestroy(ICancelToken cancelToken)
        {
            if (cancelToken != null)
            {
                _cancelWhenDestroy.Add(cancelToken);
            }
        }

        public void Fail(Exception exception) => _failureHandler(exception);

        public void EarlyTick()
        {
            _earlyTickScheduler.Schedule(Time.ToFrameTime());
        }

        public void FrameTick()
        {
            _frameTickScheduler.Schedule(Time.ToFrameTime());
            _timerScheduler.Schedule((ulong)Time.DeltaTimeMs);
        }

        public void LateTick()
        {
            _lateTickScheduler.Schedule(Time.ToFrameTime());
        }
    }
}
