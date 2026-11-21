using System;
using Gameplay.UI;

namespace Gameplay
{
    [Flags]
    public enum UIViewOptionFlags
    {
        None = 0,
        ClosePopup = 1 << 0,
        HideHUD = 1 << 1,
        Blur = 1 << 2,
        HideMainCamera = 1 << 3,
        BlockGameplayInput = 1 << 4
    }

    public static class UIViewOptionFlagsExtensions
    {
        public static UIViewOptionMask ToOptionMask(this UIViewOptionFlags options) => new UIViewOptionMask((int)options);
    }
}
