namespace Gameplay.UI
{
    /// <summary>界面打开数据；创建会话时快照资源、层和选项配置，复用刷新不重新应用这些配置。</summary>
    public abstract class UIViewData
    {
        /// <summary>Provider 加载界面实例使用的资源地址，同时作为顶部复用和单实例冲突的比较键。</summary>
        public abstract string Location { get; }
        /// <summary>目标逻辑层；未指定时使用 Host 的默认层。</summary>
        public virtual UILayerId LayerId => UILayerId.Unspecified;
        /// <summary>提供给扩展的界面选项，由项目定义各选项位的含义。</summary>
        public virtual UIViewOptionMask Options => default;
        /// <summary>是否允许同资源多实例；默认复用同层顶部实例，或在新实例初始化成功后替换旧实例。</summary>
        public virtual bool AllowMultiple => false;
        /// <summary>本界面目标可见时是否遮挡同层下方界面；遮挡只隐藏，不释放实例。</summary>
        public virtual bool HideViewsBelow => false;
    }
}
