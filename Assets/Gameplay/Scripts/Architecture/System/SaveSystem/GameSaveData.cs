using System.Collections.Generic;
using MessagePack;

namespace Gameplay
{
    [MessagePackObject]
    public sealed class GameSaveData
    {
        [Key(0)] public int SceneId { get; set; }
        [Key(1)] public long SaveTimeUtcTicks { get; set; }
        [Key(2)] public PlayerCharacterSaveData PlayerCharacter { get; set; }
        [Key(3)] public List<NpcSaveData> Npcs { get; set; }
        [Key(4)] public List<int> CompletedSceneEvents { get; set; }
    }
}
