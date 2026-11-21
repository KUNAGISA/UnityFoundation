using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gameplay.Rendering
{
    internal enum DualKawaseBlurInjectionPoint
    {
        BeforeTransparents = RenderPassEvent.BeforeRenderingTransparents,
        BeforePostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
        AfterPostProcessing = RenderPassEvent.AfterRenderingPostProcessing
    }

    [Serializable]
    internal sealed class DualKawaseBlurSettings
    {
        internal const int MaxLevels = 6;

        [SerializeField]
        private DualKawaseBlurInjectionPoint _injectionPoint = DualKawaseBlurInjectionPoint.BeforePostProcessing;

        [SerializeField, Range(1, MaxLevels)]
        private int _levels = 3;

        [SerializeField, Min(0.1f)]
        private float _radius = 0.5f;

        internal RenderPassEvent RenderPassEvent => (RenderPassEvent)_injectionPoint;
        internal int Levels => Mathf.Clamp(_levels, 1, MaxLevels);
        internal float Radius => Mathf.Max(0.1f, _radius);
    }
}
