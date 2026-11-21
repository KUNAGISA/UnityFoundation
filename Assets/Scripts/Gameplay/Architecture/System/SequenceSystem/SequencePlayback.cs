using Gameplay.Sequence;

namespace Gameplay
{
    public sealed class SequencePlayback
    {
        public SequenceToken Token { get; }
        public SequencePlaybackStatus Status { get; private set; }

        private SequenceSystem _system;

        internal SequencePlayback(SequenceSystem system, SequenceToken token)
        {
            _system = system;
            Token = token;
            Status = SequencePlaybackStatus.Running;
        }

        private SequencePlayback(SequencePlaybackStatus status)
        {
            Token = SequenceToken.None;
            Status = status;
        }

        public void Cancel()
        {
            _system?.Cancel(Token);
        }

        internal void Complete(SequencePlaybackStatus status)
        {
            if (Status != SequencePlaybackStatus.Running)
            {
                return;
            }

            Status = status;
            _system = null;
        }

        internal static SequencePlayback Failed { get; } = new SequencePlayback(SequencePlaybackStatus.Failed);
    }
}
