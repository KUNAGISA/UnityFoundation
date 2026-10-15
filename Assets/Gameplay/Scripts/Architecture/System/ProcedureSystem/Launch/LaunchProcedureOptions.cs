using System;

namespace Gameplay
{
    public sealed class LaunchProcedureOptions : IProcedureEnterOptions
    {
        public IStartupSplash Splash { get; }
        public int? PreviewSceneId { get; }

        public LaunchProcedureOptions(IStartupSplash splash, int? previewSceneId = null)
        {
            Splash = splash ?? throw new ArgumentNullException(nameof(splash));
            PreviewSceneId = previewSceneId;
        }
    }
}
