using UnityEngine;

namespace Gameplay.Common
{
    public struct ColorRGB
    {
        public byte R;
        public byte G;
        public byte B;
        
        public ColorRGB(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
        
        public readonly Color ToColor(float alpha = 1f)
        {
            return new Color(R / 255f, G / 255f, B / 255f, alpha);
        }

        public static ColorRGB Default => new ColorRGB(255, 255, 255);
        
        public static ColorRGB Lerp(in ColorRGB from, in ColorRGB to, float t)
        {
            t = Mathf.Clamp01(t);
            return new ColorRGB(
                (byte)Mathf.Lerp(from.R, to.R, t),
                (byte)Mathf.Lerp(from.G, to.G, t),
                (byte)Mathf.Lerp(from.B, to.B, t));
        }
        
        public static implicit operator Color(ColorRGB color)
        {
            return color.ToColor();
        }

        public static explicit operator ColorRGB(Color color)
        {
            return new ColorRGB(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f));
        }
    }
}