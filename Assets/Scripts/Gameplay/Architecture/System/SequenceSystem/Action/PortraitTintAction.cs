using Gameplay.Common;

namespace Gameplay.Sequence
{
    internal sealed class PortraitTintAction : PortraitTweenAction<ColorRGB>
    {
        internal PortraitTintAction(int portraitId, float duration, ColorRGB tint) : base(portraitId, duration, tint) { }
        protected override AdvPortraitChannel Channel => AdvPortraitChannel.Tint;
        protected override ColorRGB Read(in AdvPortraitVisualState state) => state.Tint;
        protected override ColorRGB Interpolate(ColorRGB start, ColorRGB target, float progress) => ColorRGB.Lerp(start, target, progress);
        protected override void Write(AdvPortraitSession portraits, in AdvPortraitChannelToken token, ColorRGB value) => portraits.SetTint(token, value);
    }
}
