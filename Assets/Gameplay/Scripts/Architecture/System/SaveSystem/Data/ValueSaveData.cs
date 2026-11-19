using System.Collections.Generic;
using MessagePack;

namespace Gameplay
{
    [MessagePackObject]
    public sealed class ValueSaveData
    {
        [Key(0)] public Dictionary<int, int> Variables { get; set; } = new();
        [Key(1)] public Dictionary<int, bool> Switches { get; set; } = new();
    }
}
