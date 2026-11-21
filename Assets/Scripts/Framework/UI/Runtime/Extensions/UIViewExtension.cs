namespace Gameplay.UI
{
    /// <summary>在打开请求及 Session 的可见性与释放阶段扩展横切行为。</summary>
    public abstract class UIViewExtension
    {
        /// <summary>Session 创建前调用，此时 Token 和 View 不可用；抛出异常会拒绝本次打开。</summary>
        protected internal virtual void OnBeforeOpen(in UIViewExtensionContext context) { }

        /// <summary>已打开 View 应用新 Data 后调用；不得重入打开或刷新当前 Session。</summary>
        protected internal virtual void OnRefreshed(in UIViewExtensionContext context) { }

        /// <summary>View.OnShow 前调用，适合获取目标可见期间持有的资源。</summary>
        protected internal virtual void OnBeforeShow(in UIViewExtensionContext context) { }

        /// <summary>仅在 Show 未被 Hide、Close 或新过渡打断时调用。</summary>
        protected internal virtual void OnShowCompleted(in UIViewExtensionContext context) { }

        /// <summary>View.OnHide 前调用，适合撤销目标可见期间持有的资源。</summary>
        protected internal virtual void OnBeforeHide(in UIViewExtensionContext context) { }

        /// <summary>Session 永久释放时逆序调用；打开失败时 View 可能为空，清理应可重复执行。</summary>
        protected internal virtual void OnReleased(in UIViewExtensionContext context) { }

        /// <summary>所有 Session 归还实例并注销后逆序调用，清理 Host 级共享状态。</summary>
        protected internal virtual void OnShutdown(UIViewHost host) { }
    }
}
