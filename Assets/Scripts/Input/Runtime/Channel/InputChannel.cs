using System;
using UnityEngine.InputSystem;

namespace Gameplay
{
    public abstract class InputChannel
    {
        private InputActionMap _actionMap = null;

        internal InputChannelMask Mask { get; }
        internal bool DefaultEnabled { get; }
        protected bool IsEnabled { get; private set; }

        private protected InputChannel(InputChannelMask mask, bool defaultEnabled)
        {
            var value = (byte)mask;
            if (value == 0 || (value & (value - 1)) != 0)
            {
                throw new ArgumentException("Input channel mask must contain exactly one channel.", nameof(mask));
            }

            Mask = mask;
            DefaultEnabled = defaultEnabled;
        }

        internal void Initialize(InputControls controls)
        {
            if (controls == null)
            {
                throw new ArgumentNullException(nameof(controls));
            }

            _actionMap = ResolveActionMap(controls);
            OnInitialize(controls);
        }

        internal void SetEnabled(bool enabled)
        {
            if (_actionMap == null || IsEnabled == enabled)
            {
                return;
            }

            if (enabled)
            {
                _actionMap.Enable();
                IsEnabled = true;
                OnEnabledChanged(true);
            }
            else
            {
                IsEnabled = false;
                OnEnabledChanged(false);
                _actionMap.Disable();
            }
        }

        internal void Shutdown()
        {
            SetEnabled(false);
            OnShutdown();
            _actionMap = null;
        }

        protected abstract InputActionMap ResolveActionMap(InputControls controls);
        protected virtual void OnInitialize(InputControls controls) { }
        protected virtual void OnEnabledChanged(bool enabled) { }
        protected virtual void OnShutdown() { }
    }
}
