using UnityEngine;

namespace Gameplay
{
    public readonly struct SpawnPointData
    {
        public int SpawnPointId { get; }
        public int RouteId { get; }
        public Vector3 Position { get; }
        public FacingDirection Facing { get; }

        public SpawnPointData(int spawnPointId, int routeId, Vector3 position, FacingDirection facing)
        {
            SpawnPointId = spawnPointId;
            RouteId = routeId;
            Position = position;
            Facing = facing;
        }
    }
}
