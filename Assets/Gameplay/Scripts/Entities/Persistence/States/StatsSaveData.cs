using MessagePack;

namespace Gameplay
{
    [MessagePackObject]
    public sealed class StatsSaveData
    {
        [Key(0)] public int[] Attributes { get; set; }
        [Key(1)] public RangedValue[] Vitals { get; set; }
    }
}
