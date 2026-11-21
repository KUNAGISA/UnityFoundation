using Gameplay.UI;

namespace Gameplay
{
    internal sealed class UIViewClosePopupExtension : UIViewExtension
    {
        protected override void OnBeforeOpen(in UIViewExtensionContext context)
        {
            if (context.HasOption(UIViewOptionFlags.ClosePopup))
            {
                context.Host.CloseAll(UILayer.Popup.ToLayerId());
            }
        }
    }
}
