namespace Gameplay.UI
{
    /// <summary>扩展回调当次的上下文快照；BeforeOpen 时没有 Token 和 View，失败释放时 View 也可能为空。</summary>
    public readonly struct UIViewExtensionContext
    {
        public UIViewToken Token { get; }
        public UIViewData Data { get; }
        public UIView View { get; }
        public UILayerId LayerId { get; }
        public UIViewOptionMask Options { get; }
        /// <summary>当前目标可见性；Hiding 阶段为 false，不代表 View GameObject 已停用。</summary>
        public bool IsVisible { get; }
        public UIViewHost Host { get; }

        internal UIViewExtensionContext(UIViewHost host, UIViewToken token, UIViewData data, UIView view, UILayerId layerId, UIViewOptionMask options, bool isVisible)
        {
            Host = host ?? throw new System.ArgumentNullException(nameof(host));
            Token = token;
            Data = data;
            View = view;
            LayerId = layerId;
            Options = options;
            IsVisible = isVisible;
        }

        /// <summary>判断创建会话时的选项快照是否包含指定的全部选项位；空选项返回 false。</summary>
        public bool HasOption(UIViewOptionMask option) => Options.Contains(option);
    }
}
