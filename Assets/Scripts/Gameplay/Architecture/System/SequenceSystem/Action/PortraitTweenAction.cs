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
        protected abstract void Write(AdvPortraitSession portraits, in AdvPortraitChannelToken token, T value);

        protected override void OnStart()
        {
            _elapsed = 0f;
            var portraits = Player.GetModule<AdvPortraitSession>();
            _token = portraits.AcquireChannel(_portraitId, Channel);
            _start = Read(portraits.GetVisualStateOrDefault(_portraitId));
        }

        protected override bool EvaluateCompletion()
        {
            return !Player.GetModule<AdvPortraitSession>().IsTokenValid(_token) || _elapsed >= _duration;
        }

        protected override void OnTick(float deltaTime)
        {
            _elapsed += deltaTime;
            var progress = _duration > 0f ? _elapsed / _duration : 1f;
            var portraits = Player.GetModule<AdvPortraitSession>();
            Write(portraits, _token, Interpolate(_start, _target, progress));
        }
    }
}
