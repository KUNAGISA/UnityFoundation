using UnityEngine;

namespace Gameplay
{
    public readonly struct PathSegment
    {
        public readonly int SegmentIndex;
        public readonly PathSegmentType Type;
        public readonly Vector2 P0;
        public readonly Vector2 P1;
        public readonly Vector2 P2;
        public readonly Vector2 P3;
        public readonly float StartDistance;
        public readonly float Length;

        public PathSegment(int segmentIndex, PathSegmentType type, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float startDistance, float length)
        {
            SegmentIndex = segmentIndex;
            Type = type;
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
            StartDistance = startDistance;
            Length = length;
        }
    }
}
