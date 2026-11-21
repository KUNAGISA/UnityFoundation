using System;
using Aoiro;

namespace Gameplay.UI
{
    public readonly struct UIViewToken : IEquatable<UIViewToken>, ICancelToken
    {
        public static UIViewToken None => default;

        public readonly long ViewId;

        private readonly UIViewSession _session;

        /// <summary>是否指向过一次打开；关闭后仍可查询终态，不代表界面当前存活。</summary>
        public bool IsValid => ViewId > 0;

        internal UIViewToken(UIViewSession session, long viewId)
        {
            _session = session;
            ViewId = viewId;
        }

        /// <summary>读取当前状态快照；默认 Token 返回 None，注销后仍可查询 Closed 或 Failed 终态。</summary>
        public UIViewStatus GetStatus() => _session?.GetStatus() ?? default;

        /// <summary>获取已附着且未请求关闭的指定类型 View，否则返回 null；返回值不代表已初始化或显示，也不转移实例所有权。</summary>
        public TView GetView<TView>() where TView : UIView => _session?.GetView<TView>();

        /// <summary>请求关闭会话；immediately 为 true 时跳过隐藏动画，默认 Token 或已释放会话为空操作。</summary>
        public void Close(bool immediately = false) => _session?.Close(immediately);

        /// <summary>通过取消凭证请求正常关闭，语义等同于 Close()。</summary>
        public void Cancel() => Close();

        /// <summary>按会话引用和打开编号判断是否为同一凭证，关闭不会改变身份。</summary>
        public bool Equals(UIViewToken other) => ReferenceEquals(_session, other._session) && ViewId == other.ViewId;

        /// <summary>判断对象是否为同一界面凭证。</summary>
        public override bool Equals(object obj) => obj is UIViewToken other && Equals(other);

        /// <summary>返回稳定会话身份对应的哈希值。</summary>
        public override int GetHashCode() => HashCode.Combine(_session, ViewId);
    }
}
