namespace Gameplay.UI
{
    /// <summary>一次界面打开的生命周期；终态只在清理完成后发布。</summary>
    public enum UIViewLifecycle
    {
        /// <summary>默认 Token，未关联会话。</summary>
        None,
        /// <summary>实例加载或初始化中。</summary>
        Loading,
        /// <summary>初始化及层提交已完成，不代表展示动画已完成。</summary>
        Opened,
        /// <summary>已请求关闭，正在等待初始化退出、隐藏或释放。</summary>
        Closing,
        /// <summary>正常关闭且清理完成。</summary>
        Closed,
        /// <summary>打开失败且回滚清理完成；不用于表达动画异常。</summary>
        Failed
    }
}
