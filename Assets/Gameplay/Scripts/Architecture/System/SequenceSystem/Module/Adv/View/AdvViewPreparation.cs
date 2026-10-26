using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Aoiro.Sequence;
using Cysharp.Threading.Tasks;
using Gameplay.UI;

namespace Gameplay.Sequence
{
    internal sealed class AdvViewPreparation : SequencePreparation
    {
        private readonly AdvViewSession _session;
        private readonly UIViewToken _token;
        private CancellationTokenSource _cancellation;
        private Exception _exception;
        private bool _isCompleted;

        internal AdvViewPreparation(AdvViewSession session, UIViewToken token)
        {
            _session = session;
            _token = token;
        }

        protected override void OnBegin()
        {
            _cancellation = new CancellationTokenSource();
            WaitForReadyAsync(_cancellation.Token).Forget();
        }

        protected override bool EvaluateCompletion()
        {
            if (_exception != null)
            {
                ExceptionDispatchInfo.Capture(_exception).Throw();
            }

            return _isCompleted;
        }

        protected override void OnCancel() => _cancellation?.Cancel();

        protected override void OnCleanup()
        {
            _cancellation?.Dispose();
            _cancellation = null;
        }

        private async UniTask WaitForReadyAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _token.WaitForReadyAsync(cancellationToken);

                if (!_session.IsViewBound)
                {
                    throw new InvalidOperationException($"AdvView [{_token.ViewId}] did not bind its session during initialization.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _exception = exception;
            }
            finally
            {
                _isCompleted = true;
            }
        }
    }
}
