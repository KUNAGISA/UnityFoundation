using System;
using System.Collections.Generic;
using Unity.Cinemachine;

namespace Gameplay
{
    /// <summary>
    /// CameraOutputRegistration 的内部所有者，集中维护输出 Active、Session 和 Live 状态。
    /// </summary>
    internal interface ICameraOutputRegistrationOwner
    {
        void SetOutputActive(long registrationId, bool active);
        void UpdateOutputSession(long registrationId, long sessionId);
        bool IsOutputLive(long registrationId);
        void UnregisterOutput(long registrationId);
    }

    internal sealed class CameraOutputRegistry : ICameraOutputRegistrationOwner
    {
        public CameraSelection Selected { get; private set; }

        private readonly CameraSystemSettings _settings;
        private readonly CinemachineBrain _brain;
        private readonly Dictionary<CameraProviderId, ICameraProviderModule> _providers;
        private readonly List<CameraOutputState> _outputs = new List<CameraOutputState>();
        private readonly Dictionary<ICinemachineCamera, CameraOutputState> _outputByCamera = new Dictionary<ICinemachineCamera, CameraOutputState>();

        private long _lastRegistrationId;

        public CameraOutputRegistry(CameraSystemSettings settings, CinemachineBrain brain, Dictionary<CameraProviderId, ICameraProviderModule> providers)
        {
            _settings = settings;
            _brain = brain;
            _providers = providers;
        }

        public CameraOutputRegistration Register(CameraProviderId providerId, long sessionId, int priority, CinemachineVirtualCameraBase outputCamera)
        {
            if (outputCamera == null)
            {
                throw new ArgumentNullException(nameof(outputCamera));
            }
            if (!_providers.ContainsKey(providerId))
            {
                throw new InvalidOperationException($"Camera provider [{providerId}] is not registered.");
            }
            if (!_settings.TryGetPriorityRange(providerId, out var range) || !range.Contains(priority))
            {
                throw new InvalidOperationException($"Priority [{priority}] is outside provider [{providerId}] range [{range.Minimum}, {range.Maximum}].");
            }
            if (_outputByCamera.ContainsKey(outputCamera))
            {
                throw new InvalidOperationException($"Cinemachine camera [{outputCamera.Name}] is already registered.");
            }

            outputCamera.Priority = priority;
            outputCamera.OutputChannel = _settings.OutputChannel;
            var state = new CameraOutputState
            {
                RegistrationId = ++_lastRegistrationId,
                ProviderId = providerId,
                SessionId = sessionId,
                Priority = priority,
                Camera = outputCamera,
            };
            _outputs.Add(state);
            _outputByCamera.Add(outputCamera, state);
            return new CameraOutputRegistration(this, state.RegistrationId);
        }

        public bool ResolveSelection()
        {
            CameraOutputState selectedState = null;
            for (var index = 0; index < _outputs.Count; ++index)
            {
                var candidate = _outputs[index];
                if (!candidate.Active || candidate.Camera == null || !candidate.Camera.isActiveAndEnabled)
                {
                    continue;
                }
                if (selectedState == null || candidate.Priority > selectedState.Priority)
                {
                    selectedState = candidate;
                }
            }

            var next = selectedState == null ? CameraSelection.None : new CameraSelection(selectedState.ProviderId, selectedState.SessionId, selectedState.Priority);
            if (Selected.ProviderId == next.ProviderId && Selected.SessionId == next.SessionId && Selected.Priority == next.Priority)
            {
                return false;
            }
            Selected = next;
            return true;
        }

        public bool TryGetState(ICinemachineCamera camera, out CameraOutputState state) => _outputByCamera.TryGetValue(camera, out state);

        public bool IsPriorityCovered(int priority)
        {
            for (var index = 0; index < _outputs.Count; ++index)
            {
                if (_outputs[index].Active && _outputs[index].Priority > priority)
                {
                    return true;
                }
            }
            return false;
        }

        public CameraOutputDebugInfo[] CreateDebugInfo(out int activeCount)
        {
            activeCount = 0;
            var result = new CameraOutputDebugInfo[_outputs.Count];
            for (var index = 0; index < _outputs.Count; ++index)
            {
                var output = _outputs[index];
                if (output.Active)
                {
                    ++activeCount;
                }
                var selected = Selected.ProviderId == output.ProviderId && Selected.SessionId == output.SessionId;
                result[index] = new CameraOutputDebugInfo(output.ProviderId, output.SessionId, output.Priority, output.Camera?.Name, output.Active, selected, IsLive(output));
            }
            return result;
        }

        public void Clear()
        {
            _outputs.Clear();
            _outputByCamera.Clear();
            Selected = default;
        }

        void ICameraOutputRegistrationOwner.SetOutputActive(long registrationId, bool active)
        {
            var state = Find(registrationId);
            if (state == null)
            {
                return;
            }
            if (active)
            {
                for (var index = 0; index < _outputs.Count; ++index)
                {
                    var other = _outputs[index];
                    if (other != state && other.Active && other.Priority == state.Priority)
                    {
                        throw new InvalidOperationException($"Camera outputs [{state.Camera?.Name}] and [{other.Camera?.Name}] have the same active priority [{state.Priority}].");
                    }
                }
            }
            state.Active = active;
        }

        void ICameraOutputRegistrationOwner.UpdateOutputSession(long registrationId, long sessionId)
        {
            var state = Find(registrationId);
            if (state == null)
            {
                throw new InvalidOperationException($"Camera output registration [{registrationId}] does not exist.");
            }
            if (state.Active)
            {
                throw new InvalidOperationException("Cannot change an active camera output session.");
            }
            state.SessionId = sessionId;
        }

        bool ICameraOutputRegistrationOwner.IsOutputLive(long registrationId)
        {
            var state = Find(registrationId);
            return state != null && IsLive(state);
        }

        void ICameraOutputRegistrationOwner.UnregisterOutput(long registrationId)
        {
            for (var index = 0; index < _outputs.Count; ++index)
            {
                var state = _outputs[index];
                if (state.RegistrationId != registrationId)
                {
                    continue;
                }
                if (IsLive(state))
                {
                    throw new InvalidOperationException($"Cannot unregister live camera output [{state.Camera?.Name}].");
                }
                _outputs.RemoveAt(index);
                if (state.Camera != null)
                {
                    _outputByCamera.Remove(state.Camera);
                }
                return;
            }
        }

        private CameraOutputState Find(long registrationId)
        {
            for (var index = 0; index < _outputs.Count; ++index)
            {
                if (_outputs[index].RegistrationId == registrationId)
                {
                    return _outputs[index];
                }
            }
            return null;
        }

        private bool IsLive(CameraOutputState state)
        {
            if (state.Camera == null)
            {
                return false;
            }
            return _brain.IsLiveChild(state.Camera) || (_brain.ActiveBlend != null && _brain.ActiveBlend.Uses(state.Camera));
        }
    }
}
