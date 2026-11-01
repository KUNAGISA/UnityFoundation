using System;

namespace Gameplay
{
    [Flags]
    public enum ConditionSource
    {
        None = 0,
        GameVariables = 1 << 0,
        GameSwitches = 1 << 1,
        GlobalVariables = 1 << 2,
        GlobalSwitches = 1 << 3,
    }
}
