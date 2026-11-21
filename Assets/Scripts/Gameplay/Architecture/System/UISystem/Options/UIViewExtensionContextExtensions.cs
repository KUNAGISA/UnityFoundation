using Gameplay.UI;

namespace Gameplay
{
    public static class UIViewExtensionContextExtensions
    {
        public static bool HasOption(this in UIViewExtensionContext context, UIViewOptionFlags option) => context.HasOption(option.ToOptionMask());
    }
}
