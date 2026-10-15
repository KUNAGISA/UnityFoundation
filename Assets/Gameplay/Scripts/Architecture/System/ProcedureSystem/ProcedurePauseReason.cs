using System;

namespace Gameplay
{
    [Flags]
    public enum ProcedurePauseReason : byte
    {
        None = 0,
        SceneTransition = 1 << 0,
        LanguageSwitch = 1 << 1,
    }
}
