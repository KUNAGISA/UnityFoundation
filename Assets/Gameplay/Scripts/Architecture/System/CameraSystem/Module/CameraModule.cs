using Aoiro;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Gameplay
{
    public abstract class CameraModule : ICameraModule
    {
        protected ICameraContext Context { get; private set; } = null;

        void ICameraModule.Initialize(ICameraContext context)
        {
            Context = context;
            try
            {
                OnInitialize();
            }
            catch
            {
                try
                {
                    OnShutdown();
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                }
                finally
                {
                    Context = null;
                }
                throw;
            }
        }

        UniTask ICameraModule.ReadyAsync(CancellationToken cancellationToken) => OnReadyAsync(cancellationToken);

        void ICameraModule.Shutdown()
        {
            try
            {
                OnShutdown();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                Context = null;
            }
        }

        protected abstract void OnInitialize();
        protected virtual UniTask OnReadyAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
        protected abstract void OnShutdown();
    }
}
