using Cysharp.Threading.Tasks;
using Aoiro;
using Gameplay.Asset;
using Gameplay.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.SceneManagement;

namespace Gameplay
{
    public interface IProcedure
    {
        ProcedureType ProcedureType { get; }

        void Initialize(ProcedureContext context);
        void Destroy();

#nullable enable
        UniTask EnterAsync(IProcedureEnterOptions? options);
#nullable disable
        UniTask ExitAsync();

        UniTask ReadyAsync();
        UniTask TeardownAsync();

        void FrameTick();
        void LateTick();

        void Paused();
        void Resumed();
    }
    
    public abstract class Procedure : IProcedure
    {
        public abstract ProcedureType ProcedureType { get; }

        protected ProcedureContext Context { get; private set; } = null;

        private ISceneAsyncOperation _sceneAsyncOperation = null;
        protected bool IsLoadedScene => _sceneAsyncOperation?.IsValid ?? false;

        private readonly List<IProcedureCapacitySystem> _activeCapacitySystems = new();

        void IProcedure.Initialize(ProcedureContext context)
        {
            Context = context;
            try
            {
                OnInitialize();
            }
            catch
            {
                Context = null;
                throw;
            }
        }

        void IProcedure.Destroy()
        {
            var exceptions = new CleanupExceptionCollector();
            try
            {
                OnDestroy();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }

            //反序注销，防止数据依赖问题
            for (var index = _activeCapacitySystems.Count - 1; index >= 0; --index)
            {
                try
                {
                    _activeCapacitySystems[index].Detach(Context);
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }
            _activeCapacitySystems.Clear();

            Context = null;
            exceptions.ThrowIfAny();
        }

        UniTask IProcedure.EnterAsync(IProcedureEnterOptions options)
        {
            return OnEnterAsync(options);
        }

        UniTask IProcedure.ExitAsync()
        {
            return OnExitAsync();
        }

        async UniTask IProcedure.ReadyAsync()
        {
            for (var index = 0; index < _activeCapacitySystems.Count; ++index)
            {
                await _activeCapacitySystems[index].ReadyAsync(Context);
            }
            await OnReadyAsync();
        }

        async UniTask IProcedure.TeardownAsync()
        {
            var exceptions = new CleanupExceptionCollector();
            try
            {
                await OnTeardownAsync();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
            for (var index = _activeCapacitySystems.Count - 1; index >= 0; --index)
            {
                try
                {
                    await _activeCapacitySystems[index].TeardownAsync(Context);
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }
            exceptions.ThrowIfAny();
        }

        void IProcedure.FrameTick()
        {
            OnFrameTick();
        }

        void IProcedure.LateTick()
        {
            OnLateTick();
        }

        void IProcedure.Paused()
        {
            OnPaused();
        }

        void IProcedure.Resumed()
        {
            OnResumed();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnDestroy() { }

#nullable enable
        protected virtual UniTask OnEnterAsync(IProcedureEnterOptions? options) => UniTask.CompletedTask;
#nullable disable
        protected virtual UniTask OnExitAsync() => UniTask.CompletedTask;

        protected virtual UniTask OnReadyAsync() => UniTask.CompletedTask;
        protected virtual UniTask OnTeardownAsync() => UniTask.CompletedTask;

        protected virtual void OnFrameTick() { }
        protected virtual void OnLateTick() { }

        protected virtual void OnPaused() { }
        protected virtual void OnResumed() { }
        
        /// <summary>
        /// 附加Procedure功能
        /// </summary>
        /// <typeparam name="TSystem">Procedure功能系统</typeparam>
        /// <returns>附加的功能系统</returns>
        protected TSystem EnsureCapacity<TSystem>() where TSystem : class, IProcedureCapacitySystem
        {
            var system = Context.GetSystem<TSystem>();
            if (!_activeCapacitySystems.Contains(system))
            {
                system.Attach(Context);
                _activeCapacitySystems.Add(system);
            }
            return system;
        }

        protected void NotifySystems<T>(Action<T> callback) where T : class
        {
            foreach (var system in _activeCapacitySystems)
            {
                if (system is T target)
                {
                    callback(target);
                }
            }
        }

        protected async UniTask NotifySystemsAsync<T>(Func<T, CancellationToken, UniTask> callback, CancellationToken cancellationToken = default) where T : class
        {
            foreach (var system in _activeCapacitySystems)
            {
                if (system is T target)
                {
                    await callback(target, cancellationToken);
                }
            }
        }

        protected async UniTask NotifySystemsReverseAsync<T>(Func<T, CancellationToken, UniTask> callback, CancellationToken cancellationToken = default) where T : class
        {
            for (var index = _activeCapacitySystems.Count - 1; index >= 0; --index)
            {
                if (_activeCapacitySystems[index] is T target)
                {
                    await callback(target, cancellationToken);
                }
            }
        }

        protected async UniTask NotifyParallelAsync<T>(Func<T, CancellationToken, UniTask> callback, CancellationToken cancellationToken = default) where T : class
        {
            List<UniTask> tasks = null;
            foreach (var system in _activeCapacitySystems)
            {
                if (system is T target)
                {
                    tasks ??= new List<UniTask>();
                    tasks.Add(callback(target, cancellationToken));
                }
            }
            if (tasks != null)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        protected async UniTask LoadSceneAsync(string location, CancellationToken cancellationToken, IProgress<float> progress = null)
        {
            _sceneAsyncOperation = Context.GetService<IAssetService>().LoadSceneAsync("Scenes/" + location, LoadSceneMode.Additive);
            await _sceneAsyncOperation.ToUniTask(cancellationToken: cancellationToken, progress: progress);
            SceneManager.SetActiveScene(_sceneAsyncOperation.Result);
            await UniTask.NextFrame(PlayerLoopTiming.EarlyUpdate, cancellationToken: cancellationToken);
        }

        protected async UniTask UnloadSceneAsync(CancellationToken cancellationToken, IProgress<float> progress = null)
        {
            if (_sceneAsyncOperation != null)
            {
                await _sceneAsyncOperation.UnloadAsync(true).ToUniTask(cancellationToken: cancellationToken, progress: progress);
                _sceneAsyncOperation = null;
            }
        }

    }

    public abstract class Procedure<TOptions> : Procedure where TOptions : class
    {
        protected sealed override UniTask OnEnterAsync(IProcedureEnterOptions options)
        {
            if (options is TOptions target)
            {
                return OnEnterAsync(target);
            }
            throw new ArgumentException($"Procedure [{GetType()}] requires enter options of type [{typeof(TOptions)}], but received [{options?.GetType()}].", nameof(options));
        }
        
#nullable enable
        protected virtual UniTask OnEnterAsync(TOptions? options)
#nullable disable
        {
            return UniTask.CompletedTask;
        }
        
    }
}
