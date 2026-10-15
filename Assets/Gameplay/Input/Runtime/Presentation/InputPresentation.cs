using System;

namespace Gameplay
{
    public enum InputDeviceKind
    {
        KeyboardMouse,
        Gamepad,
        Touch,
        Joystick,
        XR
    }

    public enum UIInteractionSource
    {
        Pointer,
        Navigation
    }

    public readonly struct InputPresentation : IEquatable<InputPresentation>
    {
        public InputDeviceKind DeviceKind { get; }
        public UIInteractionSource InteractionSource { get; }
        public InputDeviceInfo Device { get; }

        public InputPresentation(InputDeviceKind deviceKind, UIInteractionSource interactionSource, InputDeviceInfo device)
        {
            DeviceKind = deviceKind;
            InteractionSource = interactionSource;
            Device = device;
        }

        public bool Equals(InputPresentation other) => DeviceKind == other.DeviceKind && InteractionSource == other.InteractionSource && Device == other.Device;
        public override bool Equals(object obj) => obj is InputPresentation other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int)DeviceKind, (int)InteractionSource, Device);
        public static bool operator ==(InputPresentation left, InputPresentation right) => left.Equals(right);
        public static bool operator !=(InputPresentation left, InputPresentation right) => !left.Equals(right);
    }

    public readonly struct InputPresentationChanged
    {
        public InputPresentation Previous { get; }
        public InputPresentation Current { get; }

        public InputPresentationChanged(InputPresentation previous, InputPresentation current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
