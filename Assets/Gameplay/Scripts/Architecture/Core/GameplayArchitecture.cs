using Aoiro;
using System.Collections.Generic;
using Gameplay.Asset;
using Gameplay.Common;
using Gameplay.Scheduler;
using Gameplay.Storage;
using Gameplay.Tables;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Gameplay
{
    public class GameplayArchitecture : Architecture<GameplayArchitecture>
    {
        private readonly UnityTimeSource _unityTimeSource = new UnityTimeSource();

        private IFrameTickable _schedulerDriver;
        private IFrameTickable _procedureDriver;
        private IFrameTickable _cameraDriver;
        private FrameTime _frameTime;
#if UNITY_EDITOR
        private TableFileWatchSystem _tableFileWatchSystem;
#endif

        protected override void OnInitialize()
        {
            OnInitializeCommandModule();

#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                OnInitializeRuntimeModule();
            }

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopBootstrap.InsertTo<EarlyUpdate>(ref playerLoop, typeof(GameplayArchitecture), EarlyTick);
            PlayerLoopBootstrap.InsertTo<Update>(ref playerLoop, typeof(GameplayArchitecture), FrameTick);
            PlayerLoopBootstrap.InsertTo<PreLateUpdate>(ref playerLoop, typeof(GameplayArchitecture), LateTick);
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        protected override void OnShutdown()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopBootstrap.RemoveFrom<EarlyUpdate>(ref playerLoop, typeof(GameplayArchitecture));
            PlayerLoopBootstrap.RemoveFrom<Update>(ref playerLoop, typeof(GameplayArchitecture));
            PlayerLoopBootstrap.RemoveFrom<PreLateUpdate>(ref playerLoop, typeof(GameplayArchitecture));
            PlayerLoop.SetPlayerLoop(playerLoop);

            _schedulerDriver = null;
            _procedureDriver = null;
            _cameraDriver = null;
            _frameTime = default;
#if UNITY_EDITOR
            _tableFileWatchSystem = null;
#endif
        }

        //初始化通用模块，非运行时也会执行
        private void OnInitializeCommandModule()
        {
            var schedulerSystem = new SchedulerSystem();
            _schedulerDriver = schedulerSystem;

            RegisterModule(schedulerSystem);
            RegisterModule(new TablesModel());
            RegisterModule(new GlobalSettingModel());
#if UNITY_EDITOR
            _tableFileWatchSystem = new TableFileWatchSystem();
            RegisterModule(_tableFileWatchSystem);
#endif
            RegisterModule(new LocalizationSystem());
            
            RegisterModule<IAssetService>(new AddressableAssetService());
            RegisterModule<IStorageService>(new FileStorageService("./SaveData"));
        }

        //初始化运行时模块，非运行时不会执行
        private void OnInitializeRuntimeModule()
        {
            var procedureSystem = new ProcedureSystem();
            _procedureDriver = procedureSystem;

            var cameraSystem = new CameraSystem();
            cameraSystem.RegisterCameraModule(new PlayerCamera("Cameras/PlayerCameraSettings"));
            cameraSystem.RegisterCameraModule(new ArtCamera("Cameras/ArtCameraSettings"));
            _cameraDriver = cameraSystem;

            RegisterModule(new AssetPreloadSystem());
            RegisterModule(procedureSystem);
            RegisterModule(cameraSystem);
            RegisterModule(new PlayerInputSystem());
            RegisterModule(new EntitySystem());
            RegisterModule(new InteractionSystem());
            RegisterModule(new UISystem());
            RegisterModule(new ConsoleSystem());
            RegisterModule(new SceneTransitionSystem());
            RegisterModule(new SceneSystem());
            RegisterModule(new ConditionSystem());
            RegisterModule(new ActionSystem());
            RegisterModule(new SceneNpcSystem());
            RegisterModule(new SceneEventSystem());
            RegisterModule(new SequenceSystem());
            RegisterModule(new SaveSystem());
            
            RegisterModule(new CharacterModel());
            RegisterModule(new GameStateModel());
            RegisterModule(new GameVariableModel());
            RegisterModule(new GlobalVariableModel());
            RegisterModule(new SpawnPointModel());
            RegisterModule(new MovementRouteModel());
            
            RegisterModule<IInputService>(new InputService());
            RegisterModule<IPersistentSceneService>(new PersistentSceneService());
        }

        private void EarlyTick()
        {
#if UNITY_EDITOR
            _tableFileWatchSystem?.Tick();
#endif
            _frameTime = _unityTimeSource.ToFrameTime();
            _procedureDriver?.EarlyTick(in _frameTime);
            _schedulerDriver?.EarlyTick(in _frameTime);
            _cameraDriver?.EarlyTick(in _frameTime);
        }

        private void FrameTick()
        {
            _procedureDriver?.FrameTick(in _frameTime);
            _schedulerDriver?.FrameTick(in _frameTime);
            _cameraDriver?.FrameTick(in _frameTime);
        }

        private void LateTick()
        {
            _procedureDriver?.LateTick(in _frameTime);
            _schedulerDriver?.LateTick(in _frameTime);
            _cameraDriver?.LateTick(in _frameTime);
        }
        
#if UNITY_EDITOR
        private readonly HashSet<System.Type> _callStack = new HashSet<System.Type>();

        public override void ExecuteCommand<TCommand>(TCommand command)
        {
            try
            {
                Debug.Assert(_callStack.Add(command.GetType()), $"在同一次[Command]栈中重复执行了多次[{command.GetType()}]");
                base.ExecuteCommand(command);
            }
            finally
            {
                _callStack.Remove(command.GetType());
            }
        }

        public override TResult ExecuteCommand<TCommand, TResult>(TCommand command)
        {
            try
            {
                Debug.Assert(_callStack.Add(command.GetType()), $"在同一次[Command]栈中重复执行了多次[{command.GetType()}]");
                return base.ExecuteCommand<TCommand, TResult>(command);
            }
            finally
            {
                _callStack.Remove(command.GetType());
            }
        }

        public override TResult ExecuteQuery<TQuery, TResult>(TQuery query)
        {
            try
            {
                Debug.Assert(_callStack.Add(query.GetType()), $"在同一次[Query]栈中重复执行了多次[{query.GetType()}]");
                return base.ExecuteQuery<TQuery, TResult>(query);
            }
            finally
            {
                _callStack.Remove(query.GetType());
            }
        }

        public override void PublishEvent<TEvent>(in TEvent evt)
        {
            try
            {
                Debug.Assert(_callStack.Add(evt.GetType()), $"在同一次[Event]栈中重复执行了多次[{evt.GetType()}]");
                base.PublishEvent(in evt);
            }
            finally
            {
                _callStack.Remove(evt.GetType());
            }
        }
#endif
    }
}
