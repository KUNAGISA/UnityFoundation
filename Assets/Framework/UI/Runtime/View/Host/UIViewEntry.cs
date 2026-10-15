using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay.UI
{
    /// <summary>
    /// 保存一次 OpenView 创建的逻辑记录；生命周期编排统一由 UIViewHost 负责。
    /// </summary>
    internal sealed class UIViewEntry
    {
        private static long _viewIdGeneration;

        private UniTaskCompletionSource _readyCompletionSource;
        private UniTaskCompletionSource _shownCompletionSource;
        private UniTaskCompletionSource _closedCompletionSource;
        private bool _isReady;
        private bool _isRefreshing;

        internal UIViewToken Token { get; }
        internal UILayerId LayerId { get; }
        internal UIViewOptionMask Options { get; }
        internal bool AllowMultiple { get; }
        internal bool HideViewsBelow { get; }
        internal string Location { get; }
        internal UIViewData Data { get; private set; }
        internal UIView View { get; private set; }
        internal UIViewEntryState State { get; private set; } = UIViewEntryState.Loading;
        internal UIViewVisibilityState VisibilityState { get; private set; } = UIViewVisibilityState.Hidden;
        internal int DataVersion { get; private set; }
        internal int AppliedDataVersion { get; private set; }

        internal bool IsCloseRequested => State >= UIViewEntryState.Closing;
        internal bool IsReleasingOrReleased => State >= UIViewEntryState.Releasing;
        internal bool IsTargetVisible => VisibilityState is UIViewVisibilityState.Showing or UIViewVisibilityState.Shown;

        internal UIViewEntry(UIViewHost host, UIViewData data, UILayerId layerId)
        {
            Data = data;
            Token = new UIViewToken(host, Interlocked.Increment(ref _viewIdGeneration));
            LayerId = layerId;
            Options = data.Options;
            AllowMultiple = data.AllowMultiple;
            HideViewsBelow = data.HideViewsBelow;
            Location = data.Location;
        }

        internal void UpdateData(UIViewData data)
        {
            Data = data;
            DataVersion++;
        }

        internal void AttachView(UIView view) => View = view;

        internal bool TryBeginRefresh()
        {
            if (_isRefreshing || View == null)
            {
                return false;
            }

            _isRefreshing = true;
            return true;
        }

        internal void CommitDataVersion(int version) => AppliedDataVersion = version;

        internal void EndRefresh() => _isRefreshing = false;

        internal void MarkOpened() => State = UIViewEntryState.Opened;

        internal bool TryRequestClose(out UIViewEntryState previousState)
        {
            previousState = State;
            if (State >= UIViewEntryState.Releasing)
            {
                return false;
            }

            if (State < UIViewEntryState.Closing)
            {
                State = UIViewEntryState.Closing;
                CancelShownCompletion();
            }

            return true;
        }

        internal bool TryBeginRelease()
        {
            if (State >= UIViewEntryState.Releasing)
            {
                return false;
            }

            State = UIViewEntryState.Releasing;
            CancelShownCompletion();
            return true;
        }

        internal bool TryBeginShow()
        {
            if (State != UIViewEntryState.Opened || VisibilityState is not (UIViewVisibilityState.Hidden or UIViewVisibilityState.Hiding))
            {
                return false;
            }

            VisibilityState = UIViewVisibilityState.Showing;
            return true;
        }

        internal bool TryBeginHide()
        {
            if (IsReleasingOrReleased || VisibilityState is not (UIViewVisibilityState.Showing or UIViewVisibilityState.Shown))
            {
                return false;
            }

            VisibilityState = UIViewVisibilityState.Hiding;
            CancelShownCompletion();
            return true;
        }

        internal bool IsShowing => State == UIViewEntryState.Opened && VisibilityState == UIViewVisibilityState.Showing;

        internal bool IsHiding => !IsReleasingOrReleased && VisibilityState == UIViewVisibilityState.Hiding;

        internal bool TryCompleteShow(out UniTaskCompletionSource completionSource)
        {
            completionSource = null;
            if (!IsShowing)
            {
                return false;
            }

            VisibilityState = UIViewVisibilityState.Shown;
            completionSource = _shownCompletionSource;
            _shownCompletionSource = null;
            return true;
        }

        internal bool TryCompleteHide()
        {
            if (!IsHiding)
            {
                return false;
            }

            VisibilityState = UIViewVisibilityState.Hidden;
            return true;
        }

        internal UniTask GetReadyTask()
        {
            if (_isReady)
            {
                return UniTask.CompletedTask;
            }

            if (IsCloseRequested)
            {
                return UniTask.FromException(CreateReleasedBeforeReadyException());
            }

            _readyCompletionSource ??= new UniTaskCompletionSource();
            return _readyCompletionSource.Task;
        }

        internal UniTask GetShownTask()
        {
            if (IsCloseRequested || VisibilityState == UIViewVisibilityState.Shown)
            {
                return UniTask.CompletedTask;
            }

            _shownCompletionSource ??= new UniTaskCompletionSource();
            return _shownCompletionSource.Task;
        }

        internal UniTask GetClosedTask()
        {
            if (State == UIViewEntryState.Released)
            {
                return UniTask.CompletedTask;
            }

            _closedCompletionSource ??= new UniTaskCompletionSource();
            return _closedCompletionSource.Task;
        }

        internal void CompleteReady()
        {
            _isReady = true;
            var completionSource = _readyCompletionSource;
            _readyCompletionSource = null;
            completionSource?.TrySetResult();
        }

        internal void FailOpen(Exception exception)
        {
            var readyCompletionSource = _readyCompletionSource;
            _readyCompletionSource = null;
            readyCompletionSource?.TrySetException(exception);
            var shownCompletionSource = _shownCompletionSource;
            _shownCompletionSource = null;
            shownCompletionSource?.TrySetException(exception);
        }

        internal void CompleteRelease()
        {
            View = null;
            State = UIViewEntryState.Released;
            var readyCompletionSource = _readyCompletionSource;
            _readyCompletionSource = null;
            if (!_isReady)
            {
                readyCompletionSource?.TrySetException(CreateReleasedBeforeReadyException());
            }
            var closedCompletionSource = _closedCompletionSource;
            _closedCompletionSource = null;
            closedCompletionSource?.TrySetResult();
        }

        private void CancelShownCompletion()
        {
            var completionSource = _shownCompletionSource;
            _shownCompletionSource = null;
            completionSource?.TrySetCanceled();
        }

        private InvalidOperationException CreateReleasedBeforeReadyException() => new($"UI view token [{Token.ViewId}] was released before it finished opening.");

    }
}
