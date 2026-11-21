namespace Gameplay
{
    public enum SequencePlaybackStatus : byte
    {
        None = 0,
        Running = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4,
    }
}
