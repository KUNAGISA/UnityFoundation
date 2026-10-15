using MessagePack;
using UnityEngine;

namespace Gameplay
{
    [MessagePackObject]
    public sealed class SpatialSaveData
    {
        [Key(0)] public int? RouteId { get; set; }
        [Key(1)] public Vector2 Position { get; set; }
        [Key(2)] public Quaternion Rotation { get; set; }
    }
}
