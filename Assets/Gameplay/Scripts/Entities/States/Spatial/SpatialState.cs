using UnityEngine;

namespace Gameplay
{
    public sealed class SpatialState
    {
        public int? RouteId { get; private set; }
        public Vector2 Position { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        internal void SetPose(int? routeId, Vector2 position, Quaternion rotation)
        {
            RouteId = routeId;
            Position = position;
            Rotation = rotation;
        }
    }
}
