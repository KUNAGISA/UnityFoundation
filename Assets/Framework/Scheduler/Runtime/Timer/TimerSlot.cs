using System;

namespace Gameplay.Scheduler
{
    internal struct TimerSlot
    {
        internal int PrevIndex;
        internal int NextIndex;
        internal int BucketIndex;

        internal int Version;

        internal ulong Expires;
        internal int Times;

        internal ulong Interval;
        internal Action Callback;

        internal bool Paused;
        internal ulong Remaining;
        
        internal void Invalidate()
        {
            ++Version;
            Expires = 0UL;
            Times = 0;
            Interval = 0UL;
            Callback = null;
            Paused = false;
            Remaining = 0UL;
            
        }
    }
}