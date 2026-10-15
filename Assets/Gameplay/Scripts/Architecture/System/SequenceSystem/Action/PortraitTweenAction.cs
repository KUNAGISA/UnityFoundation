using Aoiro.Sequence;

namespace Gameplay.Sequence
{
    internal abstract class PortraitTweenAction<T> : SequenceAction<ISequenceContext>
    {
        private readonly int _portraitId;
        private readonly float _duration;
        private readonly T _target;
        private T _start;
        private float _elapsed;
        private AdvPortraitChannelToken _token;

        protected PortraitTweenAction(int portraitId, float duration, T target)
        {
            _portraitId = portraitId;
            _duration = duration;
            _target = target;
        }

        protected abstract AdvPortraitChannel Channel { get; }
        protected abstract T Read(in AdvPortraitVisualState state);
        protected abstract T Interpolate(T start, T target, float progress);
        protected abstract void Write(AdvSession session, in AdvPortraitChannelToken token, T value);

        protected override void OnStart()
        {
            _elapsed = 0f;
            var session = Player.GetModule<AdvSession>();
            _token = session.AcquirePortraitToken(_portraitId, Channel);
            _start = Read(session.GetPortraitVisualState(_portraitId));
        }

        protected override bool EvaluateCompletion()
        {
            return !Player.GetModule<AdvSession>().IsPortraitTokenValid(_token) || _elapsed >= _duration;
        }

        protected override void OnTick(float deltaTime)
        {
            _elapsed += deltaTime;
            var progress = _duration > 0f ? _elapsed / _duration : 1f;
            var session = Player.GetModule<AdvSession>();
            Write(session, _token, Interpolate(_start, _target, progress));
        }
    }
}
