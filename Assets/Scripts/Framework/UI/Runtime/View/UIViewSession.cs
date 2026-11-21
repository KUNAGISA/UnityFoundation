using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Gameplay.UI
{
    /// <summary>拥有一次界面打开的完整生命周期；全局冲突与注册由 Host 协调。</summary>
    internal sealed class UIViewSession
    {
        private static long _viewIdGeneration;

        private UIViewHost _host;
        private UILayer _layer;
        private IUIViewInstanceProvider _instanceProvider;
        private UIViewExtensionContainer _extensions;
        private bool _isInitializing;
        private bool _isReleasing;

        internal UIViewToken Token { get; }
        internal UILayerId LayerId { get; }
        internal UIViewOptionMask Options { get; }
        internal bool AllowMultiple { get; }
        internal bool HideViewsBelow { get; }
        internal string Location { get; }
        internal UIViewData Data { get; private set; }
        internal UIView View { get; private set; }
        internal UIViewLifecycle Lifecycle { get; private set; } = UIViewLifecycle.Loading;
        internal UIViewVisibilityState Visibility { get; private set; } = UIViewVisibilityState.Hidden;

        internal bool IsCloseRequested => Lifecycle is UIViewLifecycle.Closing or UIViewLifecycle.Closed or UIViewLifecycle.Failed;
        internal bool IsReleasingOrReleased => _isReleasing || Lifecycle is UIViewLifecycle.Closed or UIViewLifecycle.Failed;
        internal bool IsTargetVisible => Visibility is UIViewVisibilityState.Showing or UIViewVisibilityState.Shown;

        /// <summary>创建稳定的 Token 身份并快照打开配置；后续刷新只替换 Data。</summary>
        internal UIViewSession(UIViewHost host, UILayer layer, IUIViewInstanceProvider instanceProvider, UIViewExtensionContainer extensions, UIViewData data, UILayerId layerId)
        {
            _host = host;
            _layer = layer;
            _instanceProvider = instanceProvider;
            _extensions = extensions;
            Data = data;
            Token = new UIViewToken(this, Interlocked.Increment(ref _viewIdGeneration));
            LayerId = layerId;
            Options = data.Options;
            AllowMultiple = data.AllowMultiple;
            HideViewsBelow = data.HideViewsBelow;
            Location = data.Location;
        }

        internal UIViewStatus GetStatus() => new(Lifecycle, Visibility);

        internal TView GetView<TView>() where TView : UIView => IsCloseRequested ? null : View as TView;

        /// <summary>加载并接管实例，初始化后协调单实例替换、提交层顺序，再发布 Opened 并重算可见性。</summary>
        /// <remarks>打开失败在内部记录并回滚；每个可能触发关闭的调用后都需重新检查状态，初始化栈内的释放由此处收束。</remarks>
        internal async UniTask OpenAsync(CancellationToken cancellationToken)
        {
            // Session 可以在等待期间释放；晚到实例仍由本次请求捕获的 Provider 归还。
            var provider = _instanceProvider;
            var location = Location;
            var parent = _layer.PrimaryRoot;
            GameObject instance = null;
            var instanceAttached = false;
            try
            {
                instance = await provider.InstantiateAsync(location, parent, cancellationToken);
                if (IsReleasingOrReleased)
                {
                    ReleaseInstance(provider, location, instance);
                    return;
                }

                if (instance == null)
                {
                    throw new InvalidOperationException($"UI instance provider returned null for [{location}].");
                }

                if (!instance.TryGetComponent<UIView>(out var view))
                {
                    throw new InvalidOperationException($"UI instance [{location}] does not contain a {nameof(UIView)} component.");
                }

                View = view;
                instanceAttached = true;
                _isInitializing = true;
                view.Bind(this);
                // 加载期间的打开请求只替换 Data，交付后直接使用最新值初始化一次。
                view.Initialize(Data);
                if (IsCloseRequested)
                {
                    Release();
                    return;
                }

                _host.ResolveSingleInstanceConflicts(this);
                if (IsCloseRequested)
                {
                    Release();
                    return;
                }

                _layer.UpdateSiblingOrder(this);
                if (IsCloseRequested)
                {
                    Release();
                    return;
                }

                _isInitializing = false;
                Lifecycle = UIViewLifecycle.Opened;
                _layer.RefreshVisibility();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (!instanceAttached)
                {
                    ReleaseInstance(provider, location, instance);
                }
                Release();
            }
            catch (Exception exception)
            {
                if (!instanceAttached)
                {
                    ReleaseInstance(provider, location, instance);
                }

                if (!IsReleasingOrReleased)
                {
                    Debug.LogException(exception);
                    Release(UIViewLifecycle.Failed);
                }
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>加载期间只保存最新 Data；打开后同步刷新 View 和扩展，不支持回调中重入刷新同一会话。</summary>
        internal void Refresh(UIViewData data)
        {
            Data = data;
            if (Lifecycle != UIViewLifecycle.Opened)
            {
                return;
            }

            View.Refresh(data);
            if (IsCloseRequested)
            {
                return;
            }

            var context = CreateContext(IsTargetVisible);
            _extensions?.Refreshed(in context);
        }

        /// <summary>应用 Layer 推导出的目标可见性；支持显示与隐藏反转，动画完成后再提交可见性终态。</summary>
        internal void SetVisible(bool visible)
        {
            if (Lifecycle != UIViewLifecycle.Opened)
            {
                return;
            }

            if (visible)
            {
                if (Visibility is UIViewVisibilityState.Hidden or UIViewVisibilityState.Hiding)
                {
                    Visibility = UIViewVisibilityState.Showing;
                    var context = CreateContext(true);
                    _extensions?.BeforeShow(in context);
                    if (Lifecycle == UIViewLifecycle.Opened && Visibility == UIViewVisibilityState.Showing)
                    {
                        View.Show().Forget();
                    }
                }
            }
            else
            {
                StartHide(false);
            }
        }

        /// <summary>接收当前有效展示过渡的完成通知，提交 Shown 后通知扩展。</summary>
        internal void CompleteShow()
        {
            if (Lifecycle != UIViewLifecycle.Opened || Visibility != UIViewVisibilityState.Showing)
            {
                return;
            }

            Visibility = UIViewVisibilityState.Shown;
            var context = CreateContext(true);
            _extensions?.ShowCompleted(in context);
        }

        /// <summary>接收当前有效隐藏过渡的完成通知；普通遮挡保留实例，永久关闭则继续释放。</summary>
        internal void CompleteHide()
        {
            if (IsReleasingOrReleased || Visibility != UIViewVisibilityState.Hiding)
            {
                return;
            }

            Visibility = UIViewVisibilityState.Hidden;
            if (Lifecycle == UIViewLifecycle.Closing)
            {
                Release();
            }
        }

        /// <summary>进入关闭阶段；加载中可直接结束，初始化栈内推迟释放，显示中的界面先完成隐藏。</summary>
        internal void Close(bool immediately)
        {
            if (IsReleasingOrReleased)
            {
                return;
            }

            Lifecycle = UIViewLifecycle.Closing;
            // Initialize/Refresh 所在的初始化栈退出后，由 OpenAsync 统一归还实例。
            if (_isInitializing)
            {
                return;
            }

            if (View == null || Visibility == UIViewVisibilityState.Hidden)
            {
                Release();
            }
            else if (Visibility == UIViewVisibilityState.Hiding)
            {
                if (immediately)
                {
                    View.Hide(true).Forget();
                }
            }
            else
            {
                StartHide(immediately);
            }
        }

        /// <summary>先提交 Hiding 再通知扩展；回调可能关闭会话，状态仍有效时才启动隐藏。</summary>
        private void StartHide(bool immediately)
        {
            if (Visibility is not (UIViewVisibilityState.Showing or UIViewVisibilityState.Shown))
            {
                return;
            }

            Visibility = UIViewVisibilityState.Hiding;
            var context = CreateContext(false);
            _extensions?.BeforeHide(in context);
            if (!IsReleasingOrReleased && Visibility == UIViewVisibilityState.Hiding)
            {
                View.Hide(immediately).Forget();
            }
        }

        /// <summary>永久释放：OnClose → 层移除与重算 → 扩展反序释放 → 实例归还 → 宿主注销 → 清空引用并发布终态。</summary>
        /// <remarks>先进入释放阶段隔离重复关闭；只有打开失败使用 Failed，动画异常不由此强制收束。</remarks>
        private void Release(UIViewLifecycle finalLifecycle = UIViewLifecycle.Closed)
        {
            if (IsReleasingOrReleased)
            {
                return;
            }

            _isReleasing = true;
            Lifecycle = UIViewLifecycle.Closing;
            var view = View;
            if (view != null)
            {
                try
                {
                    view.Close();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            if (_layer.RemoveSession(this))
            {
                _layer.RefreshVisibility();
            }

            var context = CreateContext(IsTargetVisible);
            _extensions?.Released(in context);
            if (view != null)
            {
                view.Bind(null);
                ReleaseInstance(_instanceProvider, Location, view.gameObject);
            }

            _host.UnregisterSession(this);
            View = null;
            Data = null;
            _host = null;
            _layer = null;
            _instanceProvider = null;
            _extensions = null;
            Visibility = UIViewVisibilityState.Hidden;
            Lifecycle = finalLifecycle;
            _isReleasing = false;
        }

        private UIViewExtensionContext CreateContext(bool visible) => new(_host, Token, Data, View, LayerId, Options, visible);

        /// <summary>通过实例所属的 Provider 归还资源；释放异常记录到日志，不阻断其余会话清理。</summary>
        private static void ReleaseInstance(IUIViewInstanceProvider provider, string location, GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            try
            {
                provider.Release(location, instance);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
