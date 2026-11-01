using System;

namespace Gameplay
{
    [Flags]
    public enum ConditionSource
    {
        None = 0,
        GameIntVariables = 1 << 0,
        GameBoolVariables = 1 << 1,
        GlobalIntVariables = 1 << 2,
        GlobalBoolVariables = 1 << 3,
    }
}
