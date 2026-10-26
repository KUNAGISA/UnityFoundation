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

        public static AdvPortraitVisualState Default => new AdvPortraitVisualState
        {
            Tint = ColorRGB.Default,
            Opacity = 1f,
            Scale = Vector2.one
        };
    }
}
