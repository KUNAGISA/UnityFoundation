using System.Runtime.CompilerServices;
using Gameplay.Scheduler;

namespace Gameplay
{
    public static class TimeSourceExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameTime ToFrameTime(this ITimeSource source)
        {
            return new FrameTime(source.DeltaTimeMs, source.UnscaledDeltaTimeMs, source.TimeMs, source.UnscaledTimeMs);
        }
    }
}