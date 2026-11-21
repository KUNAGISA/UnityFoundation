using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gameplay.Scheduler;
using Unity.Cinemachine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay
{
    internal sealed class CameraDirector
    {
        public CameraSelection Selected => _outputRegistry?.Selected ?? CameraSelection.None;
        public bool IsBlending => _context?.Brain != null && _context.Brain.IsBlending;

        private readonly FrameTickScheduler _afterCameraUpdateScheduler = new FrameTickScheduler();
        private readonly List<ICameraModule> _modules = new List<ICameraModule>();
        private readonly Dictionary<Type, ICameraModule> _moduleByType = new Dictionary<Type, ICameraModule>();
        private readonly Dictionary<CameraProviderId, ICameraProviderModule> _providerById = new Dictionary<CameraProviderId, ICameraProviderModule>();

        private CameraContext _context;
        private CameraSystemSettings _settings;
        private CameraOutputRegistry _outputRegistry;
        private CinemachineCore.GetBlendOverrideDelegate _blendOverride;
        private CinemachineCore.GetBlendOverrideDelegate _previousBlendOverride;
        private bool _initializing;
        private bool _initialized;
        private bool _shuttingDown;

        public void RegisterCameraModule<T>(T module) where T : class, ICameraModule
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }
            if (_initializing || _initialized || _shuttingDown)
            {
                throw new InvalidOperationException("Camera modules can only be registered before CameraDirector initialization.");
            }

            var type = module.GetType();
            if (_moduleByType.ContainsKey(type))
            {
                throw new InvalidOperationException($"Camera module [{type.FullName}] is already registered.");
            }
            if (module is ICameraProviderModule provider && (provider.ProviderId == CameraProviderId.None || !_providerById.TryAdd(provider.ProviderId, provider)))
            {
                throw new InvalidOperationException($"Camera provider id [{provider.ProviderId}] is invalid or already registered.");
            }
            _modules.Add(module);
            _moduleByType.Add(type, module);
        }

        public void Initialize(CameraContext context, CameraSystemSettings settings)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("CameraDirector is already initialized.");
            }

            _context = context;
            _settings = settings;
            _initializing = true;

            var initializedModuleCount = 0;
            try
            {
                ValidateProviderSettings();
                _outputRegistry = new CameraOutputRegistry(_settings, _context.Brain, _providerById);
                _context.Director = this;
                _context.OutputRegistry = _outputRegistry;
                InstallBlendOverride();

                for (; initializedModuleCount < _modules.Count; ++initializedModuleCount)
                {
                    _modules[initializedModuleCount].Initialize(_context);
                }
                _initialized = true;
            }
            catch
            {
                ShutdownModules(initializedModuleCount);
                CleanupCore();
                throw;
            }
            finally
            {
                _initializing = false;
            }
        }

        public void Shutdown()
        {
            if (!_initialized && _context == null)
            {
                return;
            }

            _shuttingDown = true;
            _afterCameraUpdateScheduler.Cleanup();
            ShutdownModules(_modules.Count);
            CleanupCore();
            _initialized = false;
            _shuttingDown = false;
        }

        public async UniTask ReadyAsync(CancellationToken cancellationToken)
        {
            var tasks = new UniTask[_modules.Count];
            for (var index = 0; index < _modules.Count; ++index)
            {
                tasks[index] = _modules[index].ReadyAsync(cancellationToken);
            }
            await UniTask.WhenAll(tasks);
        }

        public T GetModule<T>() where T : class, ICameraModule
        {
            if (_moduleByType.TryGetValue(typeof(T), out var module))
            {
                return module as T;
            }
            throw new InvalidOperationException($"Camera module [{typeof(T).FullName}] is not registered.");
        }

        public bool TryGetModule<T>(out T module) where T : class, ICameraModule
        {
            if (_moduleByType.TryGetValue(typeof(T), out var value))
            {
                module = value as T;
                return module != null;
            }
            module = null;
            return false;
        }

        public CameraDebugSnapshot GetDebugSnapshot()
        {
            var selected = Selected;
            var activeCount = 0;
            var outputs = _outputRegistry != null ? _outputRegistry.CreateDebugInfo(out activeCount) : Array.Empty<CameraOutputDebugInfo>();
            return new CameraDebugSnapshot(in selected, IsBlending, _context?.Brain?.ActiveVirtualCamera?.Name, activeCount, outputs);
        }

        public FrameTickToken AddAfterCameraUpdate(FrameTickAction action) => _afterCameraUpdateScheduler.Add(action);

        public CameraPriorityRange GetPriorityRange(CameraProviderId providerId)
        {
            if (_settings != null && _settings.TryGetPriorityRange(providerId, out var range))
            {
                return range;
            }
            throw new InvalidOperationException($"No priority range is configured for camera provider [{providerId}].");
        }

        public void FrameTick(in FrameTime time) => _context.OnFrameTick(in time);

        public void LateTick(in FrameTime time)
        {
            ResolveSelection();
            _context.OnLateTick(in time);
            ResolveSelection();
            _context.Brain.ManualUpdate(Time.frameCount, time.DeltaTime);
            _afterCameraUpdateScheduler.Schedule(in time);
        }

        private void ValidateProviderSettings()
        {
            var ranges = _settings.ProviderRanges;
            for (var index = 0; index < ranges.Length; ++index)
            {
                if (ranges[index].ProviderId == CameraProviderId.None || !ranges[index].Range.IsValid)
                {
                    throw new InvalidOperationException($"Camera provider range at index [{index}] is invalid.");
                }
                for (var otherIndex = index + 1; otherIndex < ranges.Length; ++otherIndex)
                {
                    var otherRange = ranges[otherIndex].Range;
                    if (ranges[index].ProviderId == ranges[otherIndex].ProviderId || ranges[index].Range.Overlaps(in otherRange))
                    {
                        throw new InvalidOperationException($"Camera provider ranges [{ranges[index].ProviderId}] and [{ranges[otherIndex].ProviderId}] conflict.");
                    }
                }
            }

            var blends = _settings.ProviderBlends;
            for (var index = 0; index < blends.Length; ++index)
            {
                if (blends[index].From == CameraProviderId.None || blends[index].To == CameraProviderId.None)
                {
                    throw new InvalidOperationException($"Camera provider blend at index [{index}] uses None. None transitions are always Cut.");
                }
                for (var otherIndex = index + 1; otherIndex < blends.Length; ++otherIndex)
                {
                    if (blends[index].From == blends[otherIndex].From && blends[index].To == blends[otherIndex].To)
                    {
                        throw new InvalidOperationException($"Camera provider blend [{blends[index].From} -> {blends[index].To}] is configured more than once.");
                    }
                }
            }

            foreach (var pair in _providerById)
            {
                if (!_settings.TryGetPriorityRange(pair.Key, out _))
                {
                    throw new InvalidOperationException($"Registered camera provider [{pair.Key}] has no priority range.");
                }
            }
        }

        private void InstallBlendOverride()
        {
            _previousBlendOverride = CinemachineCore.GetBlendOverride;
            _blendOverride = ResolveBlendOverride;
            CinemachineCore.GetBlendOverride = _blendOverride;
        }

        private CinemachineBlendDefinition ResolveBlendOverride(ICinemachineCamera fromCamera, ICinemachineCamera toCamera, CinemachineBlendDefinition defaultBlend, Object owner)
        {
            if (owner != _context.Brain)
            {
                return _previousBlendOverride != null ? _previousBlendOverride(fromCamera, toCamera, defaultBlend, owner) : defaultBlend;
            }
            if (_outputRegistry == null || !_outputRegistry.TryGetState(fromCamera, out var from) || !_outputRegistry.TryGetState(toCamera, out var to))
            {
                return new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }
            if (from.ProviderId == to.ProviderId && _providerById.TryGetValue(from.ProviderId, out var provider) && provider is ICameraProviderBlendResolver blendResolver && blendResolver.TryResolveBlend(from.SessionId, to.SessionId, out var providerBlend))
            {
                return providerBlend;
            }
            return _settings.ResolveBlend(from.ProviderId, to.ProviderId);
        }

        private void ResolveSelection()
        {
            if (_outputRegistry == null || !_outputRegistry.ResolveSelection())
            {
                return;
            }

            var selected = _outputRegistry.Selected;
            for (var index = 0; index < _modules.Count; ++index)
            {
                if (_modules[index] is ICameraSelectionListener listener)
                {
                    listener.OnCameraSelectionChanged(in selected);
                }
            }
        }

        private void ShutdownModules(int count)
        {
            for (var index = count - 1; index >= 0; --index)
            {
                try
                {
                    _modules[index].Shutdown();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private void CleanupCore()
        {
            if (_blendOverride != null && CinemachineCore.GetBlendOverride == _blendOverride)
            {
                CinemachineCore.GetBlendOverride = _previousBlendOverride;
            }
            _blendOverride = null;
            _previousBlendOverride = null;
            _outputRegistry?.Clear();
            _outputRegistry = null;
            if (_context != null)
            {
                _context.Director = null;
                _context.OutputRegistry = null;
            }
            _context = null;
            _settings = null;
        }
    }
}
