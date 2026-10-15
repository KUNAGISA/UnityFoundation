using System;
using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace Gameplay
{
    public sealed class AssetPreloadSystem : SystemBase
    {
        private const string GlobalPreloadLabel = "preload-global";

        private IAssetAsyncOperation<IList<UObject>> _globalOperation;
        private UniTaskCompletionSource _globalLoadCompletion;
        private bool _globalReady;
        private CancellationTokenSource _lifetimeCancellationToken;

        protected override void OnInitialize()
        {
            _lifetimeCancellationToken = new CancellationTokenSource();
        }

        protected override void OnShutdown()
        {
            var completion = _globalLoadCompletion;
            var cancellationToken = _lifetimeCancellationToken?.Token ?? CancellationToken.None;
            _lifetimeCancellationToken?.Cancel();
            completion?.TrySetCanceled(cancellationToken);
            _lifetimeCancellationToken?.Dispose();
            _lifetimeCancellationToken = null;
            ReleaseGlobalOperation();
        }

        public async UniTask PreloadGlobalAssetsAsync(CancellationToken cancellationToken, IProgress<float> progress = null)
        {
            if (_globalReady)
            {
                progress?.Report(1f);
                return;
            }

            var completion = _globalLoadCompletion;
            if (completion == null)
            {
                completion = new UniTaskCompletionSource();
                _globalLoadCompletion = completion;
                LoadGlobalAssetsAsync(completion).Forget();
            }

            await completion.Task.AttachExternalCancellation(cancellationToken);
            progress?.Report(1f);
        }

        private async UniTask LoadGlobalAssetsAsync(UniTaskCompletionSource completion)
        {
            IAssetAsyncOperation<IList<UObject>> operation = null;
            var lifetimeCancellationToken = default(CancellationToken);
            try
            {
                lifetimeCancellationToken = _lifetimeCancellationToken.Token;
                operation = this.GetService<IAssetService>().LoadAssetsAsync<UObject>(GlobalPreloadLabel);
                _globalOperation = operation;
                var assets = await operation.ToUniTask(cancellationToken: lifetimeCancellationToken);
                if (!IsCurrentLoad(operation, completion))
                {
                    completion.TrySetCanceled(lifetimeCancellationToken);
                    return;
                }
                WarmupShaderVariants(assets);
                _globalReady = true;
                completion.TrySetResult();
            }
            catch (OperationCanceledException exception)
            {
                if (IsCurrentLoad(operation, completion))
                {
                    ReleaseGlobalOperation();
                }
                completion.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                if (IsCurrentLoad(operation, completion))
                {
                    ReleaseGlobalOperation();
                }
                completion.TrySetException(exception);
            }
        }

        private bool IsCurrentLoad(IAssetAsyncOperation<IList<UObject>> operation, UniTaskCompletionSource completion)
        {
            return ReferenceEquals(_globalOperation, operation) && ReferenceEquals(_globalLoadCompletion, completion);
        }

        private static void WarmupShaderVariants(IList<UObject> assets)
        {
            if (assets == null)
            {
                return;
            }
            for (var index = 0; index < assets.Count; index++)
            {
                if (assets[index] is ShaderVariantCollection shaderVariants)
                {
                    shaderVariants.WarmUp();
                }
            }
        }

        private void ReleaseGlobalOperation()
        {
            var operation = _globalOperation;
            _globalOperation = null;
            _globalLoadCompletion = null;
            _globalReady = false;
            operation?.Release();
        }
    }
}
