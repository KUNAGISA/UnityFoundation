using System;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Tables;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    public class GameProcedure : Procedure<GameProcedureEnterOptions>
    {
        public override ProcedureType ProcedureType => ProcedureType.Game;

        private SceneTransitionSystem _sceneTransitionSystem;

        private UIViewToken _loadViewToken = UIViewToken.None;
        private CameraSessionToken _playerCameraToken;
        private CancellationTokenSource _sceneTransitionCancellationSource;
        private AsyncLazy _sceneTransitionTask;

        protected override void OnInitialize()
        {
            _sceneTransitionSystem = Context.GetSystem<SceneTransitionSystem>();

            EnsureCapacity<SceneSystem>();
            EnsureCapacity<EntitySystem>();
            EnsureCapacity<PlayerInputSystem>();
            EnsureCapacity<InteractionSystem>();
            EnsureCapacity<SequenceSystem>();
            EnsureCapacity<SceneNpcSystem>();
            EnsureCapacity<SceneEventSystem>();
        }

        protected override async UniTask OnEnterAsync(GameProcedureEnterOptions options)
        {
            Context.GetService<IInputService>().PushMode(InputModeCatalog.Game).CancelWhenProcedureDestroy(Context);
            var saveData = options.SaveData;

            Context.ExecuteCommand(new InitializeCharacterStateCommand());
            if (saveData != null)
            {
                Context.ExecuteCommand(new RestoreGameSaveDataCommand(saveData));
            }

            var sceneId = saveData?.SceneId ?? options.InitialSceneId ?? Context.GetModel<TablesModel>().GlobalTable.InitialSceneId;
            await StartSceneTransitionAsync(new SceneTransitionRequest(sceneId, SceneTransitionReason.System), saveData != null);
        }

        protected override async UniTask OnTeardownAsync()
        {
            _sceneTransitionSystem.ClearPending();
            var cancellationSource = _sceneTransitionCancellationSource;
            var task = _sceneTransitionTask;
            cancellationSource?.Cancel();
            if (task != null)
            {
                await task.Task.SuppressCancellationThrow();
            }
        }

        protected override async UniTask OnExitAsync()
        {
            var cancellationToken = Context.LifetimeCancellationToken;
            try
            {
                if (IsLoadedScene)
                {
                    await LeaveCurrentSceneAsync(cancellationToken);
                    await UnloadSceneAsync(cancellationToken);
                }
            }
            finally
            {
                _loadViewToken.Close();
                _loadViewToken = UIViewToken.None;
                Context.GetModel<CharacterModel>().Cleanup();
                Context.GetModel<GameStateModel>().Reset();
            }
        }

        protected override void OnLateTick()
        {
            if (_sceneTransitionTask == null && _sceneTransitionSystem.TryConsumeTransition(out var request))
            {
                StartSceneTransitionAsync(request).Forget(Context.Fail);
            }
        }

        private UniTask StartSceneTransitionAsync(SceneTransitionRequest request, bool useSavedPlayerLocation = false)
        {
            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(Context.LifetimeCancellationToken);
            _sceneTransitionCancellationSource = cancellationSource;
            _sceneTransitionTask = UniTask.Lazy(() => ExecuteSceneTransitionAsync(request, useSavedPlayerLocation, cancellationSource));
            return _sceneTransitionTask.Task;
        }

        private async UniTask ExecuteSceneTransitionAsync(SceneTransitionRequest request, bool useSavedPlayerLocation, CancellationTokenSource cancellationSource)
        {
            var cancellationToken = cancellationSource.Token;
            var pauseToken = default(ProcedurePauseToken);
            try
            {
                pauseToken = Context.GetSystem<ProcedureSystem>().Pause(ProcedurePauseReason.SceneTransition);
                if (!Context.GetModel<TablesModel>().SceneTable.TryGet(request.SceneId, out var sceneCfg))
                {
                    throw new InvalidOperationException($"Scene config [{request.SceneId}] was not found.");
                }

                var transitionContext = new SceneTransitionContext(Context, request, sceneCfg);
                _loadViewToken = Context.GetSystem<UISystem>().OpenView(new LoadingView.Data());
                await _loadViewToken.WaitForShownAsync(cancellationToken);

                if (IsLoadedScene)
                {
                    await LeaveCurrentSceneAsync(cancellationToken);
                    await UnloadSceneAsync(cancellationToken);
                }

                await LoadSceneAsync(sceneCfg.Location, cancellationToken);
                await NotifyParallelAsync<IScenePreload>((system, token) => system.PreloadSceneAsync(transitionContext, token), cancellationToken);

                Context.GetModel<GameStateModel>().SetCurrentScene(request.SceneId);
                await NotifySystemsAsync<ISceneBeforeEnter>((system, token) => system.BeforeEnterSceneAsync(transitionContext, token), cancellationToken);
                await PreparePlayerAsync(request, useSavedPlayerLocation, cancellationToken);
                await NotifySystemsAsync<ISceneAfterEnter>((system, token) => system.AfterEnterSceneAsync(transitionContext, token), cancellationToken);

                Context.GetSystem<UISystem>().OpenView(new DemoMainView.Data());
                _loadViewToken.Close();
                _loadViewToken = UIViewToken.None;
            }
            catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
            {
            }
            finally
            {
                pauseToken.Cancel();
                cancellationSource.Dispose();
                _sceneTransitionCancellationSource = null;
                _sceneTransitionTask = null;
            }
        }

        private async UniTask PreparePlayerAsync(SceneTransitionRequest request, bool useSavedPlayerLocation, CancellationToken cancellationToken)
        {
            var routeModel = Context.GetModel<MovementRouteModel>();

            int? initRouteId = null;
            var initPosition = Vector2.zero;
            var initRotation = Quaternion.identity;

            if (useSavedPlayerLocation)
            {
                var spatial = Context.GetModel<CharacterModel>().PlayerCharacter.Spatial;
                initRouteId = spatial.RouteId;
                initPosition = spatial.Position;
                initRotation = spatial.Rotation;
            }

            var spawnPointModel = Context.GetModel<SpawnPointModel>();
            if (initRouteId == null && request.SpawnPointId.HasValue)
            {
                if (spawnPointModel.TryGet(request.SpawnPointId.Value, out var spawnPointData))
                {
                    initRouteId = spawnPointData.RouteId;
                    initPosition = spawnPointData.Position;
                    initRotation = spawnPointData.Rotation;
                }
                else
                {
                    Debug.LogError($"[Scene] 找不到出生点 [{request.SpawnPointId.Value}]，尝试使用默认出生点");
                }
            }

            if (initRouteId == null && spawnPointModel.TryGetDefault(out var defaultSpawnPointData))
            {
                initRouteId = defaultSpawnPointData.RouteId;
                initPosition = defaultSpawnPointData.Position;
                initRotation = defaultSpawnPointData.Rotation;
            }

            if (!initRouteId.HasValue || !routeModel.TryProject(initRouteId.Value, initPosition, out var initialLocation))
            {
                Debug.LogError(initRouteId.HasValue ? $"[Scene] 初始路线 [{initRouteId.Value}] 无法投影，尝试使用最近路线" : "[Scene] 当前场景没有可用出生点，尝试使用最近路线");
                if (!routeModel.TryProjectToClosestRoute(initPosition, out initialLocation))
                {
                    throw new InvalidOperationException("Failed to initialize player route location.");
                }
            }

            var playerEntity = Context.GetSystem<EntitySystem>().SpawnPlayer(initialLocation, initRotation);
            if (playerEntity == null)
            {
                throw new InvalidOperationException("Failed to spawn player entity.");
            }

            Context.GetSystem<PlayerInputSystem>().Bind(playerEntity);

            var playerCamera = Context.GetSystem<CameraSystem>().GetModule<PlayerCamera>();
            _playerCameraToken = await playerCamera.ActivateAsync(new PlayerCameraActivateOptions(playerEntity), cancellationToken);
        }

        private async UniTask LeaveCurrentSceneAsync(CancellationToken cancellationToken)
        {
            _playerCameraToken.Cancel();
            _playerCameraToken = default;
            await NotifySystemsReverseAsync<ISceneBeforeLeave>((system, token) => system.BeforeLeaveSceneAsync(token), cancellationToken);

            Context.GetModel<GameStateModel>().ClearCurrentScene();

            Context.GetSystem<UISystem>().CloseAll(UILayer.HUD, true);
            Context.GetSystem<UISystem>().CloseAll(UILayer.View, true);
            Context.GetSystem<UISystem>().CloseAll(UILayer.Popup, true);
        }
    }
}
