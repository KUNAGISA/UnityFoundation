using UnityEngine;

namespace Gameplay
{
    public readonly struct PathSample
    {
        public readonly int RouteId;
        public readonly int SegmentIndex;
        public readonly float T;
        public readonly float Distance;
        public readonly Vector2 Position;

        public PathSample(int routeId, int segmentIndex, float t, float distance, Vector2 position)
        {
            RouteId = routeId;
            SegmentIndex = segmentIndex;
            T = t;
            Distance = distance;
            Position = position;
        }
    }
}
