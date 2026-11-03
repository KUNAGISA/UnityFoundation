using Cysharp.Threading.Tasks;
using Gameplay.Common;
using UnityEngine;

namespace Gameplay.UI
{
    /// <summary>承载界面表现和异步动画；生命周期状态与实例所有权由绑定的 Session 管理。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIView : MonoBehaviour
    {
        /// <summary>目标可见性；Hide 开始即为 false，但 GameObject 可保持激活至动画结束。</summary>
        public bool IsTargetVisible { get; private set; }

        private VersionGate _transitionVersionGate;
        private UIViewSession _session;

        /// <summary>绑定当前实例所属会话；归还池前解绑，避免复用实例继续持有旧会话。</summary>
        internal void Bind(UIViewSession session) => _session = session;

        internal void Initialize(UIViewData data) => OnInitialize(data);

        internal void Refresh(UIViewData data) => OnRefresh(data);

        /// <summary>激活并播放展示动画；版本仍有效时才通知完成，动画异常只记录，不发布成功状态。</summary>
        internal async UniTask Show()
        {
            var transitionVersion = _transitionVersionGate.Bump();
            var session = _session;
            IsTargetVisible = true;
            try
            {
                gameObject.SetActive(true);
                if (!IsTransitionValid(transitionVersion))
                {
                    return;
                }

                OnShow();
                if (!IsTransitionValid(transitionVersion))
                {
                    return;
                }

                await OnPlayShowAnimation(transitionVersion);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return;
            }

            if (IsTransitionValid(transitionVersion))
            {
                session.CompleteShow();
            }
        }

        /// <summary>播放隐藏动画后先停用对象，再通知会话；立即隐藏跳过动画，版本校验隔离反转和同步回调。</summary>
        internal async UniTask Hide(bool immediately)
        {
            var transitionVersion = _transitionVersionGate.Bump();
            var session = _session;
            var wasTargetVisible = IsTargetVisible;
            IsTargetVisible = false;
            try
            {
                // 已在隐藏中或尚未真正显示时，只收束过渡，不重复派发 OnHide。
                if (wasTargetVisible)
                {
                    OnHide();
                    if (!IsTransitionValid(transitionVersion))
                    {
                        return;
                    }

                    if (!immediately)
                    {
                        await OnPlayHideAnimation(transitionVersion);
                    }
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return;
            }

            if (!IsTransitionValid(transitionVersion))
            {
                return;
            }

            if (this != null)
            {
                gameObject.SetActive(false);
            }
            // SetActive(false) 的 OnDisable 也允许重入，不能通知已经失效的隐藏。
            if (IsTransitionValid(transitionVersion))
            {
                session.CompleteHide();
            }
        }

        /// <summary>使未完成的过渡失效，再执行永久关闭清理；普通遮挡不会调用此方法。</summary>
        internal void Close()
        {
            _transitionVersionGate.Bump();
            OnClose();
        }

        /// <summary>关闭当前界面实例。</summary>
        protected void CloseSelf(bool immediately = false) => _session?.Close(immediately);

        /// <summary>检查异步动画是否仍属于当前过渡。</summary>
        protected bool IsTransitionValid(in VersionToken transitionVersion) => transitionVersion == _transitionVersionGate.Current;

        private protected abstract void OnInitialize(UIViewData data);

        private protected abstract void OnRefresh(UIViewData data);

        /// <summary>界面完成隐藏并即将永久释放时，清理业务订阅和长生命周期异步工作。</summary>
        protected virtual void OnClose() { }

        /// <summary>目标切换为可见时、展示动画前调用。</summary>
        protected virtual void OnShow() { }

        /// <summary>目标切换为隐藏时、隐藏动画前调用；普通遮挡和永久关闭都会经过这里。</summary>
        protected virtual void OnHide() { }

        /// <summary>播放展示动画；耗时逻辑应使用 transitionVersion 忽略被 Hide 或新 Show 打断后的结果。</summary>
        protected virtual UniTask OnPlayShowAnimation(VersionToken transitionVersion) => UniTask.CompletedTask;

        /// <summary>播放隐藏动画；动画期间 GameObject 仍可激活，耗时逻辑应使用 transitionVersion 忽略反转后的结果。</summary>
        protected virtual UniTask OnPlayHideAnimation(VersionToken transitionVersion) => UniTask.CompletedTask;

#if UNITY_EDITOR
        [ContextMenu("生成绑定代码", false, 10000)]
        private void GenerateCode()
        {
            UIBindingCodeBridge.Generate?.Invoke(this);
        }
#endif
    }

    public abstract class UIView<TData> : UIView where TData : UIViewData
    {
        private protected sealed override void OnInitialize(UIViewData data)
        {
            OnInitialize((TData)data);
        }

        private protected sealed override void OnRefresh(UIViewData data)
        {
            OnRefresh((TData)data);
        }

        /// <summary>初始化界面；生命周期回调中不得重入打开或刷新同一 Session。</summary>
        protected virtual void OnInitialize(TData data) { }

        /// <summary>顶部单实例使用新 Data 同步刷新一次；不得重入打开或刷新同一 Session。</summary>
        protected virtual void OnRefresh(TData data) { }
    }
}
