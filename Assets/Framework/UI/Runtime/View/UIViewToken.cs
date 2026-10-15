using System;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;

namespace Gameplay.UI
{
    public readonly struct UIViewToken : IEquatable<UIViewToken>, ICancelToken
    {
        public static UIViewToken None => default;

        public readonly long ViewId;

        private readonly UIViewHost _host;

        public bool IsValid => ViewId > 0;

        internal UIViewToken(UIViewHost host, long viewId)
        {
            _host = host;
            ViewId = viewId;
        }

        public TView GetView<TView>() where TView : UIView => _host?.GetView<TView>(this);

        public UniTask WaitForReadyAsync(CancellationToken cancellationToken = default)
        {
            if (_host == null)
            {
                return cancellationToken.IsCancellationRequested ? UniTask.FromCanceled(cancellationToken) : UniTask.CompletedTask;
            }

            return _host.WaitForReadyAsync(this, cancellationToken);
        }

        public UniTask WaitForShownAsync(CancellationToken cancellationToken = default)
        {
            if (_host == null)
            {
                return cancellationToken.IsCancellationRequested ? UniTask.FromCanceled(cancellationToken) : UniTask.CompletedTask;
            }

            return _host.WaitForShownAsync(this, cancellationToken);
        }

        public UniTask WaitForClosedAsync(CancellationToken cancellationToken = default)
        {
            if (_host == null)
            {
                return cancellationToken.IsCancellationRequested ? UniTask.FromCanceled(cancellationToken) : UniTask.CompletedTask;
            }

            return _host.WaitForClosedAsync(this, cancellationToken);
        }

        public void Close(bool immediately = false) => _host?.CloseView(this, immediately);

        public void Cancel() => Close();

        public bool Equals(UIViewToken other) => ReferenceEquals(_host, other._host) && ViewId == other.ViewId;

        public override bool Equals(object obj) => obj is UIViewToken other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_host, ViewId);
    }
}
