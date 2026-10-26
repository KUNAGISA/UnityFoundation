using UnityEngine;

namespace Gameplay.Sequence
{
    internal sealed class PortraitMoveAction : PortraitTweenAction<Vector2>
    {
        internal PortraitMoveAction(int portraitId, float duration, Vector2 position) : base(portraitId, duration, position) { }
        protected override AdvPortraitChannel Channel => AdvPortraitChannel.Position;
        protected override Vector2 Read(in AdvPortraitVisualState state) => state.Position;
        protected override Vector2 Interpolate(Vector2 start, Vector2 target, float progress) => Vector2.Lerp(start, target, progress);
        protected override void Write(AdvPortraitSession portraits, in AdvPortraitChannelToken token, Vector2 value) => portraits.SetPosition(token, value);
    }
}
