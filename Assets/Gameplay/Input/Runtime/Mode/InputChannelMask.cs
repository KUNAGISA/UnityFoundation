using System;

namespace Gameplay
{
    [Flags]
    public enum InputChannelMask : byte
    {
        None = 0,
        Player = 1 << 0,
        UIGlobal = 1 << 1,
        UIModal = 1 << 2,
        HotKey = 1 << 3,
        All = byte.MaxValue,
    }
}
