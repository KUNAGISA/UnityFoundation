namespace Gameplay.Scheduler
{
    internal struct FrameTickSlot
    {
        internal int PrevIndex;
        internal int NextIndex;
        internal int Version;

        internal bool Paused;

        internal FrameTickAction Callback;
        
        internal void Invalidate()
        {
            ++Version;
            Paused = false;
            Callback = null;
        }
    }
}
