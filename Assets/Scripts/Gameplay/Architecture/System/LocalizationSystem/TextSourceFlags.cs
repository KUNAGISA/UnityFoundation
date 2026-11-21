using System;

namespace Gameplay
{
    [Flags]
    public enum TextSourceFlags
    {
        Global      = 1 << 0,
        Sequence    = 1 << 1,
        
        All         = ~0
    }
}
