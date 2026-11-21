using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Gameplay
{
    internal sealed class UIGlobalInputChannel : InputChannel
    {
        private InputSystemUIInputModule _uiInputModule;
        private InputActionReference _point;
        private InputActionReference _leftClick;
        private InputActionReference _rightClick;
        private InputActionReference _middleClick;
        private InputActionReference _trackedDevicePosition;
        private InputActionReference _trackedDeviceOrientation;

        internal UIGlobalInputChannel() : base(InputChannelMask.UIGlobal, true)
        {
        }

        internal void BindUIInputModule(InputSystemUIInputModule module)
        {
            if (_uiInputModule != null)
            {
                throw new InvalidOperationException("A UI input module is already bound to the UI global input channel.");
            }

            _uiInputModule = module ?? throw new ArgumentNullException(nameof(module));
            ApplyUIInputModuleBindings(IsEnabled);
        }

        internal void UnbindUIInputModule(InputSystemUIInputModule module)
        {
            if (!ReferenceEquals(_uiInputModule, module))
            {
                return;
            }

            ApplyUIInputModuleBindings(false);
            _uiInputModule = null;
        }

        protected override InputActionMap ResolveActionMap(InputControls controls)
        {
            return controls.UIGlobal;
        }

        protected override void OnInitialize(InputControls controls)
        {
            _point = InputActionReference.Create(controls.UIGlobal.Point);
            _leftClick = InputActionReference.Create(controls.UIGlobal.Click);
            _rightClick = InputActionReference.Create(controls.UIGlobal.RightClick);
            _middleClick = InputActionReference.Create(controls.UIGlobal.MiddleClick);
            _trackedDevicePosition = InputActionReference.Create(controls.UIGlobal.TrackedDevicePosition);
            _trackedDeviceOrientation = InputActionReference.Create(controls.UIGlobal.TrackedDeviceOrientation);
        }

        protected override void OnEnabledChanged(bool enabled) => ApplyUIInputModuleBindings(enabled);

        protected override void OnShutdown()
        {
            ApplyUIInputModuleBindings(false);
            _uiInputModule = null;
            DestroyReference(ref _point);
            DestroyReference(ref _leftClick);
            DestroyReference(ref _rightClick);
            DestroyReference(ref _middleClick);
            DestroyReference(ref _trackedDevicePosition);
            DestroyReference(ref _trackedDeviceOrientation);
        }

        private void ApplyUIInputModuleBindings(bool enabled)
        {
            if (_uiInputModule == null)
            {
                return;
            }

            _uiInputModule.point = enabled ? _point : null;
            _uiInputModule.leftClick = enabled ? _leftClick : null;
            _uiInputModule.rightClick = enabled ? _rightClick : null;
            _uiInputModule.middleClick = enabled ? _middleClick : null;
            _uiInputModule.trackedDevicePosition = enabled ? _trackedDevicePosition : null;
            _uiInputModule.trackedDeviceOrientation = enabled ? _trackedDeviceOrientation : null;
        }

        private static void DestroyReference(ref InputActionReference reference)
        {
            if (reference != null)
            {
                UnityEngine.Object.Destroy(reference);
                reference = null;
            }
        }
    }
}
