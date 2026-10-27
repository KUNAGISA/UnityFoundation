using System.Collections.Generic;
using MessagePack;

namespace Gameplay
{
    [MessagePackObject]
    public sealed class VariableSaveData
    {
        [Key(0)] public Dictionary<int, int> IntValues { get; set; } = new();
        [Key(1)] public Dictionary<int, bool> BoolValues { get; set; } = new();
    }
}
