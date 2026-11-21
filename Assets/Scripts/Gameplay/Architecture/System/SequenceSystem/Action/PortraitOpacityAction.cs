namespace Gameplay.Sequence
{
    internal sealed class PortraitOpacityAction : PortraitTweenAction<float>
    {
        internal PortraitOpacityAction(int portraitId, float duration, float opacity) : base(portraitId, duration, opacity) { }
        protected override AdvPortraitChannel Channel => AdvPortraitChannel.Opacity;
        protected override float Read(in AdvPortraitVisualState state) => state.Opacity;
        protected override float Interpolate(float start, float target, float progress) => start + (target - start) * progress;
        protected override void Write(AdvPortraitSession portraits, in AdvPortraitChannelToken token, float value) => portraits.SetOpacity(token, value);
    }
}
