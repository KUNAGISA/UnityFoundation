using System.Collections.Generic;
using Aoiro;
using UnityEngine;

namespace Gameplay
{
    public sealed class MovementRouteModel : ModelBase
    {
        private readonly struct PathEvaluateResult
        {
            public readonly float Distance;
            public readonly Vector2 Position;
            public readonly Vector2 Tangent;

            public PathEvaluateResult(float distance, Vector2 position, Vector2 tangent)
            {
                Distance = distance;
                Position = position;
                Tangent = tangent;
            }
        }

        private readonly Dictionary<int, MovementRoute> _routes = new();

        public bool HasRoutes => _routes.Count > 0;

        protected override void OnInitialize()
        {
            Clear();
        }

        protected override void OnShutdown()
        {
            Clear();
        }

        public bool Contains(int routeId)
        {
            return _routes.ContainsKey(routeId);
        }

        public bool TryProject(int routeId, Vector3 position, out RouteLocation location)
        {
            return TryProject(routeId, ToVector2(position), out location);
        }

        public bool TryProject(int routeId, Vector2 position, out RouteLocation location)
        {
            location = default;
            if (!_routes.TryGetValue(routeId, out var route) || route.Samples.Count < 2)
            {
                return false;
            }

            var bestDistanceSq = float.MaxValue;
            var bestDistanceOnRoute = 0f;

            for (var i = 0; i < route.Samples.Count - 1; i++)
            {
                var a = route.Samples[i];
                var b = route.Samples[i + 1];
                var ab = b.Position - a.Position;
                var abLengthSq = ab.sqrMagnitude;
                if (abLengthSq <= Mathf.Epsilon)
                {
                    continue;
                }

                var t = Vector2.Dot(position - a.Position, ab) / abLengthSq;
                t = Mathf.Clamp01(t);

                var projected = Vector2.Lerp(a.Position, b.Position, t);
                var sqrDistance = (position - projected).sqrMagnitude;
                if (sqrDistance >= bestDistanceSq)
                {
                    continue;
                }

                bestDistanceSq = sqrDistance;
                bestDistanceOnRoute = Mathf.Lerp(a.Distance, b.Distance, t);
            }

            return bestDistanceSq < float.MaxValue && TryEvaluateAtDistance(routeId, bestDistanceOnRoute, out location);
        }

        public bool TryProjectToClosestRoute(Vector3 position, out RouteLocation location)
        {
            return TryProjectToClosestRoute(ToVector2(position), out location);
        }

        public bool TryProjectToClosestRoute(Vector2 position, out RouteLocation location)
        {
            location = default;
            var bestDistanceSq = float.MaxValue;
            var bestLocation = default(RouteLocation);

            foreach (var route in _routes.Values)
            {
                if (!TryProject(route.RouteId, position, out var routeLocation))
                {
                    continue;
                }

                var sqrDistance = (position - routeLocation.Position).sqrMagnitude;
                if (sqrDistance >= bestDistanceSq)
                {
                    continue;
                }

                bestDistanceSq = sqrDistance;
                bestLocation = routeLocation;
            }

            if (bestDistanceSq >= float.MaxValue)
            {
                return false;
            }

            location = bestLocation;
            return true;
        }

        public bool TryEvaluateAtDistance(int routeId, float distance, out RouteLocation location)
        {
            location = default;
            if (!TryEvaluateInternal(routeId, distance, out var result))
            {
                return false;
            }

            location = new RouteLocation
            {
                RouteId = routeId,
                Distance = result.Distance,
                Position = result.Position
            };
            return true;
        }

        public bool TryMove(RouteLocation current, Vector2 direction, float moveDistance, out RouteLocation next)
        {
            if (direction == Vector2.zero || moveDistance <= 0f)
            {
                next = current;
                return true;
            }

            if (!TryEvaluateInternal(current.RouteId, current.Distance, out var evaluated))
            {
                next = current;
                return false;
            }

            if (evaluated.Tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                next = current;
                return true;
            }

            var dot = Vector2.Dot(direction.normalized, evaluated.Tangent.normalized);
            if (Mathf.Abs(dot) <= 0.001f)
            {
                next = current;
                return true;
            }

            var nextDistance = current.Distance + Mathf.Sign(dot) * moveDistance;
            return TryEvaluateAtDistance(current.RouteId, nextDistance, out next);
        }

        internal bool TryAdd(MovementRoute route)
        {
            return route != null && _routes.TryAdd(route.RouteId, route);
        }

        internal void Clear()
        {
            _routes.Clear();
        }

        private bool TryEvaluateInternal(int routeId, float distance, out PathEvaluateResult result)
        {
            result = default;
            if (!_routes.TryGetValue(routeId, out var route) || route.Samples.Count == 0 || route.Segments.Count == 0)
            {
                return false;
            }

            distance = Mathf.Clamp(distance, 0f, route.TotalLength);
            var sampleIndex = BinarySearchSample(route.Samples, distance);
            var a = route.Samples[sampleIndex];
            var b = route.Samples[Mathf.Min(sampleIndex + 1, route.Samples.Count - 1)];
            var range = b.Distance - a.Distance;
            var lerp = range > 0f ? (distance - a.Distance) / range : 0f;

            var segment = FindSegment(route, a.SegmentIndex);
            var t = Mathf.Lerp(a.T, b.T, lerp);
            result = new PathEvaluateResult(distance, PathMath.Evaluate(segment, t), PathMath.EvaluateTangent(segment, t));
            return true;
        }

        private static int BinarySearchSample(IReadOnlyList<PathSample> samples, float distance)
        {
            if (samples.Count <= 1 || distance <= samples[0].Distance)
            {
                return 0;
            }

            if (distance >= samples[^1].Distance)
            {
                return Mathf.Max(0, samples.Count - 2);
            }

            var low = 0;
            var high = samples.Count - 1;
            while (low <= high)
            {
                var mid = (low + high) >> 1;
                if (samples[mid].Distance < distance)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return Mathf.Clamp(low - 1, 0, samples.Count - 2);
        }

        private static PathSegment FindSegment(MovementRoute route, int segmentIndex)
        {
            foreach (var segment in route.Segments)
            {
                if (segment.SegmentIndex == segmentIndex)
                {
                    return segment;
                }
            }

            return route.Segments[Mathf.Clamp(segmentIndex, 0, route.Segments.Count - 1)];
        }

        private static Vector2 ToVector2(Vector3 position)
        {
            return new Vector2(position.x, position.y);
        }
    }
}
