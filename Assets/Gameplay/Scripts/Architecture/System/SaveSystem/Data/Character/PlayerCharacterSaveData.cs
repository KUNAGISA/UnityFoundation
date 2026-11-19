using MessagePack;

namespace Gameplay
{
    [MessagePackObject]
    public sealed class PlayerCharacterSaveData
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public StatsSaveData Stats { get; set; }
        [Key(2)] public EmotionSaveData Emotion { get; set; }
        [Key(3)] public SpatialSaveData Spatial { get; set; }
    }
}
