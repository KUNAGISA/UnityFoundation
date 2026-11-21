using System.Collections.Generic;

namespace Gameplay
{
    public sealed class MovementRoute
    {
        public int RouteId { get; }
        public float TotalLength { get; }
        public IReadOnlyList<PathSegment> Segments { get; }
        public IReadOnlyList<PathSample> Samples { get; }

        public MovementRoute(int routeId, float totalLength, IReadOnlyList<PathSegment> segments, IReadOnlyList<PathSample> samples)
        {
            RouteId = routeId;
            TotalLength = totalLength;
            Segments = segments;
            Samples = samples;
        }
    }
}
