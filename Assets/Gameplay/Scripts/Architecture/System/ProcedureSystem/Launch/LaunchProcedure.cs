using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using Gameplay.Tables;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    public sealed class LaunchProcedure : Procedure<LaunchProcedureOptions>
    {
        private const float LaunchLoadingViewMinimumDuration = 0.5f;

        public override ProcedureType ProcedureType => ProcedureType.Launch;

        private UIViewToken _launchLoadingViewToken = default;

        protected override async UniTask OnEnterAsync(LaunchProcedureOptions options)
        {
            var cancellationToken = Context.LifetimeCancellationToken;

            await Context.GetModel<GlobalSettingModel>().LoadAsync(cancellationToken);
            await Context.GetService<IAssetService>().InitializeAsync(cancellationToken);
            await Context.GetSystem<CameraSystem>().InitializeAsync(cancellationToken);
            await Context.GetSystem<UISystem>().InitializeAsync(cancellationToken);

            var progress = new CompositeProgress();
            var tableProgress = progress.Add(25f);
            var preloadProgress = progress.Add(25f);
            var localizationProgress = progress.Add(10f);
            var consoleProgress = progress.Add(5f);

            var loadingTask = UniTask.WhenAll(
                Context.GetModel<TablesModel>().LoadAllAsync(cancellationToken, tableProgress),
                Context.GetSystem<AssetPreloadSystem>().PreloadGlobalAssetsAsync(cancellationToken, preloadProgress),
                Context.GetSystem<LocalizationSystem>().InitializeAsync(cancellationToken, localizationProgress),
                Context.GetSystem<ConsoleSystem>().InitializeAsync(cancellationToken, consoleProgress)).Preserve();

            await options.Splash.WaitForSplashAsync(cancellationToken);

            var loadingViewShownAt = -1d;
            if (!loadingTask.Status.IsCompleted())
            {
                var uiSystem = Context.GetSystem<UISystem>();
                _launchLoadingViewToken = uiSystem.OpenView(new LaunchLoadingView.Data(progress));
                await _launchLoadingViewToken.WaitForShownAsync(cancellationToken);
                loadingViewShownAt = Time.realtimeSinceStartupAsDouble;
                options.Splash.HideSplash();
            }

            await loadingTask;

            if (loadingViewShownAt >= 0d)
            {
                var remainingDuration = LaunchLoadingViewMinimumDuration - (float)(Time.realtimeSinceStartupAsDouble - loadingViewShownAt);
                if (remainingDuration > 0f)
                {
                    await UniTask.WaitForSeconds(remainingDuration, true, PlayerLoopTiming.EarlyUpdate, cancellationToken);
                }
            }

            var previewSceneId = options.PreviewSceneId;
            Context.GetSystem<ProcedureSystem>().Transition(new TransitionProcedureRequest(previewSceneId.HasValue ? ProcedureType.Game : ProcedureType.MainMenu)
            {
                Reason = TransitionProcedureReason.Initialize,
                Options = previewSceneId.HasValue ? new GameProcedureEnterOptions(initialSceneId: previewSceneId.Value) : null,
            });
        }

        protected override UniTask OnExitAsync()
        {
            _launchLoadingViewToken.Close();
            _launchLoadingViewToken = default;
            return base.OnExitAsync();
        }
    }
}
