using MessagePack;

namespace Gameplay
{
    [MessagePackObject]
    public sealed class NpcSaveData
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public EmotionSaveData Emotion { get; set; }
        [Key(2)] public int Favorability { get; set; }
    }
}
