using Aoiro;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Gameplay
{
    internal sealed class InputPresentationTracker
    {
        private readonly Signal<InputPresentationChanged> _changed = new();

        private InputControls _controls;
        private Vector2 _pointerPosition;
        private bool _hasPointerPosition;

        internal InputPresentation CurrentPresentation { get; private set; } = CreateDefaultPresentation();
        internal ISignal<InputPresentationChanged> Changed => _changed;

        internal void Initialize(InputControls controls)
        {
            _controls = controls ?? throw new ArgumentNullException(nameof(controls));
            InputSystem.onActionChange += OnActionChange;
        }

        internal void Shutdown()
        {
            InputSystem.onActionChange -= OnActionChange;
            _changed.Clear();
            _controls = null;
            _hasPointerPosition = false;
            CurrentPresentation = CreateDefaultPresentation();
        }

        internal bool TryGetPointerPosition(out Vector2 position)
        {
            position = _pointerPosition;
            return _hasPointerPosition;
        }

        private void OnActionChange(object value, InputActionChange change)
        {
            if (change is not (InputActionChange.ActionStarted or InputActionChange.ActionPerformed) || value is not InputAction action || !ReferenceEquals(action.actionMap?.asset, _controls?.asset))
            {
                return;
            }

            var control = action.activeControl;
            if (control == null)
            {
                return;
            }

            var device = control.device;
            if (ReferenceEquals(action, _controls.UIGlobal.Point))
            {
                var position = action.ReadValue<Vector2>();
                var moved = !_hasPointerPosition || (position - _pointerPosition).sqrMagnitude >= 0.25f;
                _pointerPosition = position;
                _hasPointerPosition = true;
                if (!moved || device is Touchscreen)
                {
                    return;
                }
            }
            else if (!IsMeaningfulAction(action, control))
            {
                return;
            }

            if (device is Pointer pointer)
            {
                _pointerPosition = pointer.position.ReadValue();
                _hasPointerPosition = true;
            }

            var deviceKind = GetDeviceKind(device);
            var interactionSource = GetInteractionSource(device, deviceKind, action);
            var next = new InputPresentation(deviceKind, interactionSource, new InputDeviceInfo(device.deviceId, device.layout, device.displayName));
            if (next == CurrentPresentation)
            {
                return;
            }

            var previous = CurrentPresentation;
            CurrentPresentation = next;
            _changed.Emit(new InputPresentationChanged(previous, next));
        }

        private bool IsMeaningfulAction(InputAction action, InputControl control)
        {
            if (ReferenceEquals(action, _controls.UIGlobal.TrackedDevicePosition) || ReferenceEquals(action, _controls.UIGlobal.TrackedDeviceOrientation))
            {
                return false;
            }
            if (ReferenceEquals(action, _controls.Player.Move) || ReferenceEquals(action, _controls.UIModal.Navigate) || ReferenceEquals(action, _controls.UIModal.ScrollWheel))
            {
                return action.ReadValue<Vector2>().sqrMagnitude >= 0.0625f;
            }
            if (control is ButtonControl button)
            {
                return button.isPressed;
            }

            return action.IsPressed();
        }

        private UIInteractionSource GetInteractionSource(InputDevice device, InputDeviceKind deviceKind, InputAction action)
        {
            if (device is Mouse or Pen or Touchscreen)
            {
                return UIInteractionSource.Pointer;
            }
            if (deviceKind == InputDeviceKind.XR && action.actionMap == _controls.UIGlobal.Get())
            {
                return UIInteractionSource.Pointer;
            }
            return UIInteractionSource.Navigation;
        }

        private static InputDeviceKind GetDeviceKind(InputDevice device)
        {
            if (device is Touchscreen)
            {
                return InputDeviceKind.Touch;
            }
            if (device is Gamepad)
            {
                return InputDeviceKind.Gamepad;
            }
            if (device is Joystick)
            {
                return InputDeviceKind.Joystick;
            }
            if (device is Keyboard || device is Mouse || device is Pen)
            {
                return InputDeviceKind.KeyboardMouse;
            }
            return InputDeviceKind.XR;
        }

        private static InputPresentation CreateDefaultPresentation() => new InputPresentation(InputDeviceKind.KeyboardMouse, UIInteractionSource.Navigation, default);
    }
}
