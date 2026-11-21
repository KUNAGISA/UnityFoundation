using System;

using Aoiro;

namespace Gameplay.Scheduler
{
    /// <summary>
    /// 帧调度任务Token
    /// </summary>
    public readonly struct FrameTickToken : IEquatable<FrameTickToken>, ICancelToken
    {
        internal readonly int Index;
        internal readonly int Version;
        internal readonly FrameTickScheduler Scheduler;

        internal FrameTickToken(FrameTickScheduler scheduler, int index, int version)
        {
            Index = index;
            Version = version;
            Scheduler = scheduler;
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        public void Pause()
        {
            Scheduler?.Pause(this);
        }

        /// <summary>
        /// 恢复任务
        /// </summary>
        public void Resume()
        {
            Scheduler?.Resume(this);
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public void Cancel()
        {
            Scheduler?.Cancel(this);
        }

        public bool Equals(FrameTickToken other)
        {
            return Index == other.Index && Version == other.Version && Scheduler == other.Scheduler;
        }

        public override bool Equals(object obj)
        {
            return obj is FrameTickToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, Version, Scheduler);
        }

        public override string ToString()
        {
            return $"FrameTickToken({Index}, v{Version})";
        }
    }
}
