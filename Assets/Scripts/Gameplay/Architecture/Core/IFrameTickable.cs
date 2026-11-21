using Gameplay.Scheduler;

namespace Gameplay
{
    internal interface IFrameTickable
    {
        void EarlyTick(in FrameTime time);
        void FrameTick(in FrameTime time);
        void LateTick(in FrameTime time);
    }
}
