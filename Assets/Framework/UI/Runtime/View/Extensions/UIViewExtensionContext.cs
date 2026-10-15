namespace Gameplay.UI
{
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

        public bool HasOption(UIViewOptionMask option) => Options.Contains(option);
    }
}
