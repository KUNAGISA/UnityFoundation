using MessagePack;

namespace Gameplay
{
    [MessagePackObject]
    public sealed class EmotionSaveData
    {
        [Key(0)] public int[] Values { get; set; }
        [Key(1)] public int[] Thresholds { get; set; }
    }
}
