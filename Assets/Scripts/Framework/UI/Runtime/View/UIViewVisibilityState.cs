namespace Gameplay.UI
{
    /// <summary>当前显隐阶段；隐藏后可再次显示，生命周期需另行查询。</summary>
    public enum UIViewVisibilityState
    {
        /// <summary>当前隐藏，或尚未开始展示。</summary>
        Hidden,
        /// <summary>目标已切换为可见，展示尚未完成。</summary>
        Showing,
        /// <summary>当前有效的展示过渡已完成。</summary>
        Shown,
        /// <summary>目标已切换为隐藏，隐藏尚未完成。</summary>
        Hiding
    }
}
