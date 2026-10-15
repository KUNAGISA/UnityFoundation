namespace Gameplay
{
    public readonly struct InputButtonState
    {
        private const byte Pressed = 1 << 0;
        private const byte Released = 1 << 1;
        private const byte Held = 1 << 2;

        private readonly byte _value;

        public bool IsPressed => (_value & Pressed) != 0;
        public bool IsReleased => (_value & Released) != 0;
        public bool IsHeld => (_value & Held) != 0;

        private InputButtonState(byte value)
        {
            _value = value;
        }

        internal InputButtonState WithPressed() => new((byte)(_value | Pressed | Held));

        internal InputButtonState WithReleased() => new((byte)((_value | Released) & ~Held));

        internal InputButtonState WithoutTransient() => new((byte)(_value & Held));
    }
}
