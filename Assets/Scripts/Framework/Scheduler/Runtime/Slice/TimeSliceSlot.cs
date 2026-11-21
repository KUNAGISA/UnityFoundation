namespace Gameplay.Scheduler
{
    internal struct TimeSliceSlot
    {
        internal int PrevIndex;
        internal int NextIndex;
        internal int Version;

        internal bool Paused;

        internal TimeSliceCallback Callback;

        internal long EmaTicks;
        
        internal void Invalidate()
        {
            ++Version;
            Paused = false;
            Callback = null;
        }
    }
}
