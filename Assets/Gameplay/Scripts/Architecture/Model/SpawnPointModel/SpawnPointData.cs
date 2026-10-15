using UnityEngine;

namespace Gameplay
{
    public readonly struct SpawnPointData
    {
        public int SpawnPointId { get; }
        public int RouteId { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public SpawnPointData(int spawnPointId, int routeId, Vector3 position, Quaternion rotation)
        {
            SpawnPointId = spawnPointId;
            RouteId = routeId;
            Position = position;
            Rotation = rotation;
        }
    }
}
