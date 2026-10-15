using System.Collections.Generic;
using Aoiro;
using UnityEngine;

namespace Gameplay
{
    public readonly struct BuildMovementRoutesCommand : ICommand
    {
        private readonly struct LengthSample
        {
            public readonly float T;
            public readonly float Length;

            public LengthSample(float t, float length)
            {
                T = t;
                Length = length;
            }
        }

        private const int BezierLengthSampleCount = 128;

        private readonly SceneMovementPath _sceneMovementPath;

        public BuildMovementRoutesCommand(SceneMovementPath sceneMovementPath)
        {
            _sceneMovementPath = sceneMovementPath;
        }

        void ICommand.Execute(ICommandContext context)
        {
            var model = context.GetModel<MovementRouteModel>();
            model.Clear();

            if (_sceneMovementPath == null)
            {
                Debug.LogWarning("[Movement] 当前场景没有 SceneMovementPath");
                return;
            }

            BuildRoutes(_sceneMovementPath.Routes, _sceneMovementPath.SampleSpacing, model);
        }

        private static void BuildRoutes(IReadOnlyList<MovementRouteData> routeDataList, float sampleSpacing, MovementRouteModel model)
        {
            sampleSpacing = Mathf.Max(0.01f, sampleSpacing);
            var lengthSamples = new List<LengthSample>(BezierLengthSampleCount + 1);

            foreach (var routeData in routeDataList)
            {
                if (routeData == null || routeData.Nodes == null || routeData.Nodes.Count < 2)
                {
                    continue;
                }

                if (model.Contains(routeData.RouteId))
                {
                    Debug.LogWarning($"[Movement] 重复路线 id [{routeData.RouteId}]，已跳过");
                    continue;
                }

                if (TryBuildRoute(routeData, sampleSpacing, lengthSamples, out var route))
                {
                    model.TryAdd(route);
                }
            }
        }

        private static bool TryBuildRoute(MovementRouteData routeData, float sampleSpacing, List<LengthSample> lengthSamples, out MovementRoute route)
        {
            var segments = new List<PathSegment>(routeData.Nodes.Count - 1);
            var samples = new List<PathSample>();
            var routeDistance = 0f;

            for (var i = 0; i < routeData.Nodes.Count - 1; i++)
            {
                var current = routeData.Nodes[i];
                var next = routeData.Nodes[i + 1];
                var type = current.NextSegmentType;
                float length;
                if (type == PathSegmentType.Line)
                {
                    length = Vector2.Distance(current.Position, next.Position);
                }
                else
                {
                    BuildBezierLengthSamples(current.Position, current.OutTangent, next.InTangent, next.Position, lengthSamples);
                    length = lengthSamples[^1].Length;
                }

                if (length <= Mathf.Epsilon)
                {
                    Debug.LogWarning($"[Movement] 路线 [{routeData.RouteId}] 路段 [{i}] 长度为 0，已跳过");
                    continue;
                }

                var segment = type == PathSegmentType.Line
                    ? new PathSegment(i, type, current.Position, next.Position, default, default, routeDistance, length)
                    : new PathSegment(i, type, current.Position, current.OutTangent, next.InTangent, next.Position, routeDistance, length);

                segments.Add(segment);
                AppendSegmentSamples(routeData.RouteId, segment, sampleSpacing, lengthSamples, samples);
                routeDistance += length;
            }

            if (segments.Count == 0 || samples.Count < 2)
            {
                route = null;
                return false;
            }

            route = new MovementRoute(routeData.RouteId, routeDistance, segments, samples);
            return true;
        }

        private static void AppendSegmentSamples(int routeId, in PathSegment segment, float sampleSpacing, List<LengthSample> lengthSamples, List<PathSample> samples)
        {
            if (segment.Type == PathSegmentType.Line)
            {
                samples.Add(new PathSample(routeId, segment.SegmentIndex, 0f, segment.StartDistance, segment.P0));
                samples.Add(new PathSample(routeId, segment.SegmentIndex, 1f, segment.StartDistance + segment.Length, segment.P1));
                return;
            }

            var count = Mathf.Max(Mathf.CeilToInt(segment.Length / sampleSpacing), 1);
            for (var i = 0; i <= count; i++)
            {
                var localDistance = i == count ? segment.Length : Mathf.Min(i * sampleSpacing, segment.Length);
                var t = FindBezierTAtLocalDistance(localDistance, lengthSamples);
                var distance = segment.StartDistance + localDistance;
                samples.Add(new PathSample(routeId, segment.SegmentIndex, t, distance, PathMath.Evaluate(segment, t)));
            }
        }

        private static void BuildBezierLengthSamples(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, List<LengthSample> lengthSamples)
        {
            lengthSamples.Clear();
            lengthSamples.Add(new LengthSample(0f, 0f));

            var length = 0f;
            var previous = p0;
            for (var i = 1; i <= BezierLengthSampleCount; i++)
            {
                var t = (float)i / BezierLengthSampleCount;
                var position = PathMath.EvaluateBezier(p0, p1, p2, p3, t);
                length += Vector2.Distance(previous, position);
                lengthSamples.Add(new LengthSample(t, length));
                previous = position;
            }
        }

        private static float FindBezierTAtLocalDistance(float localDistance, IReadOnlyList<LengthSample> lengthSamples)
        {
            if (lengthSamples.Count == 0 || localDistance <= 0f)
            {
                return 0f;
            }

            var last = lengthSamples[^1];
            if (localDistance >= last.Length)
            {
                return 1f;
            }

            var low = 0;
            var high = lengthSamples.Count - 1;
            while (low <= high)
            {
                var mid = (low + high) >> 1;
                if (lengthSamples[mid].Length < localDistance)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            var index = Mathf.Clamp(low - 1, 0, lengthSamples.Count - 2);
            var a = lengthSamples[index];
            var b = lengthSamples[index + 1];
            var range = b.Length - a.Length;
            var lerp = range > 0f ? (localDistance - a.Length) / range : 0f;
            return Mathf.Lerp(a.T, b.T, lerp);
        }
    }
}
