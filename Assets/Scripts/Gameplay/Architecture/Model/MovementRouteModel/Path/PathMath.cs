using UnityEngine;

namespace Gameplay
{
    public static class PathMath
    {
        public static Vector2 EvaluateLine(Vector2 p0, Vector2 p1, float t)
        {
            return Vector2.LerpUnclamped(p0, p1, t);
        }

        public static Vector2 EvaluateLineTangent(Vector2 p0, Vector2 p1)
        {
            var tangent = p1 - p0;
            return tangent.sqrMagnitude > Mathf.Epsilon ? tangent.normalized : Vector2.right;
        }

        public static Vector2 EvaluateBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            var u = 1f - t;
            return u * u * u * p0
                + 3f * u * u * t * p1
                + 3f * u * t * t * p2
                + t * t * t * p3;
        }

        public static Vector2 EvaluateBezierDerivative(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            var u = 1f - t;
            return 3f * u * u * (p1 - p0)
                + 6f * u * t * (p2 - p1)
                + 3f * t * t * (p3 - p2);
        }

        public static Vector2 Evaluate(in PathSegment segment, float t)
        {
            return segment.Type == PathSegmentType.Line
                ? EvaluateLine(segment.P0, segment.P1, t)
                : EvaluateBezier(segment.P0, segment.P1, segment.P2, segment.P3, t);
        }

        public static Vector2 EvaluateTangent(in PathSegment segment, float t)
        {
            var tangent = segment.Type == PathSegmentType.Line
                ? segment.P1 - segment.P0
                : EvaluateBezierDerivative(segment.P0, segment.P1, segment.P2, segment.P3, t);

            return tangent.sqrMagnitude > Mathf.Epsilon ? tangent.normalized : Vector2.right;
        }
    }
}
