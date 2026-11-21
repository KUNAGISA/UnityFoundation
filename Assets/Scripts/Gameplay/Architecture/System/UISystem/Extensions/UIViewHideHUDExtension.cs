using System.Collections.Generic;
using Gameplay.UI;

namespace Gameplay
{
    internal sealed class UIViewHideHUDExtension : UIViewExtension
    {
        private readonly HashSet<UIViewToken> _owners = new();

        protected override void OnBeforeShow(in UIViewExtensionContext context)
        {
            if (context.HasOption(UIViewOptionFlags.HideHUD))
            {
                AddOwner(in context);
            }
        }

        protected override void OnBeforeHide(in UIViewExtensionContext context)
        {
            RemoveOwner(in context);
        }

        protected override void OnReleased(in UIViewExtensionContext context)
        {
            RemoveOwner(in context);
        }

        private void AddOwner(in UIViewExtensionContext context)
        {
            if (_owners.Add(context.Token))
            {
                context.Host.AddLayerVisibilityBlocker(UILayer.HUD.ToLayerId(), context.Token);
            }
        }

        private void RemoveOwner(in UIViewExtensionContext context)
        {
            if (_owners.Remove(context.Token))
            {
                context.Host.RemoveLayerVisibilityBlocker(UILayer.HUD.ToLayerId(), context.Token);
            }
        }
    }
}
