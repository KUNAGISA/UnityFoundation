using System;

namespace Gameplay.Sequence
{
    [Flags]
    public enum AdvDialogueChangeFlags
    {
        None = 0,
        Text = 1 << 0,
        CharacterName = 1 << 1,
        AdvancePrompt = 1 << 2,
        All = Text | CharacterName | AdvancePrompt
    }
}
