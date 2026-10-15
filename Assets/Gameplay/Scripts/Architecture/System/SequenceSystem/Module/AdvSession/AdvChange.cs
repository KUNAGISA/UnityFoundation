using System;

namespace Gameplay.Sequence
{
    [Flags]
    public enum AdvChange
    {
        None = 0,
        Dialogue = 1 << 0,
        CharacterName = 1 << 1,
        AdvancePrompt = 1 << 2,
        Choices = 1 << 3,
        All = Dialogue | CharacterName | AdvancePrompt | Choices
    }
}
