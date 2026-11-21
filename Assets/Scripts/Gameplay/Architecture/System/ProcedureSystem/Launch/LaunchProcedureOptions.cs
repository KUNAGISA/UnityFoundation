using System;
using UnityEngine;

namespace Gameplay
{
    public sealed class LaunchProcedureOptions : IProcedureEnterOptions
    {
        public IStartupSplash Splash { get; }
        public int? PreviewSceneId { get; }
        public Vector2? PreviewPosition { get; }

        public LaunchProcedureOptions(IStartupSplash splash, int? previewSceneId = null, Vector2? previewPosition = null)
        {
            Splash = splash ?? throw new ArgumentNullException(nameof(splash));
            PreviewSceneId = previewSceneId;
            PreviewPosition = previewPosition;
        }
    }
}
