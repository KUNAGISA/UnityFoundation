using System;
using FancyScrollView.Universal;

namespace Gameplay
{
    public static class UniversalFancyItemActivationExtensions
    {
        public static void SetItemActivation(this UniversalFancyScrollRect scrollRect, Action<int> action)
        {
            scrollRect.SharedContext.Set(new ItemActivation(action));
        }

        public static void SetItemActivation(this UniversalFancyGridView gridView, Action<int> action)
        {
            gridView.SharedContext.Set(new ItemActivation(action));
        }
    }
}
