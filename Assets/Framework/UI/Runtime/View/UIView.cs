using Cysharp.Threading.Tasks;
using Gameplay.Common;
using UnityEngine;

namespace Gameplay.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIView : MonoBehaviour
    {
        /// <summary>目标可见性；Hide 开始即为 false，但 GameObject 可保持激活至动画结束。</summary>
        public bool IsShow { get; private set; }

        private VersionGate _transitionVersionGate;
        private UIViewToken _token;

        internal bool Validate(UIViewData data, out string reason) => ValidateData(data, out reason);

        internal void Bind(UIViewToken token) => _token = token;

        internal void Initialize(UIViewData data) => OnInitialize(data);

        internal void Refresh(UIViewData data) => OnRefresh(data);

        internal void Show() => PlayShowAsync().Forget();

        internal void Hide(bool immediately) => PlayHideAsync(immediately).Forget();

        private async UniTask PlayShowAsync()
        {
            var transitionVersion = _transitionVersionGate.Bump();
            var token = _token;
            IsShow = true;
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
            }

            if (IsTransitionValid(transitionVersion))
            {
                token.NotifyShowCompleted();
            }
        }

        private async UniTask PlayHideAsync(bool immediately)
        {
            var transitionVersion = _transitionVersionGate.Bump();
            var token = _token;
            var wasShown = IsShow;
            IsShow = false;
            try
            {
                // 已在隐藏中或尚未真正显示时，只收束过渡，不重复派发 OnHide。
                if (wasShown)
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
            }

            if (!IsTransitionValid(transitionVersion))
            {
                return;
            }

            DeactivateAfterHide();
            // SetActive(false) 的 OnDisable 也允许重入，不能通知已经失效的隐藏。
            if (IsTransitionValid(transitionVersion))
            {
                token.NotifyHideCompleted();
            }
        }

        internal void Close()
        {
            _transitionVersionGate.Bump();
            OnClose();
        }

        private void DeactivateAfterHide()
        {
            IsShow = false;
            if (this != null)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>关闭当前界面实例。</summary>
        protected void CloseSelf(bool immediately = false) => _token.Close(immediately);

        /// <summary>检查异步动画是否仍属于当前过渡。</summary>
        protected bool IsTransitionValid(in VersionToken transitionVersion) => transitionVersion == _transitionVersionGate.Current;

        private protected abstract bool ValidateData(UIViewData data, out string reason);

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
        private protected override bool ValidateData(UIViewData data, out string reason)
        {
            if (data is not TData)
            {
                reason = $"[{GetType()}] requires ViewData of type [{typeof(TData)}], but received [{data.GetType()}].";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private protected sealed override void OnInitialize(UIViewData data)
        {
            OnInitialize((TData)data);
        }

        private protected sealed override void OnRefresh(UIViewData data)
        {
            OnRefresh((TData)data);
        }

        protected virtual void OnInitialize(TData data) { }

        /// <summary>默认单实例界面位于当前层顶部时使用新的打开数据刷新。</summary>
        protected virtual void OnRefresh(TData data) { }
    }
}
