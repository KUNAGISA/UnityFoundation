using UnityEngine;

namespace Gameplay.Sequence
{
    internal sealed class PortraitScaleAction : PortraitTweenAction<Vector2>
    {
        internal PortraitScaleAction(int portraitId, float duration, Vector2 scale) : base(portraitId, duration, scale) { }
        protected override AdvPortraitChannel Channel => AdvPortraitChannel.Scale;
        protected override Vector2 Read(in AdvPortraitVisualState state) => state.Scale;
        protected override Vector2 Interpolate(Vector2 start, Vector2 target, float progress) => Vector2.Lerp(start, target, progress);
        protected override void Write(AdvPortraitRegistry portraits, in AdvPortraitChannelToken token, Vector2 value) => portraits.SetScale(token, value);
    }
}
