using UnityEngine;

namespace Gameplay
{
    public sealed class SpatialState
    {
        public int? RouteId { get; private set; }
        public Vector2 Position { get; private set; }
        public FacingDirection Facing { get; private set; } = FacingDirection.Left;

        internal void SetFacing(FacingDirection facing) => Facing = facing;

        internal void SetPosition(int? routeId, Vector2 position)
        {
            RouteId = routeId;
            Position = position;
        }
    }
}
