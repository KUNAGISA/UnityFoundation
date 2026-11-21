using System;
using Aoiro;
using Gameplay.Scheduler;
using Unity.Cinemachine;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Camera Module 在受控生命周期内访问镜头基础设施和 Gameplay 架构能力的上下文。
    /// 不应在 Module Shutdown 后保存或继续使用。
    /// </summary>
    public interface ICameraContext : ISystemAccessor, IModelAccessor, IServiceAccessor, IEventPublisher, IQueryExecutor, IEventSubscriber
    {
        Camera MainCamera { get; }
        Transform ActiveRoot { get; }
        Transform StagingRoot { get; }

        /// <summary>
        /// 注册 Module 的帧回调；返回的 Token 由注册方在会话或 Module 结束时取消。
        /// </summary>
        FrameTickToken AddFrameTick(FrameTickAction callback, CameraUpdatePhase phase = CameraUpdatePhase.LateTick);

        /// <summary>
        /// 获取 Provider 的固定优先级范围。
        /// </summary>
        CameraPriorityRange GetPriorityRange(CameraProviderId providerId);

        /// <summary>
        /// 注册参与全局仲裁的顶层 Cinemachine Camera；Priority 必须位于 Provider 范围内。
        /// </summary>
        CameraOutputRegistration RegisterOutput(CameraProviderId providerId, long sessionId, int priority, CinemachineVirtualCameraBase outputCamera);

        /// <summary>
        /// 判断是否存在更高固定优先级的 Active 输出。
        /// </summary>
        bool IsPriorityCovered(int priority);
    }

    internal sealed class CameraContext : ArchitectureContext, ICameraContext
    {
        public Camera MainCamera { get; set; }
        public Transform ActiveRoot { get; set; }
        public Transform StagingRoot { get; set; }
        public CinemachineBrain Brain { get; set; }

        internal CameraDirector Director { get; set; }
        internal CameraOutputRegistry OutputRegistry { get; set; }

        private readonly FrameTickScheduler _frameTickScheduler = new FrameTickScheduler();
        private readonly FrameTickScheduler _lateTickScheduler = new FrameTickScheduler();

        public void Cleanup()
        {
            MainCamera = null;
            ActiveRoot = null;
            StagingRoot = null;
            Brain = null;
            Director = null;
            OutputRegistry = null;
            _frameTickScheduler.Cleanup();
            _lateTickScheduler.Cleanup();
        }

        public FrameTickToken AddFrameTick(FrameTickAction callback, CameraUpdatePhase phase = CameraUpdatePhase.LateTick)
        {
            var scheduler = phase switch
            {
                CameraUpdatePhase.FrameTick => _frameTickScheduler,
                CameraUpdatePhase.LateTick => _lateTickScheduler,
                _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
            };
            return scheduler.Add(callback);
        }

        public CameraPriorityRange GetPriorityRange(CameraProviderId providerId) => Director.GetPriorityRange(providerId);

        public CameraOutputRegistration RegisterOutput(CameraProviderId providerId, long sessionId, int priority, CinemachineVirtualCameraBase outputCamera)
        {
            return OutputRegistry.Register(providerId, sessionId, priority, outputCamera);
        }

        public bool IsPriorityCovered(int priority) => OutputRegistry.IsPriorityCovered(priority);
        public void OnFrameTick(in FrameTime time)
        {
            _frameTickScheduler.Schedule(in time);
        }

        public void OnLateTick(in FrameTime time)
        {
            _lateTickScheduler.Schedule(in time);
        }
    }
}
