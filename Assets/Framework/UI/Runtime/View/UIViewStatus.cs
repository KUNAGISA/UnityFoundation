namespace Gameplay.UI
{
    /// <summary>一次界面打开的当前状态；终态在全部逻辑释放完成后发布。</summary>
    public readonly struct UIViewStatus
    {
        public UIViewLifecycle Lifecycle { get; }
        public UIViewVisibilityState Visibility { get; }

        internal UIViewStatus(UIViewLifecycle lifecycle, UIViewVisibilityState visibility)
        {
            Lifecycle = lifecycle;
            Visibility = visibility;
        }
    }
}
