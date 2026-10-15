using Gameplay.Common;
using UnityEngine;

namespace Gameplay.Sequence
{
    public struct AdvPortraitVisualState
    {
        public Vector2 Position;
        public Vector2 Origin;
        public ColorRGB Tint;
        public float Opacity;
        public Vector2 Scale;
        public float RotationDegrees;
        public bool HorizontalFlip;
        public bool VerticalFlip;

        public void Reset()
        {
            Position = Vector2.zero;
            Origin = Vector2.zero;
            Tint = ColorRGB.Default;
            Opacity = 1f;
            Scale = Vector2.one;
            RotationDegrees = 0f;
            HorizontalFlip = false;
            VerticalFlip = false;
        }
    }
}
