namespace Gameplay.Sequence
{
    internal sealed class PortraitRotateAction : PortraitTweenAction<float>
    {
        internal PortraitRotateAction(int portraitId, float duration, float degree) : base(portraitId, duration, degree) { }
        protected override AdvPortraitChannel Channel => AdvPortraitChannel.Rotation;
        protected override float Read(in AdvPortraitVisualState state) => state.RotationDegrees;
        protected override float Interpolate(float start, float target, float progress) => start + (target - start) * progress;
        protected override void Write(AdvSession session, in AdvPortraitChannelToken token, float value) => session.SetPortraitRotation(token, value);
    }
}
