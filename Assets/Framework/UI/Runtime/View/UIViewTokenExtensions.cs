using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay.UI
{
    /// <summary>按 Update 采样当前状态，不记录两次采样之间曾经完成的过渡。</summary>
    public static class UIViewTokenExtensions
    {
        /// <summary>等待加载和初始化完成；当前为 Opened 时返回 true，无效、关闭或失败时返回 false。</summary>
        /// <remarks>先同步检查，再按 Update 采样；取消仅终止本次等待并抛出取消异常，不关闭界面。</remarks>
        public static async UniTask<bool> WaitForReadyAsync(this UIViewToken token, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lifecycle = token.GetStatus().Lifecycle;
                if (lifecycle != UIViewLifecycle.Loading)
                {
                    return lifecycle == UIViewLifecycle.Opened;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        /// <summary>等待当前状态为 Opened 且 Shown；无效、关闭或失败时返回 false。</summary>
        /// <remarks>遮挡期间继续等待下一次显示；两次 Update 采样间的短暂 Shown 可能被错过。取消仅终止等待并抛出取消异常。</remarks>
        public static async UniTask<bool> WaitForShownAsync(this UIViewToken token, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var status = token.GetStatus();
                if (status.Lifecycle is UIViewLifecycle.None or UIViewLifecycle.Closing or UIViewLifecycle.Closed or UIViewLifecycle.Failed)
                {
                    return false;
                }

                if (status.Lifecycle == UIViewLifecycle.Opened && status.Visibility == UIViewVisibilityState.Shown)
                {
                    return true;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        /// <summary>等待清理完成；Closed 或 Failed 返回 true，默认 Token 返回 false。</summary>
        /// <remarks>先同步检查，再按 Update 采样；取消仅终止本次等待并抛出取消异常，不关闭界面。</remarks>
        public static async UniTask<bool> WaitForClosedAsync(this UIViewToken token, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lifecycle = token.GetStatus().Lifecycle;
                if (lifecycle is UIViewLifecycle.None or UIViewLifecycle.Closed or UIViewLifecycle.Failed)
                {
                    return lifecycle != UIViewLifecycle.None;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}
